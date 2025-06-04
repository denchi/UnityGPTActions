using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace GPTUnity.Actions
{
    [GPTAction("Select all specified project assets in the project view")]
    public class SelectAssetsAction : GPTAssistantAction
    {
        [GPTParameter("Paths to the project assets, separated by a ;. Ex: Assets/Textures/MyTexture.png;Assets/Prefabs/MyPrefab.prefab")] 
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