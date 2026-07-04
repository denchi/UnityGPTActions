using System.Threading.Tasks;
using UnityEditor;

namespace GPTUnity.Actions
{
    [GPTAction("Enters Unity play mode if the editor is not already playing.", Name = "enter_play_mode")]
    public class EnterPlayModeAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                return "Unity is already in play mode.";

            EditorApplication.isPlaying = true;
            return "Entering play mode.";
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }
    }

    [GPTAction("Exits Unity play mode if the editor is currently playing.", Name = "exit_play_mode")]
    public class ExitPlayModeAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return "Unity is not currently in play mode.";

            EditorApplication.isPlaying = false;
            return "Exiting play mode.";
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }
    }
}
