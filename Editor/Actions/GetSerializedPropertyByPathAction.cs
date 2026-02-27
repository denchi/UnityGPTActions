using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Gets a serialized property value from a component by SerializedProperty path.")]
    public class GetSerializedPropertyByPathAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("SerializedProperty path, e.g. '_assets.Array.data[0]' or '_assets[0]'")]
        public string PropertyPath { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var gameObject))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var component = gameObject.GetComponent(type);
            if (!component)
                throw new Exception($"GameObject '{ObjectName}' does not have component '{ComponentTypeName}'.");

            var serializedObject = new SerializedObject(component);
            var property = ActionEditingUtilities.FindPropertyWithAliases(serializedObject, PropertyPath);
            if (property == null)
            {
                var normalized = ActionEditingUtilities.NormalizeSerializedPropertyPath(PropertyPath);
                throw new Exception($"Serialized property '{PropertyPath}' not found on '{ComponentTypeName}'. Normalized attempt: '{normalized}'.");
            }

            return ActionEditingUtilities.GetSerializedPropertyValue(property);
        }
    }
}
