using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Sets a serialized property on a component by SerializedProperty path. Use this for advanced serialized edits when higher-level component actions are insufficient.", Name = "set_serialized_property")]
    public class SetSerializedPropertyByPathAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Component type name.", true, Name = "component_type_name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("SerializedProperty path, for example '_assets.Array.data[0]' or '_assets[0]'.", true, Name = "property_path")]
        public string PropertyPath { get; set; }

        [GPTParameter("New value to assign to the serialized property.", true, Name = "value")]
        public string Value { get; set; }

        [GPTParameter("Record a prefab override when the target component belongs to a prefab instance.", Name = "record_prefab_override")]
        public bool RecordPrefabOverride { get; set; } = true;

        public override async Task<string> Execute()
        {
            var component = ResolveComponent();
            var serializedObject = new SerializedObject(component);
            var property = ActionEditingUtilities.FindPropertyWithAliases(serializedObject, PropertyPath);

            if (property == null)
            {
                var normalized = ActionEditingUtilities.NormalizeSerializedPropertyPath(PropertyPath);
                throw new Exception($"Serialized property '{PropertyPath}' not found on '{ComponentTypeName}'. Normalized attempt: '{normalized}'.");
            }

            ActionEditingUtilities.SetSerializedPropertyValue(property, Value);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (RecordPrefabOverride && PrefabUtility.IsPartOfPrefabInstance(component))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }

            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }

            return $"Set serialized property '{property.propertyPath}' on '{ObjectNameOrPath}' ({ComponentTypeName}) to '{Value}'.";
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
