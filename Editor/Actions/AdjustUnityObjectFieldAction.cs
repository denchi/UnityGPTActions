using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPTUnity.Actions
{
    [GPTAction("Adjusts a serialized field or property on any UnityEngine.Object asset.")]
    public class AdjustUnityObjectFieldAction : GPTAssistantAction
    {
        [GPTParameter("The asset path (e.g., 'Assets/MyAsset.asset'")]
        public string ObjectIdentifier { get; set; }

        [GPTParameter("The type name of the object (e.g., 'UnityEngine.Material')")]
        public string ObjectTypeName { get; set; }

        [GPTParameter("Field/property name")]
        public string FieldName { get; set; }

        [GPTParameter("New value as string")]
        public string Value { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(ObjectIdentifier))
                throw new Exception("ObjectIdentifier cannot be empty.");
            
            if (string.IsNullOrWhiteSpace(ObjectTypeName))
                throw new Exception("ObjectTypeName cannot be empty.");
            
            if (string.IsNullOrWhiteSpace(FieldName))
                throw new Exception("FieldName cannot be empty.");

            if (!UnityAiHelpers.TryGetObjectTypeByType(ObjectTypeName, out var type))
                throw new Exception($"Type '{ObjectTypeName}' not found.");
            
            if (type == null || !typeof(UnityEngine.Object).IsAssignableFrom(type))
                throw new Exception($"Type '{ObjectTypeName}' not found or is not a UnityEngine.Object.");

            UnityEngine.Object target = null;

            // Try asset path
            if (!ObjectIdentifier.StartsWith("Assets/"))
                throw new Exception($"ObjectIdentifier '{ObjectIdentifier}' must start with 'Assets/'.");
            
            target = AssetDatabase.LoadAssetAtPath(ObjectIdentifier, type);
            
            if (target == null)
                throw new Exception($"Could not find object '{ObjectIdentifier}' of type '{ObjectTypeName}'.");

            var field = type.GetField(FieldName) ?? (object)type.GetProperty(FieldName);
            if (field == null)
                throw new Exception($"Field/Property '{FieldName}' not found in '{ObjectTypeName}'.");

            if (field is System.Reflection.FieldInfo fieldInfo)
            {
                var converted = ConvertValue(Value, fieldInfo.FieldType);
                fieldInfo.SetValue(target, converted);
            }
            else if (field is System.Reflection.PropertyInfo propInfo)
            {
                var converted = ConvertValue(Value, propInfo.PropertyType);
                propInfo.SetValue(target, converted);
            }

            EditorUtility.SetDirty(target);

            return $"Set field '{FieldName}' to '{Value}' on '{ObjectIdentifier}'";
#else
            throw new Exception("This action can only be executed in the Unity Editor.");
#endif
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(Vector2)) return ParseVector2(value);
            if (targetType == typeof(Vector3)) return ParseVector3(value);
            if (targetType == typeof(Color)) return ParseColor(value);

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, out var result))
                    return result;
                throw new Exception($"Cannot convert value {value} to enum {targetType.Name}");
            }

#if UNITY_EDITOR
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                if (value.StartsWith("Assets/"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath(value, targetType);
                    if (asset != null) return asset;
                }
                var guids = AssetDatabase.FindAssets($"{value} t:{targetType.Name}");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath(path, targetType);
                }
            }
#endif

            throw new Exception($"Cannot convert value '{value}' to type {targetType}");
        }

        private Vector2 ParseVector2(string value)
        {
            var parts = value.Split(',');
            return new Vector2(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim())
            );
        }

        private Vector3 ParseVector3(string value)
        {
            var parts = value.Split(',');
            return new Vector3(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim()),
                float.Parse(parts[2].Trim())
            );
        }

        private Color ParseColor(string value)
        {
            if (value.StartsWith("#"))
                return ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.white;

            var parts = value.Split(',');
            return new Color(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim()),
                float.Parse(parts[2].Trim()),
                parts.Length > 3 ? float.Parse(parts[3].Trim()) : 1f
            );
        }
    }
}
