using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPTUnity.Helpers
{
    internal static class ActionEditingUtilities
    {
        private static readonly BindingFlags PublicInstanceIgnoreCase =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        public static string NormalizeSerializedPropertyPath(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return propertyPath;

            var normalized = propertyPath.Trim();

            // Supports user-friendly notation: listField[0] -> listField.Array.data[0]
            normalized = Regex.Replace(normalized, @"\[(\d+)\]", ".Array.data[$1]");

            // Supports user-friendly notation: listField.size -> listField.Array.size
            normalized = Regex.Replace(normalized, @"(?<!Array)\.size\b", ".Array.size");

            return normalized;
        }

        public static SerializedProperty FindPropertyWithAliases(SerializedObject serializedObject, string propertyPath)
        {
            if (serializedObject == null)
                return null;

            var direct = serializedObject.FindProperty(propertyPath);
            if (direct != null)
                return direct;

            var normalizedPath = NormalizeSerializedPropertyPath(propertyPath);
            if (string.Equals(normalizedPath, propertyPath, StringComparison.Ordinal))
                return null;

            return serializedObject.FindProperty(normalizedPath);
        }

        public static string ResolveMemberName(string memberName, string legacyFieldName)
        {
            if (!string.IsNullOrWhiteSpace(memberName))
                return memberName.Trim();

            return legacyFieldName?.Trim();
        }

        public static bool TryGetPublicFieldOrProperty(Type type, string memberName, out FieldInfo field, out PropertyInfo property)
        {
            field = type.GetField(memberName, PublicInstanceIgnoreCase);
            if (field != null)
            {
                property = null;
                return true;
            }

            property = type.GetProperty(memberName, PublicInstanceIgnoreCase);
            return property != null;
        }

        public static object ConvertStringToType(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (IsNullToken(value))
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;

                throw new Exception($"Cannot assign null to value type '{targetType.Name}'.");
            }

            var nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (nonNullable == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);
            if (nonNullable == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);
            if (nonNullable == typeof(float))
                return float.Parse(value, CultureInfo.InvariantCulture);
            if (nonNullable == typeof(double))
                return double.Parse(value, CultureInfo.InvariantCulture);
            if (nonNullable == typeof(decimal))
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            if (nonNullable == typeof(bool))
                return bool.Parse(value);
            if (nonNullable == typeof(char))
                return value[0];

            if (nonNullable.IsEnum)
            {
                if (Enum.TryParse(nonNullable, value, true, out var enumValue))
                    return enumValue;

                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var enumIndex))
                    return Enum.ToObject(nonNullable, enumIndex);

                throw new Exception($"Cannot parse '{value}' as enum '{nonNullable.Name}'.");
            }

            if (nonNullable == typeof(Vector2))
                return ParseVector2(value);
            if (nonNullable == typeof(Vector2Int))
                return ParseVector2Int(value);
            if (nonNullable == typeof(Vector3))
                return ParseVector3(value);
            if (nonNullable == typeof(Vector3Int))
                return ParseVector3Int(value);
            if (nonNullable == typeof(Vector4))
                return ParseVector4(value);
            if (nonNullable == typeof(Color))
                return ParseColor(value);
            if (nonNullable == typeof(Quaternion))
                return ParseQuaternion(value);

            if (typeof(Object).IsAssignableFrom(nonNullable))
            {
                if (TryResolveUnityObject(value, nonNullable, out var obj))
                    return obj;

                throw new Exception($"Could not resolve Unity object reference '{value}' for type '{nonNullable.Name}'.");
            }

            throw new Exception($"Unsupported conversion to type '{nonNullable.Name}'.");
        }

        public static void SetSerializedPropertyValue(SerializedProperty property, string value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (property.type == "long" || property.type == "ulong")
                    {
                        property.longValue = long.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        property.intValue = int.Parse(value, CultureInfo.InvariantCulture);
                    }
                    break;

                case SerializedPropertyType.Boolean:
                    property.boolValue = bool.Parse(value);
                    break;

                case SerializedPropertyType.Float:
                    property.floatValue = float.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case SerializedPropertyType.String:
                    property.stringValue = IsNullToken(value) ? string.Empty : value;
                    break;

                case SerializedPropertyType.Color:
                    property.colorValue = ParseColor(value);
                    break;

                case SerializedPropertyType.ObjectReference:
                    if (IsNullToken(value))
                    {
                        property.objectReferenceValue = null;
                        break;
                    }

                    if (TryResolveUnityObject(value, typeof(Object), out var referenceObject))
                    {
                        property.objectReferenceValue = referenceObject;
                        break;
                    }

                    throw new Exception($"Could not resolve object reference value '{value}'.");

                case SerializedPropertyType.LayerMask:
                    property.intValue = int.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case SerializedPropertyType.Enum:
                    SetEnumValue(property, value);
                    break;

                case SerializedPropertyType.Vector2:
                    property.vector2Value = ParseVector2(value);
                    break;

                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = ParseVector2Int(value);
                    break;

                case SerializedPropertyType.Vector3:
                    property.vector3Value = ParseVector3(value);
                    break;

                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = ParseVector3Int(value);
                    break;

                case SerializedPropertyType.Vector4:
                    property.vector4Value = ParseVector4(value);
                    break;

                case SerializedPropertyType.Rect:
                    property.rectValue = ParseRect(value);
                    break;

                case SerializedPropertyType.RectInt:
                    property.rectIntValue = ParseRectInt(value);
                    break;

                case SerializedPropertyType.Bounds:
                    property.boundsValue = ParseBounds(value);
                    break;

                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = ParseBoundsInt(value);
                    break;

                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = ParseQuaternion(value);
                    break;

                case SerializedPropertyType.Character:
                    property.intValue = string.IsNullOrEmpty(value) ? 0 : value[0];
                    break;

                default:
                    throw new Exception(
                        $"Unsupported serialized property type '{property.propertyType}' for path '{property.propertyPath}'.");
            }
        }

        public static string GetSerializedPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.type == "long" || property.type == "ulong"
                        ? property.longValue.ToString(CultureInfo.InvariantCulture)
                        : property.intValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();

                case SerializedPropertyType.Float:
                    return property.floatValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.String:
                    return property.stringValue;

                case SerializedPropertyType.Color:
                    return $"#{ColorUtility.ToHtmlStringRGBA(property.colorValue)}";

                case SerializedPropertyType.ObjectReference:
                    return DescribeObjectReference(property.objectReferenceValue);

                case SerializedPropertyType.LayerMask:
                    return property.intValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames != null &&
                           property.enumValueIndex >= 0 &&
                           property.enumValueIndex < property.enumDisplayNames.Length
                        ? property.enumDisplayNames[property.enumValueIndex]
                        : property.enumValueIndex.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();

                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue.ToString();

                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();

                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue.ToString();

                case SerializedPropertyType.Vector4:
                    return property.vector4Value.ToString();

                case SerializedPropertyType.Rect:
                    return property.rectValue.ToString();

                case SerializedPropertyType.RectInt:
                    return property.rectIntValue.ToString();

                case SerializedPropertyType.Bounds:
                    return property.boundsValue.ToString();

                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue.ToString();

                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue.eulerAngles.ToString();

                case SerializedPropertyType.ArraySize:
                    return property.intValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.Generic:
                    return property.isArray
                        ? $"Array(size={property.arraySize})"
                        : $"Generic({property.type})";

                default:
                    return $"<{property.propertyType}>";
            }
        }

        public static string GetGameObjectHierarchyPath(GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;

            var current = gameObject.transform;
            var path = current.name;

            while (current.parent != null)
            {
                current = current.parent;
                path = $"{current.name}/{path}";
            }

            return "/" + path;
        }

        public static bool TryResolveUnityObject(string value, Type targetType, out Object obj)
        {
            obj = null;

            if (IsNullToken(value))
                return true;

            if (targetType == null || !typeof(Object).IsAssignableFrom(targetType))
                targetType = typeof(Object);

            // Scene object path or name
            try
            {
                if (UnityAiHelpers.TryFindGameObject(value, out var gameObject))
                {
                    if (targetType == typeof(GameObject) || targetType == typeof(Object))
                    {
                        obj = gameObject;
                        return true;
                    }

                    if (typeof(Component).IsAssignableFrom(targetType))
                    {
                        obj = gameObject.GetComponent(targetType);
                        return obj != null;
                    }
                }
            }
            catch
            {
                // Ignore lookup failures and continue with other resolvers.
            }

#if UNITY_EDITOR
            if (value.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                var loaded = AssetDatabase.LoadAssetAtPath(value, targetType);
                if (loaded != null)
                {
                    obj = loaded;
                    return true;
                }
            }

            // Allow GUID input.
            if (Regex.IsMatch(value, "^[a-fA-F0-9]{32}$"))
            {
                var guidPath = AssetDatabase.GUIDToAssetPath(value);
                if (!string.IsNullOrWhiteSpace(guidPath))
                {
                    var loaded = AssetDatabase.LoadAssetAtPath(guidPath, targetType);
                    if (loaded != null)
                    {
                        obj = loaded;
                        return true;
                    }
                }
            }

            var searchType = targetType == typeof(Object) ? string.Empty : $" t:{targetType.Name}";
            var guids = AssetDatabase.FindAssets($"{value}{searchType}");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var loaded = AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                if (loaded != null)
                {
                    obj = loaded;
                    return true;
                }
            }
