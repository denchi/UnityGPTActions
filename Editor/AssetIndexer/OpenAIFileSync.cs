using GPTUnity.Settings;

namespace GptActions.Editor.AssetIndexer
{
    using UnityEngine;
    using UnityEditor;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.IO;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;

    public static class OpenAIFileSync
    {
        private const string FilePath = "Assets/Editor/AssetIndexer/ExportedAssetIndex.json";
        private const string TrackerPath = "Assets/Editor/AssetIndexer/OpenAIFileTracker.asset";

        [MenuItem("Tools/OpenAI/Sync Asset Index to OpenAI")]
        public static async void UploadToOpenAI()
        {
            if (!File.Exists(FilePath))
            {
                Debug.LogError("❌ Asset index file not found.");
                return;
            }

            var tracker = AssetDatabase.LoadAssetAtPath<OpenAIFileTracker>(TrackerPath);
            if (tracker == null)
            {
                Debug.LogError(
                    "❌ OpenAIFileTracker.asset not found. Create it via Assets > Create > Tools > OpenAI File Tracker");
                return;
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChatSettings.instance.ApiKey);

            // 🔄 Delete previous file if it exists
            if (!string.IsNullOrEmpty(tracker.lastFileId))
            {
                var deleteResponse = await client.DeleteAsync($"https://api.openai.com/v1/files/{tracker.lastFileId}");
                if (deleteResponse.IsSuccessStatusCode)
                    Debug.Log($"🗑️ Deleted previous file: {tracker.lastFileId}");
                else
                    Debug.LogWarning(
                        $"⚠️ Could not delete file {tracker.lastFileId}: {await deleteResponse.Content.ReadAsStringAsync()}");
            }

            // ⬆️ Upload new file
            using var form = new MultipartFormDataContent();
            using var fs = File.OpenRead(FilePath);
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            form.Add(fileContent, "file", "ExportedAssetIndex.json");
            form.Add(new StringContent("assistants"), "purpose");

            var uploadResponse = await client.PostAsync("https://api.openai.com/v1/files", form);
            var result = await uploadResponse.Content.ReadAsStringAsync();

            if (!uploadResponse.IsSuccessStatusCode)
            {
                Debug.LogError("❌ Upload failed:\n" + result);
                return;
            }

            // ✅ Extract file_id using Newtonsoft
            var json = JObject.Parse(result);
            string newFileId = json["id"]?.ToString();

            if (string.IsNullOrEmpty(newFileId))
            {
                Debug.LogError("❌ Could not find file_id in response.");
                return;
            }

            tracker.lastFileId = newFileId;
            EditorUtility.SetDirty(tracker);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Upload successful. New file_id: {newFileId}");
        }
    }
}