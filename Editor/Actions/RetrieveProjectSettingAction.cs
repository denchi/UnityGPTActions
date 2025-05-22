using System;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves a value from a Project Setting asset.")]
    public class RetrieveProjectSettingAction : GPTActionBase
    {
        [GPTParameter("Settings asset name, e.g. 'ProjectSettings/PlayerSettings.asset'")]
        public string AssetPath { get; set; }

        [GPTParameter("Serialized property path, e.g. 'productName'")]
        public string PropertyPath { get; set; }

        private string _value;

        public override string Content =>
            $"'{AssetPath}' property '{PropertyPath}' value: {_value}";
        
        public override string Description =>
            $"'{Highlight(AssetPath)}' property '{Highlight(PropertyPath)}' value: {Highlight(_value)}";

        public override void Execute()
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            if (assets == null || assets.Length == 0)
                throw new Exception($"Could not find asset at: {AssetPath}");

            var so = new UnityEditor.SerializedObject(assets[0]);
            var prop = so.FindProperty(PropertyPath);
            if (prop == null)
                throw new Exception($"Could not find property: {PropertyPath}. To see available properties try to dump all properties action!");

            _value = GetPropertyValueAsString(prop);
#endif
        }

        private string GetPropertyValueAsString(UnityEditor.SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    if (property.type == "Enum")
                    {
                        int idx = property.enumValueIndex;
                        return $"{property.enumNames[idx]} ({idx})";
                    }

                    return property.intValue.ToString();
                case UnityEditor.SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case UnityEditor.SerializedPropertyType.Float:
                    return property.floatValue.ToString();
                case UnityEditor.SerializedPropertyType.String:
                    return property.stringValue;
                case UnityEditor.SerializedPropertyType.Enum:
                    int enumIdx = property.enumValueIndex;
                    return $"{property.enumNames[enumIdx]} ({enumIdx})";
                default:
                    return "(unsupported type)";
            }
        }
    }
}