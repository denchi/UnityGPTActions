using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPTUnity.Helpers
{
    public class IconsHelpers
    {
        public Image GetImage(string role)
        {
            var icon = new Image();
            switch (role)
            {
                case "user":
                    icon.image = LoadEditorIcon("person");
                    break;
                case "assistant":
                    icon.image = LoadEditorIcon("cognition");
                    break;
                case "system":
                    icon.image = LoadEditorIcon("psychology");
                    break;
                case "tool":
                    icon.image = LoadEditorIcon("build");
                    break;
                default:
                    icon.image = LoadUnityIcon("d_UnityEditor.ProjectBrowser");
                    break;
            }

            icon.style.width = 24;
            icon.style.height = 24;
            icon.style.marginRight = 5;
            icon.style.marginTop = 5;
            return icon;
        }

        public Texture2D LoadEditorIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/ChatGPTAssistant/Editor/Icons/{name}.png");
        }

        public Texture2D LoadUnityIcon(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}