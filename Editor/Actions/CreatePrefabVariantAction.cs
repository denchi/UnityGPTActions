using System;
using System.IO;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a prefab variant from a scene prefab instance or a source prefab.")]
    public class CreatePrefabVariantAction : GPTAssistantAction
    {
        [GPTParameter("Output variant path (Assets/... .prefab)")]
        public string VariantPrefabPath { get; set; }

        [GPTParameter("Optional scene object name/path; must be a prefab instance root for variant behavior")]
        public string SceneObjectName { get; set; }

        [GPTParameter("Optional source prefab path (Assets/... .prefab)")]
        public string SourcePrefabPath { get; set; }

        [GPTParameter("Require resulting asset type to be a prefab variant")]
        public bool RequireVariant { get; set; } = true;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(VariantPrefabPath))
                throw new Exception("VariantPrefabPath is required.");

            if (!VariantPrefabPath.StartsWith("Assets/") || !VariantPrefabPath.EndsWith(".prefab"))
                throw new Exception("VariantPrefabPath must start with 'Assets/' and end with '.prefab'.");

            EnsureDirectoryExists(VariantPrefabPath);

            GameObject sourceInstance = null;
            var destroySourceAfterSave = false;

            if (!string.IsNullOrWhiteSpace(SceneObjectName))
            {
                if (!UnityAiHelpers.TryFindGameObject(SceneObjectName, out sourceInstance))
                    throw new Exception($"Scene object '{SceneObjectName}' not found.");

                sourceInstance = PrefabUtility.GetOutermostPrefabInstanceRoot(sourceInstance) ?? sourceInstance;
            }
            else if (!string.IsNullOrWhiteSpace(SourcePrefabPath))
            {
                var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
                if (sourcePrefab == null)
                    throw new Exception($"Source prefab not found at '{SourcePrefabPath}'.");

                sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
                destroySourceAfterSave = true;
            }
            else
            {
                throw new Exception("Provide either SceneObjectName or SourcePrefabPath.");
            }

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(sourceInstance, VariantPrefabPath);
            if (destroySourceAfterSave && sourceInstance != null)
                UnityEngine.Object.DestroyImmediate(sourceInstance);

            if (prefabAsset == null)
                throw new Exception($"Failed to create prefab at '{VariantPrefabPath}'.");

            var assetType = PrefabUtility.GetPrefabAssetType(prefabAsset);
            if (RequireVariant && assetType != PrefabAssetType.Variant)
                throw new Exception($"Created asset is '{assetType}', not a Variant. Source must be a prefab instance.");

            return $"Created prefab asset at '{VariantPrefabPath}' with type '{assetType}'.";
#endif
        }

        private static void EnsureDirectoryExists(string prefabPath)
        {
            var directory = Path.GetDirectoryName(prefabPath);
            if (string.IsNullOrWhiteSpace(directory))
                return;

            if (Directory.Exists(directory))
                return;

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
    }
}
