using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace GPTUnity.Actions
{
    [GPTAction("Lists all settings assets found in the ProjectSettings folder.")]
    public class ListAllProjectSettingsAssetsAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            string settingsPath = "ProjectSettings";
            if (!Directory.Exists(settingsPath))
                throw new Exception("Could not find the ProjectSettings directory.");

            var sb = new StringBuilder();
            string[] files = Directory.GetFiles(settingsPath, "*.asset", SearchOption.TopDirectoryOnly);

            foreach (string filePath in files)
            {
                string assetPath = filePath.Replace("\\", "/");
                UnityEngine.Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath);

                string assetTypes = loaded.Length > 0
                    ? string.Join(", ", Array.ConvertAll(loaded, o => o?.GetType().Name ?? "null"))
                    : "No assets loaded";

                sb.AppendLine($"{assetPath} → {assetTypes}");
            }

            return sb.ToString();
#endif
        }
    }
}