using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPTUnity.Actions
{
    [GPTAction("Creates, opens, or saves Unity scenes.")]
    public class ManageSceneAction : GPTAssistantAction
    {
        public enum SceneOperation
        {
            New,
            Open,
            Save,
            SaveAs
        }

        [GPTParameter("Operation to perform: New, Open, Save, SaveAs")]
        public SceneOperation Operation { get; set; } = SceneOperation.New;

        [GPTParameter("Scene path (e.g., Assets/Scenes/MyScene.unity). Required for Open/SaveAs; optional for New/Save.")]
        public string ScenePath { get; set; }

        [GPTParameter("New scene setup: DefaultGameObjects or EmptyScene")]
        public string NewSceneSetup { get; set; } = "DefaultGameObjects";

        [GPTParameter("New scene mode: Single or Additive")]
        public string NewSceneMode { get; set; } = "Single";

        [GPTParameter("Save modified scenes before operation")]
        public bool SaveCurrentModifiedScenes { get; set; } = true;

        [GPTParameter("Save behavior when scenes are modified: Prompt, Save, Discard. Overrides SaveCurrentModifiedScenes if set.")]
        public string SaveBehavior { get; set; } = "Prompt";

        [GPTParameter("For Open: if the scene doesn't exist, create it")]
        public bool CreateIfMissing { get; set; } = false;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var saveDecision = ApplySaveBehavior();
            if (!saveDecision)
                return "Operation canceled (unsaved changes).";

            switch (Operation)
            {
                case SceneOperation.New:
                    return CreateNewScene();
                case SceneOperation.Open:
                    return OpenScene();
                case SceneOperation.Save:
                    return SaveScene();
                case SceneOperation.SaveAs:
                    return SaveSceneAs();
                default:
                    return $"Unsupported operation: {Operation}";
            }
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

        private bool ApplySaveBehavior()
        {
            var behavior = (SaveBehavior ?? string.Empty).Trim();
            if (string.Equals(behavior, "Save", StringComparison.OrdinalIgnoreCase))
            {
                return EditorSceneManager.SaveOpenScenes();
            }

            if (string.Equals(behavior, "Discard", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Prompt mode can block automation pipelines when dialogs are unavailable.
            if (Application.isBatchMode)
            {
                return SaveCurrentModifiedScenes ? EditorSceneManager.SaveOpenScenes() : true;
            }

            if (SaveCurrentModifiedScenes)
            {
                return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            return true;
        }

        private string CreateNewScene()
        {
            var setup = ParseNewSceneSetup(NewSceneSetup);
            var mode = ParseNewSceneMode(NewSceneMode);

            var scene = EditorSceneManager.NewScene(setup, mode);

            if (!string.IsNullOrWhiteSpace(ScenePath))
            {
                var path = NormalizeScenePath(ScenePath);
                EnsureDirectoryExists(path);
                if (!EditorSceneManager.SaveScene(scene, path))
                {
                    return $"Failed to save new scene to '{path}'.";
                }
                return $"Created and saved new scene '{scene.name}' at {path}.";
            }

            return $"Created new scene '{scene.name}' (unsaved).";
        }

        private string OpenScene()
        {
            if (string.IsNullOrWhiteSpace(ScenePath))
                return "ScenePath is required for Open.";

            var path = NormalizeScenePath(ScenePath);
            if (!File.Exists(path))
            {
                if (!CreateIfMissing)
                    return $"Scene not found at {path}.";

                var scene = EditorSceneManager.NewScene(ParseNewSceneSetup(NewSceneSetup), ParseNewSceneMode(NewSceneMode));
                EnsureDirectoryExists(path);
                if (!EditorSceneManager.SaveScene(scene, path))
                    return $"Failed to create scene at {path}.";
            }

            var opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return $"Opened scene '{opened.name}' from {path}.";
        }

        private string SaveScene()
        {
            var scene = SceneManager.GetActiveScene();
            var path = string.IsNullOrWhiteSpace(ScenePath) ? scene.path : NormalizeScenePath(ScenePath);
            if (string.IsNullOrWhiteSpace(path))
                return "Active scene has no path. Use SaveAs with ScenePath.";

            EnsureDirectoryExists(path);
            if (!EditorSceneManager.SaveScene(scene, path))
                return $"Failed to save scene '{scene.name}' to {path}.";

            return $"Saved scene '{scene.name}' to {path}.";
        }

        private string SaveSceneAs()
        {
            if (string.IsNullOrWhiteSpace(ScenePath))
                return "ScenePath is required for SaveAs.";

            var scene = SceneManager.GetActiveScene();
            var path = NormalizeScenePath(ScenePath);
            EnsureDirectoryExists(path);

            if (!EditorSceneManager.SaveScene(scene, path))
                return $"Failed to save scene '{scene.name}' to {path}.";

            return $"Saved scene '{scene.name}' to {path}.";
        }

        private static UnityEditor.SceneManagement.NewSceneSetup ParseNewSceneSetup(string value)
        {
            return string.Equals(value, "EmptyScene", StringComparison.OrdinalIgnoreCase)
                ? UnityEditor.SceneManagement.NewSceneSetup.EmptyScene
                : UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects;
        }

        private static UnityEditor.SceneManagement.NewSceneMode ParseNewSceneMode(string value)
        {
            return string.Equals(value, "Additive", StringComparison.OrdinalIgnoreCase)
                ? UnityEditor.SceneManagement.NewSceneMode.Additive
                : UnityEditor.SceneManagement.NewSceneMode.Single;
        }

        private static string NormalizeScenePath(string path)
        {
            var trimmed = path.Trim();
            if (!trimmed.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                trimmed += ".unity";

            if (trimmed.StartsWith("Assets" + Path.DirectorySeparatorChar) || trimmed.StartsWith("Assets/"))
                return trimmed;

            return Path.Combine("Assets", trimmed).Replace("\\", "/");
        }

        private static void EnsureDirectoryExists(string scenePath)
        {
            var directory = Path.GetDirectoryName(scenePath);
            if (string.IsNullOrEmpty(directory))
                return;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
