using System;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Applies an existing material to a GameObject.")]
    public class ApplyMaterialAction : GPTActionBase
    {
        [GPTParameter("Name of the GameObject")]
        public string ObjectName { get; set; }

        [GPTParameter("Path to the material asset. Ex: Assets/Materials/NewMaterial.mat")]
        public string MaterialAssetPath { get; set; }

        public override string Content =>
            $"Applied material '{Highlight(MaterialAssetPath)}' to '{Highlight(ObjectName)}'";

        public override void Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
            {
                throw new Exception($"GameObject '{ObjectName}' not found.");
            }

            if (!UnityAiHelpers.TryFindAsset(MaterialAssetPath, typeof(Material), out var material))
            {
                throw new Exception($"Material '{MaterialAssetPath}' not found in Resources.");
            }

            var renderer = go.GetComponent<Renderer>();
            if (!renderer)
            {
                throw new Exception("No Renderer component found on " + ObjectName);
            }

            renderer.sharedMaterial = material as Material; // or renderer.material
        }
    }
}