#endif

            var resource = Resources.Load(value, targetType);
            if (resource != null)
            {
                obj = resource;
                return true;
            }

            return false;
        }

        private static string DescribeObjectReference(Object value)
        {
            if (value == null)
                return "null";

#if UNITY_EDITOR
            var assetPath = AssetDatabase.GetAssetPath(value);
            if (!string.IsNullOrWhiteSpace(assetPath))
                return assetPath;
#endif

            if (value is Component component)
                return $"{GetGameObjectHierarchyPath(component.gameObject)}#{component.GetType().Name}";

            if (value is GameObject gameObject)
                return GetGameObjectHierarchyPath(gameObject);

            return value.name;
        }

        private static bool IsNullToken(string value)
        {
            if (value == null)
                return true;

            var trimmed = value.Trim();
            return string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetEnumValue(SerializedProperty property, string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
            {
                if (numeric >= 0 && numeric < property.enumNames.Length)
                {
                    property.enumValueIndex = numeric;
                    return;
                }

                throw new Exception($"Enum index '{numeric}' is out of range for '{property.displayName}'.");
            }

            for (var i = 0; i < property.enumNames.Length; i++)
            {
                if (string.Equals(property.enumNames[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    property.enumValueIndex = i;
                    return;
                }
            }

            throw new Exception(
                $"Invalid enum value '{value}' for '{property.displayName}'. Valid values: {string.Join(", ", property.enumNames)}");
        }

        private static float[] ParseNumbers(string value, int expectedCount)
        {
            var parts = value.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            if (parts.Length != expectedCount)
                throw new Exception($"Expected {expectedCount} comma-separated numbers but got {parts.Length} from '{value}'.");

            var numbers = new float[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                numbers[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            }

            return numbers;
        }

        private static Vector2 ParseVector2(string value)
        {
            var numbers = ParseNumbers(value, 2);
            return new Vector2(numbers[0], numbers[1]);
        }

        private static Vector2Int ParseVector2Int(string value)
        {
            var numbers = ParseNumbers(value, 2);
            return new Vector2Int((int)numbers[0], (int)numbers[1]);
        }

        private static Vector3 ParseVector3(string value)
        {
            var numbers = ParseNumbers(value, 3);
            return new Vector3(numbers[0], numbers[1], numbers[2]);
        }

        private static Vector3Int ParseVector3Int(string value)
        {
            var numbers = ParseNumbers(value, 3);
            return new Vector3Int((int)numbers[0], (int)numbers[1], (int)numbers[2]);
        }

        private static Vector4 ParseVector4(string value)
        {
            var numbers = ParseNumbers(value, 4);
            return new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        private static Quaternion ParseQuaternion(string value)
        {
            var parts = value.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            if (parts.Length == 3)
            {
                var euler = ParseVector3(value);
                return Quaternion.Euler(euler);
            }

            if (parts.Length == 4)
            {
                var numbers = ParseNumbers(value, 4);
                return new Quaternion(numbers[0], numbers[1], numbers[2], numbers[3]);
            }

            throw new Exception("Quaternion value must have either 3 (Euler) or 4 (x,y,z,w) comma-separated numbers.");
        }

        private static Color ParseColor(string value)
        {
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                if (ColorUtility.TryParseHtmlString(value, out var color))
                    return color;

                throw new Exception($"Invalid html color value '{value}'.");
            }

            var parts = value.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            if (parts.Length < 3 || parts.Length > 4)
                throw new Exception("Color value must be 'r,g,b' or 'r,g,b,a'.");

            var r = float.Parse(parts[0], CultureInfo.InvariantCulture);
            var g = float.Parse(parts[1], CultureInfo.InvariantCulture);
            var b = float.Parse(parts[2], CultureInfo.InvariantCulture);
            var a = parts.Length == 4 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 1f;

            return new Color(r, g, b, a);
        }

        private static Rect ParseRect(string value)
        {
            var numbers = ParseNumbers(value, 4);
            return new Rect(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        private static RectInt ParseRectInt(string value)
        {
            var numbers = ParseNumbers(value, 4);
            return new RectInt((int)numbers[0], (int)numbers[1], (int)numbers[2], (int)numbers[3]);
        }

        private static Bounds ParseBounds(string value)
        {
            var numbers = ParseNumbers(value, 6);
            return new Bounds(
                new Vector3(numbers[0], numbers[1], numbers[2]),
                new Vector3(numbers[3], numbers[4], numbers[5]));
        }

        private static BoundsInt ParseBoundsInt(string value)
        {
            var numbers = ParseNumbers(value, 6);
            return new BoundsInt(
                new Vector3Int((int)numbers[0], (int)numbers[1], (int)numbers[2]),
                new Vector3Int((int)numbers[3], (int)numbers[4], (int)numbers[5]));
        }
    }
}
