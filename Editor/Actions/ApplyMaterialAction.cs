using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Assigns an existing material asset to a GameObject renderer.", Name = "apply_material")]
    public class ApplyMaterialAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

        [GPTParameter("Material asset path, for example 'Assets/Materials/NewMaterial.mat'.", true, Name = "material_asset_path")]
        public string MaterialAssetPath { get; set; }

        public override async Task<string> Execute()
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
            
            return $"Applied material '{MaterialAssetPath}' to '{ObjectName}'.";
        }
    }
}
