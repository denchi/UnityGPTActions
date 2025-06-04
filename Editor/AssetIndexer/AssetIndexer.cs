using System.Net.Http;
using System.Net.Http.Headers;
using GPTUnity.Settings;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace GptActions.Editor.AssetIndexer
{
    [InitializeOnLoad]
    public static class AssetIndexer
    {
        private const string AssetPath = "Assets/Editor/AssetIndexer/AssetIndexDatabase.asset";
        private static AssetIndexDatabase _database;
        private static double _lastCheckTime = 0;
        private static readonly double _checkInterval = 10.0; // seconds
        private static string _lastJsonHash = "";
        private const string FilePath = "Assets/Editor/AssetIndexer/ExportedAssetIndex.json";
        
        static AssetIndexer()
        {
            // EditorApplication.update += Update;
            // LoadOrCreateDatabase();
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling || Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastCheckTime < _checkInterval) return;

            _lastCheckTime = EditorApplication.timeSinceStartup;
            IndexAssets();
        }

        private static void LoadOrCreateDatabase()
        {
            _database = AssetDatabase.LoadAssetAtPath<AssetIndexDatabase>(AssetPath);
            if (_database == null)
            {
                _database = ScriptableObject.CreateInstance<AssetIndexDatabase>();
                Directory.CreateDirectory("Assets/Editor/AssetIndexer");
                AssetDatabase.CreateAsset(_database, AssetPath);
                AssetDatabase.SaveAssets();
            }
        }


        private static void IndexAssets()
        {
            var guids = AssetDatabase.FindAssets("");
            var seen = new HashSet<string>();
            bool changed = false;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                var extension = Path.GetExtension(path).ToLowerInvariant();
                var type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown";

                var entry = new AssetIndexEntry
                {
                    guid = guid,
                    path = path,
                    extension = extension,
                    type = type
                };

                var existing = _database.entries.FirstOrDefault(e => e.guid == guid);
                if (existing == null || !EntriesEqual(existing, entry))
                {
                    _database.UpdateOrAddEntry(entry);
                    changed = true;
                }

                seen.Add(guid);
            }

            var removedGuids = _database.entries
                .Where(e => !seen.Contains(e.guid))
                .Select(e => e.guid)
                .ToList();

            foreach (var guid in removedGuids)
            {
                _database.RemoveByGUID(guid);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(_database);
                ExportToJsonIfChanged();
            }
        }

        private static bool EntriesEqual(AssetIndexEntry a, AssetIndexEntry b)
        {
            return a.path == b.path && a.extension == b.extension && a.type == b.type;
        }
        
        private static void ExportToJsonIfChanged()
        {
            var wrapper = new ExportWrapper(_database.entries);
            var json = JsonUtility.ToJson(wrapper, true);

            string hash = json.GetHashCode().ToString();
            if (hash == _lastJsonHash) return; // no meaningful change

            _lastJsonHash = hash;

            string path = "Assets/Editor/AssetIndexer/ExportedAssetIndex.json";
            File.WriteAllText(path, json);
            AssetDatabase.Refresh(); // ensure it’s updated in Unity

            Debug.Log("Asset index updated and exported.");
        }

        [MenuItem("Tools/Upload Asset Index to OpenAI")]
        public static async void UploadToOpenAI()
        {
            if (!File.Exists(FilePath))
            {
                Debug.LogError("Asset index file not found.");
                return;
            }
            
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChatSettings.instance.ApiKey);

            // Upload new file
            using var form = new MultipartFormDataContent();
            using var fs = File.OpenRead(FilePath);
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            form.Add(fileContent, "file", "ExportedAssetIndex.json");
            form.Add(new StringContent("file-search"), "purpose");

            Debug.Log("Uploading file to OpenAI...");
            var response = await client.PostAsync("https://api.openai.com/v1/files", form);
            var result = await response.Content.ReadAsStringAsync();
            Debug.Log("Upload result: " + result);
        }
    }
}