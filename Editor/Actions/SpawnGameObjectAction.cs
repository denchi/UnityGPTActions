using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPTUnity.Actions
{
    [GPTAction("Spawns a new (or existing) prefab at a specified position.")]
    public class SpawnGameObjectAction : GPTAssistantAction
    {
        [GPTParameter("Prefab asset path")] 
        public string PrefabAssetPath { get; set; }

        [GPTParameter("Parent GameObject name. Can be a path like Canvas/Panel/Button")]
        public string ParentObjectName { get; set; }

        [GPTParameter("New position in 'x,y,z' format. Leave empty if no change.")]
        public string Position { get; set; }

        [GPTParameter("New rotation in 'x,y,z' format. Leave empty if no change.")]
        public string Rotation { get; set; }

        [GPTParameter("New scale in 'x,y,z' format. Leave empty if no change.")]
        public string Scale { get; set; }

        [GPTParameter("New local position in 'x,y,z' format. Leave empty if no change.")]
        public string LocalPosition { get; set; }

        [GPTParameter("New local rotation in 'x,y,z' format. Leave empty if no change.")]
        public string LocalRotation { get; set; }

        public override string Description => $"Spawned '{Highlight(PrefabAssetPath)}' at '{Highlight(Position)}'";

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (!UnityAiHelpers.TryFindAsset(PrefabAssetPath, typeof(GameObject), out var asset))
                throw new Exception($"Prefab '{PrefabAssetPath}' not found!");
            
            var go = Object.Instantiate(asset as GameObject);
            
            if (UnityAiHelpers.TryFindGameObject(ParentObjectName, out var parent))
            {
                go.transform.SetParent(parent.transform);
            }
            
            if (UnityAiHelpers.TryParseVector3(Position, out var position))
                go.transform.position = position;

            if (UnityAiHelpers.TryParseVector3(Rotation, out var rotation))
                go.transform.eulerAngles = rotation;

            if (UnityAiHelpers.TryParseVector3(Scale, out var scale))
                go.transform.localScale = scale;

            if (UnityAiHelpers.TryParseVector3(LocalPosition, out var localPosition))
                go.transform.localPosition = localPosition;

            if (UnityAiHelpers.TryParseVector3(LocalRotation, out var localRotation))
                go.transform.localEulerAngles = localRotation;

            Undo.RegisterCreatedObjectUndo(go, "Spawn GameObject");

            
            return $"Prefab '{PrefabAssetPath}' spawned at {go.PathToGameObject()}";
            
            #endif
        }
    }
}