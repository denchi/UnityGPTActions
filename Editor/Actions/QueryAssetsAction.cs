using System.Text;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Queries Unity assets by path, type, and/or name, providing detailed information about matching assets.")]
    public class QueryAssetsAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("The path to search for assets (optional)")]
        public string Path { get; set; }

        [GPTParameter("The type of assets to search for (e.g., 'Material', 'Texture2D', 'Prefab') (optional)")]
        public string AssetType { get; set; }

        [GPTParameter("The name of the asset to search for (optional)")]
        public string AssetName { get; set; }

        public string Content => GetFilter();

        public override async Task<string> Execute()
        {
            var sb = new StringBuilder();
            var searchPath = string.IsNullOrEmpty(Path) ? "Assets" : Path;

            var filter = GetFilter();

            // Find all matching assets
            var guids = AssetDatabase.FindAssets(filter, new[] { searchPath });
            if (guids.Length == 0)
            {
                return $"No assets found matching the specified criteria in path: {searchPath}";
            }

            sb.AppendLine($"Found {guids.Length} assets matching the criteria:");

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                
                // // Skip if type doesn't match (if specified)
                // if (!string.IsNullOrEmpty(AssetType) && 
                //     !asset.GetType().Name.Equals(AssetType, System.StringComparison.OrdinalIgnoreCase))
                // {
                //     continue;
                // }

                if (asset)
                    sb.AppendLine($"{assetPath}");

                // // Add specific asset type information
                // if (asset is Texture2D texture)
                // {
                //     sb.AppendLine($"Size: {texture.width}x{texture.height}");
                //     sb.AppendLine($"Format: {texture.format}");
                // }
                // else if (asset is Material material)
                // {
                //     sb.AppendLine($"Shader: {material.shader.name}");
                //     sb.AppendLine($"Render Queue: {material.renderQueue}");
                // }
                // else if (asset is GameObject prefab)
                // {
                //     var components = prefab.GetComponents<Component>();
                //     sb.AppendLine($"Components: {string.Join(", ", components.Select(c => c.GetType().Name))}");
                // }
                // else if (asset is AudioClip audio)
                // {
                //     sb.AppendLine($"Length: {audio.length:F2}s");
                //     sb.AppendLine($"Channels: {audio.channels}");
                // }

                //sb.AppendLine();
            }

            return sb.ToString(); 
        }

        private string GetFilter()
        {
            var filter = "";
            if (!string.IsNullOrEmpty(AssetName))
            {
                filter = AssetName;
            }
            
            if (!string.IsNullOrEmpty(AssetType))
            {
                filter = $"t: {AssetType} {filter}";
            }

            return filter;
        }
    }
}
