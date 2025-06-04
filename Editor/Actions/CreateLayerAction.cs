using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a new layer.")]
    public class CreateLayerAction : GPTAssistantAction
    {
        [GPTParameter("Name of the new layer")]
        public string LayerName { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(LayerName))
                throw new Exception("Layer name cannot be empty.");

            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            if (Array.Exists(layers, layer => layer == LayerName))
                return $"Layer '{LayerName}' already exists.";

            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++) // Layers 0-7 are reserved
            {
                SerializedProperty layer = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = LayerName;
                    tagManager.ApplyModifiedProperties();
                    return $"Layer '{LayerName}' created.";
                }
            }

            throw new Exception("Maximum number of layers reached. Cannot create new layer.");
#endif
        }
    }
}