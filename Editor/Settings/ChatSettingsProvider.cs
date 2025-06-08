using GPTUnity.Settings;
using UnityEditor;

namespace GPTUnity.Data
{
    public class ChatSettingsProvider
    {
        private static readonly string PreferencesPath = "Project/Chat Settings";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(PreferencesPath, SettingsScope.Project)
            {
                label = "Chat Settings",
                guiHandler = (searchContext) =>
                {
                    var settings = ChatSettings.instance;
                    EditorGUI.BeginChangeCheck();

                    // Color fields
                    var newColorBackgroundUser =
                        EditorGUILayout.ColorField("User Background Color", settings.ColorBackgroundUser);
                    var newColorBackgroundAssistant = EditorGUILayout.ColorField("Assistant Background Color",
                        settings.ColorBackgroundAssistant);
                    var newColorChatBackground =
                        EditorGUILayout.ColorField("Chat Background Color", settings.ColorChatBackground);

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ColorBackgroundUser = newColorBackgroundUser;
                        settings.ColorBackgroundAssistant = newColorBackgroundAssistant;
                        settings.ColorChatBackground = newColorChatBackground;
                    }
                }
            };
        }
    }
}