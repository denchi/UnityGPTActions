using UnityEditor;

namespace GptActions.Editor.AssetIndexer
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "OpenAIFileTracker", menuName = "Tools/OpenAI File Tracker")]
    public class OpenAIFileTracker : ScriptableObject
    {
        public string lastFileId;

        private const string TrackerPath = "Assets/Editor/AssetIndexer/OpenAIFileTracker.asset";
        private static OpenAIFileTracker _instance;
        public static OpenAIFileTracker Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<OpenAIFileTracker>(TrackerPath);
                    if (_instance == null)
                    {
                        Debug.LogError("❌ OpenAIFileTracker not found in Resources folder.");
                        return null;
                    }
                }

                return _instance;
            }
        }
    }
}