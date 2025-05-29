using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPTUnity.Actions
{
    [GPTAction("Adjusts a serialized field on a component.")]
    public class AdjustSerializedFieldAction : GPTActionBase
    {
        [GPTParameter("GameObject name")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type name")]
        public string ComponentTypeName { get; set; }

        [GPTParameter("Field/property name")]
        public string FieldName { get; set; }

        [GPTParameter("New value as string")]
        public string Value { get; set; }
        
        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");
            
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"aAmeObject '{ObjectName}' not found.");

            var comp = go.GetComponent(type);
            if (!comp)
                throw new Exception($"GameObject '{ObjectName}' does not have a '{ComponentTypeName}' component.");

            var field = type.GetField(FieldName) ?? (object)type.GetProperty(FieldName);
            if (field == null)
                throw new Exception($"Field/Property '{FieldName}' not found in component '{ComponentTypeName}'.");

            // Very naive approach to convert the string value

            if (field is System.Reflection.FieldInfo fieldInfo)
            {
                var converted = ConvertValue(Value, fieldInfo.FieldType);
                fieldInfo.SetValue(comp, converted);
            }
            else if (field is System.Reflection.PropertyInfo propInfo)
            {
                var converted = ConvertValue(Value, propInfo.PropertyType);
                propInfo.SetValue(comp, converted);
            }

            EditorUtility.SetDirty(go);

            return $"Set field '{FieldName}' to '{Value}' on '{ObjectName}'";
        }
        
        private object ConvertValue(string value, Type targetType, Component context = null)
        {
            if (string.IsNullOrEmpty(value))
                return null;
        
            // Handle primitive types
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(Vector2)) return ParseVector2(value);
            if (targetType == typeof(Vector3)) return ParseVector3(value);
            if (targetType == typeof(Color)) return ParseColor(value);
            
            // Enum
            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, out var result))
                {
                    return result;
                }
                
                throw new Exception($"Can not convert value {value} to enum {targetType.Name}");
            }
        
            // Handle Unity reference types
            if (typeof(Object).IsAssignableFrom(targetType))
            {
                // Try scene path (GameObject/Component)
                if (value.StartsWith("/"))
                {
                    var parts = value.TrimStart('/').Split('/');
                    var current = GameObject.Find(parts[0]);
                    
                    for (var i = 1; i < parts.Length && current != null; i++)
                    {
                        if (i == parts.Length - 1 && typeof(Component).IsAssignableFrom(targetType))
                        {
                            var comp = current.GetComponent(targetType);
                            if (comp != null) return comp;
                        }
                        else
                        {
                            var child = current.transform.Find(parts[i]);
                            if (child == null) break;
                            current = child.gameObject;
                        }
                    }
                    
                    if (targetType == typeof(GameObject) && current != null)
                        return current;
                }
                
                // If GameObject - Try find in scene by name
                if (targetType == typeof(GameObject))
                {
                    if (UnityAiHelpers.TryFindGameObject(value, out var go)) 
                        return go;
                }
                
                // Try component on current GameObject
                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    if (!UnityAiHelpers.TryFindGameObject(value, out var go)) 
                        throw new Exception($"Can not find gameObject {value}");
                    
                    var comp = go.GetComponent(targetType);
                    if (comp != null) 
                        return comp;
                }
        
#if UNITY_EDITOR
                
                // Try asset path
                if (value.StartsWith("Assets/"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath(value, targetType);
                    if (asset != null) return asset;
                }
                
                // Try finding by name in project
                var guids = AssetDatabase.FindAssets($"{value} t:{targetType.Name}");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath(path, targetType);
                }
                
#endif
        
                // Value is a Resource Asset Path
                var resource = Resources.Load(value, targetType);
                if (resource != null) return resource;
            }
        
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