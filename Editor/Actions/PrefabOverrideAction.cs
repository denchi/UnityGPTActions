using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Checks, sets, or clears prefab instance overrides for a serialized property path. Use this for advanced prefab override management.", Name = "manage_prefab_override")]
    public class PrefabOverrideAction : GPTAssistantAction
    {
        public enum OverrideOperation
        {
            Check,
            Set,
            Clear
        }

        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Component type name.", true, Name = "component_type_name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("SerializedProperty path to inspect or override.", true, Name = "property_path")]
        public string PropertyPath { get; set; }

        [GPTParameter("Override operation to perform: Check, Set, or Clear.", true, Name = "operation")]
        public OverrideOperation Operation { get; set; } = OverrideOperation.Check;

        [GPTParameter("Value to assign when Operation is Set.", Name = "value")]
        public string Value { get; set; }

        public override async Task<string> Execute()
        {
            var component = ResolveComponent();

            if (!PrefabUtility.IsPartOfPrefabInstance(component))
                throw new Exception($"GameObject '{ObjectNameOrPath}' is not part of a prefab instance.");

            var serializedObject = new SerializedObject(component);
            var property = ActionEditingUtilities.FindPropertyWithAliases(serializedObject, PropertyPath);
            if (property == null)
            {
                var normalized = ActionEditingUtilities.NormalizeSerializedPropertyPath(PropertyPath);
                throw new Exception($"Serialized property '{PropertyPath}' not found. Normalized attempt: '{normalized}'.");
            }

            switch (Operation)
            {
                case OverrideOperation.Check:
                    return property.prefabOverride
                        ? $"Override exists for '{property.propertyPath}'."
                        : $"No override for '{property.propertyPath}'.";

                case OverrideOperation.Set:
                    if (Value == null)
                        throw new Exception("Value is required for the Set operation.");
                    ActionEditingUtilities.SetSerializedPropertyValue(property, Value);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    return $"Set override for '{property.propertyPath}' to '{Value}'.";

                case OverrideOperation.Clear:
                    PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
                    return $"Cleared override for '{property.propertyPath}'.";

                default:
                    throw new Exception($"Unsupported operation '{Operation}'.");
            }
        }

        private Component ResolveComponent()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectNameOrPath, out var gameObject))
                throw new Exception($"GameObject '{ObjectNameOrPath}' not found.");

            var component = gameObject.GetComponent(type);
            if (!component)
                throw new Exception($"GameObject '{ObjectNameOrPath}' does not have component '{ComponentTypeName}'.");

            return component;
        }
    }
}
