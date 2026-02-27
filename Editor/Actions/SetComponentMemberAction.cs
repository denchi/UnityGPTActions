using System;
using System.Reflection;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace GPTUnity.Actions
{
    [GPTAction("Sets a public writable field/property on a component using reflection.")]
    public class SetComponentMemberAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("Component member name (public field/property)")]
        public string MemberName { get; set; }

        [GPTParameter("Legacy alias for MemberName")]
        public string FieldName { get; set; }

        [GPTParameter("New value as string")]
        public string Value { get; set; }

        public override async Task<string> Execute()
        {
            var resolvedMemberName = ActionEditingUtilities.ResolveMemberName(MemberName, FieldName);
            if (string.IsNullOrWhiteSpace(resolvedMemberName))
                throw new Exception("MemberName (or FieldName) is required.");

            var component = ResolveComponent();
            var componentType = component.GetType();

            if (!ActionEditingUtilities.TryGetPublicFieldOrProperty(componentType, resolvedMemberName, out var field, out var property))
                throw new Exception($"Public field/property '{resolvedMemberName}' was not found on '{componentType.Name}'.");

            if (field != null)
            {
                if (!field.IsPublic)
                    throw new Exception($"Field '{resolvedMemberName}' is not public. Use SetSerializedPropertyByPathAction.");

                var converted = ActionEditingUtilities.ConvertStringToType(Value, field.FieldType);
                field.SetValue(component, converted);
            }
            else
            {
                SetPropertyValue(property, component, resolvedMemberName);
            }

            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }

            return $"Set member '{resolvedMemberName}' on '{ObjectName}' ({ComponentTypeName}) to '{Value}'.";
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

        private void SetPropertyValue(PropertyInfo property, Component component, string resolvedMemberName)
        {
            if (property == null)
                throw new Exception($"Property '{resolvedMemberName}' not found.");

            if (property.GetIndexParameters().Length > 0)
                throw new Exception($"Indexed property '{resolvedMemberName}' is not supported.");

            var setter = property.GetSetMethod();
            if (setter == null)
                throw new Exception($"Property '{resolvedMemberName}' is read-only. Use SetSerializedPropertyByPathAction for serialized backing fields.");

            if (!setter.IsPublic)
                throw new Exception($"Property '{resolvedMemberName}' has a non-public setter. Use SetSerializedPropertyByPathAction.");

            var converted = ActionEditingUtilities.ConvertStringToType(Value, property.PropertyType);
            property.SetValue(component, converted);
        }
    }
}
