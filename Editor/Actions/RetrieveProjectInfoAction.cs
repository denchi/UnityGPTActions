using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves project information such as name, build target, render pipeline, input system, 2D/3D, and loaded scenes.")]
    public class RetrieveProjectInfoAction : GPTActionBase
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var sb = new StringBuilder();

            // Project name
            string projectName = Application.productName;
            sb.AppendLine($"Project Name: {projectName}");

            // Active build target
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            sb.AppendLine($"Active Build Target: {buildTarget}");

            // Render pipeline
            string renderPipeline = "Built-in";
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                renderPipeline = GraphicsSettings.currentRenderPipeline.GetType().Name;
            }
            sb.AppendLine($"Render Pipeline: {renderPipeline}");

            // Input system
            string inputSystem = "Old";
#if ENABLE_INPUT_SYSTEM
            inputSystem = "New";
#endif
            sb.AppendLine($"Input System: {inputSystem}");

            // 2D or 3D
            bool is2D = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D;
            sb.AppendLine($"Project Type: {(is2D ? "2D" : "3D")}");

            // Loaded scenes
            var loadedScenes = Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Select(i => EditorSceneManager.GetSceneAt(i).name)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();
            sb.AppendLine($"Loaded Scenes: {string.Join(", ", loadedScenes)}");

            return sb.ToString();
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }
    }
}
