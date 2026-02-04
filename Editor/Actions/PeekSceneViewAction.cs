using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Captures a Scene view screenshot to the project's Temp folder.")]
    public class PeekSceneViewAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return "No active SceneView camera found.";

            var fileName = $"peek_scene_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var tempDir = Path.Combine(projectRoot, "Temp");
            var path = Path.Combine(tempDir, fileName).Replace("\\", "/");

            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            CaptureSceneView(sceneView.camera, path);
            return $"Scene view screenshot saved to {path}.";
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

        private static void CaptureSceneView(Camera camera, string path)
        {
            var width = Mathf.Max(1, camera.pixelWidth);
            var height = Mathf.Max(1, camera.pixelHeight);

            var rt = new RenderTexture(width, height, 24);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;

            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();

            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            camera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(rt);

            var png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
        }
    }
}
