using UnityEditor;
using UnityEngine;

namespace GPTUnity.Settings
{
    [FilePath("ProjectSettings/ChatSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ChatSettings : ScriptableSingleton<ChatSettings>
    {
        [SerializeField] private string apiKey = "";

        [SerializeField] private Color colorHighlight = Color.cyan;
        [SerializeField] private Color colorBackgroundUser = new Color(0.2f, 0.4f, 0.7f);
        [SerializeField] private Color colorBackgroundAssistant = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color colorChatBackground = new Color(0.1f, 0.1f, 0.1f);

        public string ApiKey
        {
            get => apiKey;
            set
            {
                apiKey = value;
                Save(true);
            }
        }

        public Color ColorHighlight
        {
            get => colorHighlight;
            set => colorHighlight = value;
        }

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