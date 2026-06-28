using System;
using System.IO;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a prefab asset from an existing scene GameObject.", Name = "create_prefab")]
    public class CreatePrefabAction : GPTAssistantAction
    {
        [GPTParameter("Prefab asset path to create, for example 'Assets/Prefabs/MyPrefab.prefab'.", true, Name = "prefab_asset_path")] 
        public string PrefabAssetPath { get; set; }

        [GPTParameter("Scene GameObject name or hierarchy path to turn into a prefab.", true, Name = "game_object_name_or_path")]
        public string GameObjectName { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(PrefabAssetPath))
                throw new Exception($"Invalid path to create a prefab: '{PrefabAssetPath}'");

            if (!PrefabAssetPath.StartsWith("Assets/") || !PrefabAssetPath.EndsWith(".prefab"))
                throw new Exception(
                    $"Invalid PrefabAssetPath. It must start with 'Assets/' and end with '.prefab'. Got: '{PrefabAssetPath}'");

            if (Path.IsPathRooted(PrefabAssetPath))
                throw new Exception(
                    $"PrefabAssetPath should be relative to the Unity project (e.g. 'Assets/...'), not an absolute path: '{PrefabAssetPath}'");

            if (!UnityAiHelpers.TryFindGameObject(GameObjectName, out var gameObject))
                throw new Exception($"Could not find game object '{GameObjectName}' to create asset from");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(PrefabAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh(); // Register new folders in Unity
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, PrefabAssetPath, InteractionMode.UserAction);
            return $"Prefab created at: {PrefabAssetPath}";
#endif
        }
    }
}
