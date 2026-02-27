using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Checks, sets, or clears prefab instance overrides for a serialized property path.")]
    public class PrefabOverrideAction : GPTAssistantAction
    {
        public enum OverrideOperation
        {
            Check,
            Set,
            Clear
        }

        [GPTParameter("GameObject name or hierarchy path")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("SerializedProperty path, e.g. '_assets.Array.data[0]'")]
        public string PropertyPath { get; set; }

        [GPTParameter("Operation: Check, Set, Clear")]
        public OverrideOperation Operation { get; set; } = OverrideOperation.Check;

        [GPTParameter("Value for Set operation")]
        public string Value { get; set; }

        public override async Task<string> Execute()
        {
            var component = ResolveComponent();

            if (!PrefabUtility.IsPartOfPrefabInstance(component))
                throw new Exception($"GameObject '{ObjectName}' is not part of a prefab instance.");

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

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var gameObject))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var component = gameObject.GetComponent(type);
            if (!component)
                throw new Exception($"GameObject '{ObjectName}' does not have component '{ComponentTypeName}'.");

            return component;
        }
    }
}
