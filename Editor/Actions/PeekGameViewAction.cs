using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Captures a Game view screenshot to the project's Temp folder.")]
    public class PeekGameViewAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var fileName = $"peek_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var tempDir = Path.Combine(projectRoot, "Temp");
            var path = Path.Combine(tempDir, fileName).Replace("\\", "/");

            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            ScreenCapture.CaptureScreenshot(path, 1);
            return $"Game view screenshot queued to {path}.";
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }
    }
}
