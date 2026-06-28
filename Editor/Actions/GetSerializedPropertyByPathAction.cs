using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Gets a serialized property value from a component by SerializedProperty path. Use this as the inspection companion to set_serialized_property.", Name = "get_serialized_property")]
    public class GetSerializedPropertyByPathAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Component type name.", true, Name = "component_type_name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("SerializedProperty path, for example '_assets.Array.data[0]' or '_assets[0]'.", true, Name = "property_path")]
        public string PropertyPath { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectNameOrPath, out var gameObject))
                throw new Exception($"GameObject '{ObjectNameOrPath}' not found.");

            var component = gameObject.GetComponent(type);
            if (!component)
                throw new Exception($"GameObject '{ObjectNameOrPath}' does not have component '{ComponentTypeName}'.");

            var serializedObject = new SerializedObject(component);
            var property = ActionEditingUtilities.FindPropertyWithAliases(serializedObject, PropertyPath);
            if (property == null)
            {
                var normalized = ActionEditingUtilities.NormalizeSerializedPropertyPath(PropertyPath);
                throw new Exception($"Serialized property '{PropertyPath}' not found on '{ComponentTypeName}'. Normalized attempt: '{normalized}'.");
            }

            var value = ActionEditingUtilities.GetSerializedPropertyValue(property);
            return $"component: {ComponentTypeName}\nobject: {ObjectNameOrPath}\nrequestedPath: {PropertyPath}\nresolvedPath: {property.propertyPath}\nvalue: {value}";
        }
    }
}
