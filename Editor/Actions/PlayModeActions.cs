using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Reports the current Unity Editor state, including play mode, compilation, updating, and play mode transition flags.", Name = "get_editor_state")]
    public class GetEditorStateAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var isPlaying = EditorApplication.isPlaying;
            var isCompiling = EditorApplication.isCompiling;
            var isUpdating = EditorApplication.isUpdating;
            var isChangingPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
            var appIsPlaying = Application.isPlaying;

            string summary;
            if (isCompiling)
            {
                summary = "Compiling";
            }
            else if (isUpdating)
            {
                summary = "Updating";
            }
            else if (isChangingPlayMode && !isPlaying)
            {
                summary = "EnteringPlayMode";
            }
            else if (isChangingPlayMode && isPlaying)
            {
                summary = "ExitingPlayMode";
            }
            else if (isPlaying)
            {
                summary = "Playing";
            }
            else
            {
                summary = "Idle";
            }

            return
                $"Editor State: {summary}\n" +
                $"Is Playing: {isPlaying}\n" +
                $"Application.isPlaying: {appIsPlaying}\n" +
                $"Is Compiling: {isCompiling}\n" +
                $"Is Updating: {isUpdating}\n" +
                $"Is Playing Or Will Change Playmode: {isChangingPlayMode}";
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }
    }

    [GPTAction("Enters Unity play mode if the editor is not already playing.", Name = "enter_play_mode")]
    public class EnterPlayModeAction : GPTAssistantAction, IGPTActionThatShouldNotReplayFromHistory
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
    public class ExitPlayModeAction : GPTAssistantAction, IGPTActionThatShouldNotReplayFromHistory
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
