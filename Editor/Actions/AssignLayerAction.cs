using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Assigns an existing Unity layer to a GameObject.", Name = "set_game_object_layer")]
    public class AssignLayerAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

        [GPTParameter("Existing layer name to assign.", true, Name = "layer_name")]
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
