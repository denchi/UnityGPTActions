using System;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a new layer.")]
    public class CreateLayerAction : GPTActionBase
    {
        [GPTParameter("Name of the new layer")]
        public string LayerName { get; set; }

        public override string Content => $"Created new layer: {Highlight(LayerName)}";

        public override void Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(LayerName))
                throw new Exception("Layer name cannot be empty.");

            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            if (Array.Exists(layers, layer => layer == LayerName))
                return;

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
                    Debug.Log($"Layer '{LayerName}' created.");
                    return;
                }
            }

            throw new Exception("Maximum number of layers reached. Cannot create new layer.");
#endif
        }
    }
}