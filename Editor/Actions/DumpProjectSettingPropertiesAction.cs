using System;
using System.Text;
using UnityEditor;

namespace GPTUnity.Actions
{
    [GPTAction("Dumps all serialized properties from a Project Setting asset.")]
    public class DumpProjectSettingPropertiesAction : GPTActionBase
    {
        [GPTParameter("Settings asset path, e.g. 'ProjectSettings/PlayerSettings.asset'")]
        public string AssetPath { get; set; }

        private string _content;

        public override string Content =>
            $"<b>Serialized properties in:</b> {Highlight(AssetPath)}\n\n<pre>{_content}</pre>";

        public override void Execute()
        {
#if UNITY_EDITOR
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            if (assets == null || assets.Length == 0)
                throw new Exception($"Could not find asset at: {AssetPath}");

            var sb = new StringBuilder();
            var so = new SerializedObject(assets[0]);
            var prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                sb.AppendLine($"{prop.propertyPath} = {GetValue(prop)}");
            }

            _content = sb.ToString();
#endif
        }

        private string GetValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
                case SerializedPropertyType.Float: return prop.floatValue.ToString("F3");
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Enum: return prop.enumNames[prop.enumValueIndex];
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue ? prop.objectReferenceValue.name : "null";
                default: return $"({prop.propertyType})";
            }
        }
    }
}