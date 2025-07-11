using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves the value of a serialized field or property on a component.")]
    public class GetSerializedFieldValueAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("Field/property name")]
        public string FieldName { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var comp = go.GetComponent(type);
            if (!comp)
                throw new Exception($"GameObject '{ObjectName}' does not have a '{ComponentTypeName}' component.");

            var field = type.GetField(FieldName) ?? (object)type.GetProperty(FieldName);
            if (field == null)
                throw new Exception($"Field/Property '{FieldName}' not found in component '{ComponentTypeName}'.");

            object value = null;
            if (field is System.Reflection.FieldInfo fieldInfo)
            {
                value = fieldInfo.GetValue(comp);
            }
            else if (field is System.Reflection.PropertyInfo propInfo)
            {
                value = propInfo.GetValue(comp);
            }

            return value != null ? value.ToString() : "null";
        }
    }
}

