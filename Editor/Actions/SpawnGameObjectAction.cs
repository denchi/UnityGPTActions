using System;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Spawns a new (or existing) prefab at a specified position.")]
    public class SpawnGameObjectAction : GPTActionBase
    {
        [GPTParameter("Prefab asset path")] public string PrefabAssetPath { get; set; }

        [GPTParameter("Spawn position in 'x,y,z' format")]
        public string Position { get; set; }

        public override string Content => $"Spawned '{Highlight(PrefabAssetPath)}' at '{Highlight(Position)}'";

        public override void Execute()
        {
#if UNITY_EDITOR
            // This assumes your prefab is in Resources or something similar
            // Alternatively, you can do Editor asset lookups
            var pos = ParseVector3(Position);

            if (!UnityAiHelpers.TryFindAsset(PrefabAssetPath, typeof(GameObject), out var asset))
                throw new Exception($"Prefab '{PrefabAssetPath}' not found in Resources.");

            var instance = GameObject.Instantiate(asset as GameObject, pos, Quaternion.identity);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn GameObject");
#endif
        }

        private Vector3 ParseVector3(string input)
        {
            var parts = input.Split(',');
            if (parts.Length != 3) return Vector3.zero;
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            float.TryParse(parts[2], out float z);
            return new Vector3(x, y, z);
        }
    }
}