using System;
using System.Reflection;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace GPTUnity.Actions
{
    [GPTAction("Sets a public writable field or property on a component. Use this for high-level component edits before falling back to serialized-property tools.", Name = "set_component_member")]
    public class SetComponentMemberAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Component type name.", true, Name = "component_type_name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("Public writable field or property name on the component.", true, Name = "member_name")]
        public string MemberName { get; set; }

        [GPTParameter("New value to assign to the member.", true, Name = "value")]
        public string Value { get; set; }

        public override async Task<string> Execute()
        {
            var component = ResolveComponent();
            var componentType = component.GetType();

            if (!ActionEditingUtilities.TryGetPublicFieldOrProperty(componentType, MemberName, out var field, out var property))
                throw new Exception($"Public field/property '{MemberName}' was not found on '{componentType.Name}'.");

            if (field != null)
            {
                if (!field.IsPublic)
                    throw new Exception($"Field '{MemberName}' is not public. Use set_serialized_property for advanced serialized edits.");

                var converted = ActionEditingUtilities.ConvertStringToType(Value, field.FieldType);
                field.SetValue(component, converted);
            }
            else
            {
                SetPropertyValue(property, component, MemberName);
            }

            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }

            return $"Set member '{MemberName}' on '{ObjectNameOrPath}' ({ComponentTypeName}) to '{Value}'.";
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

        private void SetPropertyValue(PropertyInfo property, Component component, string resolvedMemberName)
        {
            if (property == null)
                throw new Exception($"Property '{resolvedMemberName}' not found.");

            if (property.GetIndexParameters().Length > 0)
                throw new Exception($"Indexed property '{resolvedMemberName}' is not supported.");

            var setter = property.GetSetMethod();
            if (setter == null)
                throw new Exception($"Property '{resolvedMemberName}' is read-only. Use set_serialized_property for serialized backing fields.");

            if (!setter.IsPublic)
                throw new Exception($"Property '{resolvedMemberName}' has a non-public setter. Use set_serialized_property.");

            var converted = ActionEditingUtilities.ConvertStringToType(Value, property.PropertyType);
            property.SetValue(component, converted);
        }
    }
}
