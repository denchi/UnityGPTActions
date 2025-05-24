using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Assigns a layer to a GameObject.")]
    public class AssignLayerAction : GPTActionBase
    {
        [GPTParameter("Name of the GameObject")]
        public string ObjectName { get; set; }

        [GPTParameter("Name of the layer to assign")]
        public string LayerName { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(LayerName))
                throw new Exception("Layer name cannot be empty.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var layer = LayerMask.NameToLayer(LayerName);
            if (layer == -1)
                throw new Exception($"Layer '{LayerName}' does not exist.");

            go.layer = layer;
            
            return $"Assigned layer '{LayerName}' to GameObject '{ObjectName}'.";
#endif
        }
    }
}