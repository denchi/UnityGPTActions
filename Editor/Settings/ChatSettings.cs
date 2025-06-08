using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GPTUnity.Settings
{
    [FilePath("ProjectSettings/ChatSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ChatSettings : ScriptableSingleton<ChatSettings>
    {
        [SerializeField] private Color colorBackgroundUser = new Color(0.3098f, 0.3098f, 0.3098f, 1f);
        [SerializeField] private Color colorBackgroundAssistant = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color colorChatBackground = new Color(0.1686f, 0.1686f, 0.1686f, 1f);

        public Color ColorBackgroundUser
        {
            get => colorBackgroundUser;
            set => colorBackgroundUser = value;
        }

        public Color ColorBackgroundAssistant
        {
            get => colorBackgroundAssistant;
            set => colorBackgroundAssistant = value;
        }

        public Color ColorChatBackground
        {
            get => colorChatBackground;
            set => colorChatBackground = value;
        }
    }
}