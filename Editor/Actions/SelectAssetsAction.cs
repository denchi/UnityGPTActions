using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace GPTUnity.Actions
{
    [GPTAction("Selects project assets in the Unity Project window by asset path.", Name = "select_assets")]
    public class SelectAssetsAction : GPTAssistantAction
    {
        [GPTParameter("Semicolon-separated asset paths to select, for example 'Assets/Textures/A.png;Assets/Prefabs/B.prefab'.", true, Name = "asset_paths")] 
        public string AssetPathsString { get; set; }
        
        private List<string> AssetPaths => AssetPathsString
            .Split(';')
            .ToList();
        
        public override Task<string> Execute()
        {
            if (string.IsNullOrEmpty(AssetPathsString))
            {
                throw new Exception("AssetPathsString cannot be null or empty.");
            }
            
            var objects = AssetPaths.Select(filePath => AssetDatabase.LoadMainAssetAtPath(filePath)).Where(asset => asset).ToList();

            Selection.objects = objects.ToArray();

            return Task.FromResult($"Selected {objects.Count} assets in the project view.");
        }
    }
}
