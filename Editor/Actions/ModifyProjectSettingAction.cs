using System;
using System.Threading.Tasks;

namespace GPTUnity.Actions
{
    [GPTAction("Sets a serialized value in a Unity ProjectSettings asset by property path.", Name = "set_project_setting")]
    public class ModifyProjectSettingAction : GPTAssistantAction
    {
        [GPTParameter("ProjectSettings asset path, for example 'ProjectSettings/PlayerSettings.asset'.", true, Name = "asset_path")]
        public string AssetPath { get; set; }

        [GPTParameter("Serialized property path to modify, for example 'productName'.", true, Name = "property_path")]
        public string PropertyPath { get; set; }

        [GPTParameter("New value to assign, serialized as text.", true, Name = "value")] public string Value { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            if (assets == null || assets.Length == 0)
                throw new Exception($"Could not find asset at: {AssetPath}");

            var so = new UnityEditor.SerializedObject(assets[0]);
            var prop = so.FindProperty(PropertyPath);
            if (prop == null)
                throw new Exception($"Could not find property: {PropertyPath}");

            SetPropertyValue(prop, Value);
            so.ApplyModifiedProperties();
            
            return $"Set '{AssetPath}' property '{PropertyPath}' to {Value}";
#endif
        }

        private void SetPropertyValue(UnityEditor.SerializedProperty property, string value)
        {
            switch (property.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    if (property.type == "Enum")
                    {
                        // Try set by enum name or index
                        int idx = Array.IndexOf(property.enumNames, value);
                        if (idx >= 0)
                        {
                            property.enumValueIndex = idx;
                        }
                        else if (int.TryParse(value, out int enumIdx) && enumIdx >= 0 &&
                                 enumIdx < property.enumNames.Length)
                        {
                            property.enumValueIndex = enumIdx;
                        }
                        else
                        {
                            throw new Exception(
                                $"Invalid enum value '{value}' for {property.displayName}. Valid: {string.Join(", ", property.enumNames)}");
                        }
                    }
                    else if (int.TryParse(value, out int i))
                    {
                        property.intValue = i;
                    }

                    break;
                case UnityEditor.SerializedPropertyType.Boolean:
                    if (bool.TryParse(value, out bool b)) property.boolValue = b;
                    break;
                case UnityEditor.SerializedPropertyType.Float:
                    if (float.TryParse(value, out float f)) property.floatValue = f;
                    break;
                case UnityEditor.SerializedPropertyType.String:
                    property.stringValue = value;
                    break;
                case UnityEditor.SerializedPropertyType.Enum:
                    // Try set by enum name or index
                    int idx2 = Array.IndexOf(property.enumNames, value);
                    if (idx2 >= 0)
                    {
                        property.enumValueIndex = idx2;
                    }
                    else if (int.TryParse(value, out int enumIdx2) && enumIdx2 >= 0 &&
                             enumIdx2 < property.enumNames.Length)
                    {
                        property.enumValueIndex = enumIdx2;
                    }
                    else
                    {
                        throw new Exception(
                            $"Invalid enum value '{value}' for {property.displayName}. Valid: {string.Join(", ", property.enumNames)}");
                    }

                    break;
                default:
                    throw new Exception("Unsupported property type");
            }
        }
    }
}
