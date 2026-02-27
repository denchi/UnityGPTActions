using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Sets a serialized property on a component by SerializedProperty path.")]
    public class SetSerializedPropertyByPathAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("SerializedProperty path, e.g. '_assets.Array.data[0]' or '_assets[0]'")]
        public string PropertyPath { get; set; }

        [GPTParameter("New value as string")]
        public string Value { get; set; }

        [GPTParameter("Record prefab override when target is a prefab instance")]
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

            return $"Set serialized property '{property.propertyPath}' on '{ObjectName}' ({ComponentTypeName}) to '{Value}'.";
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
