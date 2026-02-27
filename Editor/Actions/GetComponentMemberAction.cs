using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Gets a public field/property value from a component using reflection.")]
    public class GetComponentMemberAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("Component member name (public field/property)")]
        public string MemberName { get; set; }

        [GPTParameter("Legacy alias for MemberName")]
        public string FieldName { get; set; }

        public override async Task<string> Execute()
        {
            var resolvedMemberName = ActionEditingUtilities.ResolveMemberName(MemberName, FieldName);
            if (string.IsNullOrWhiteSpace(resolvedMemberName))
                throw new Exception("MemberName (or FieldName) is required.");

            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var gameObject))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var component = gameObject.GetComponent(type);
            if (!component)
                throw new Exception($"GameObject '{ObjectName}' does not have component '{ComponentTypeName}'.");

            if (!ActionEditingUtilities.TryGetPublicFieldOrProperty(type, resolvedMemberName, out var field, out var property))
                throw new Exception($"Public field/property '{resolvedMemberName}' was not found on '{ComponentTypeName}'.");

            object value;
            if (field != null)
            {
                value = field.GetValue(component);
            }
            else
            {
                if (property.GetIndexParameters().Length > 0)
                    throw new Exception($"Indexed property '{resolvedMemberName}' is not supported.");

                var getter = property.GetGetMethod();
                if (getter == null || !getter.IsPublic)
                    throw new Exception($"Property '{resolvedMemberName}' is not publicly readable.");

                value = property.GetValue(component);
            }

            if (value == null)
                return "null";

            if (value is Component componentValue)
                return $"{ActionEditingUtilities.GetGameObjectHierarchyPath(componentValue.gameObject)}#{componentValue.GetType().Name}";

            if (value is GameObject gameObjectValue)
                return ActionEditingUtilities.GetGameObjectHierarchyPath(gameObjectValue);

            return value.ToString();
        }
    }
}
