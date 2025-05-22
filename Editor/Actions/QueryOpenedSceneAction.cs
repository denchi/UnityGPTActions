using System.Linq;
using System.Text;
using GPTUnity.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPTUnity.Actions
{
    [GPTAction(
        "Queries the currently opened Unity scene, providing detailed information about game objects, components, and hierarchy.")]
    public class QueryOpenedSceneAction : GPTActionBase
    {
        private string _content;

        [GPTParameter("The game object name to query information about")]
        public string TargetName { get; set; }

        [GPTParameter("The name of the type of the component to query information about")]
        public string TargetTypeName { get; set; }

        public override string Content
        {
            get
            {
                var activeScene = SceneManager.GetActiveScene();
                var sb = new StringBuilder();

                if (!string.IsNullOrEmpty(TargetName))
                {
                    // Search for specific GameObject
                    if (UnityAiHelpers.TryFindGameObject(TargetName, out var target))
                    {
                        sb.AppendLine(
                            $"Found GameObject '{Highlight(TargetName)}' in scene '{Highlight(activeScene.name)}':");
                        DescribeGameObject(target, sb, "");
                    }
                    else
                    {
                        sb.AppendLine(
                            $"GameObject '{Highlight(TargetName)}' not found in scene '{Highlight(activeScene.name)}'.");
                    }
                }
                else if (!string.IsNullOrEmpty(TargetTypeName))
                {
                    // Search for specific GameObject
                    if (UnityAiHelpers.TryGetComponentTypeByType(TargetTypeName, out var targetType))
                    {
#if UNITY_2023_1_OR_NEWER
                        var objects = GameObject.FindObjectsByType(
                            targetType,
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);
#else
                        var objects = GameObject.FindObjectsOfType(
                            targetType,
                            true);
#endif

                        var gameObjects = objects
                            .OfType<Component>()
                            .Select(obj => obj.gameObject)
                            .Distinct()
                            .ToList();

                        sb.AppendLine($"Found GameObjects in scene '{Highlight(activeScene.name)}':");
                        foreach (var go in gameObjects)
                        {
                            DescribeGameObject(go, sb, "");
                        }
                    }
                    else
                    {
                        sb.AppendLine(
                            $"GameObject '{Highlight(TargetName)}' not found in scene '{Highlight(activeScene.name)}'.");
                    }
                }
                else
                {
                    // Describe entire scene
                    sb.AppendLine($"Scene: {Highlight(activeScene.name)}");
                    sb.AppendLine($"Path: {Highlight(activeScene.path)}");
                    sb.AppendLine($"Build Index: {Highlight(activeScene.buildIndex.ToString())}");
                    sb.AppendLine($"Is Loaded: {Highlight(activeScene.isLoaded.ToString())}");
                    sb.AppendLine($"Root GameObjects: {Highlight(activeScene.rootCount.ToString())}");
                    sb.AppendLine("\nHierarchy:");

                    var rootObjects = activeScene.GetRootGameObjects();
                    foreach (var root in rootObjects)
                    {
                        DescribeGameObject(root, sb, "");
                    }
                }

                return sb.ToString();
            }
        }

        public override void Execute()
        {

        }

        private void DescribeGameObject(GameObject obj, StringBuilder sb, string indent)
        {
            sb.AppendLine($"{indent}└─ {obj.name}");
            indent += "  ";

            // List components
            var components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;

                sb.Append($"{indent}[Component] {component.GetType().Name}");

                // Special handling for common components
                if (component is Transform transform)
                {
                    sb.Append(
                        $" (Pos: {transform.position}, Rot: {transform.eulerAngles}, Scale: {transform.localScale})");
                }
                else if (component is MeshRenderer renderer)
                {
                    sb.Append($" (Materials: {renderer.sharedMaterials.Length})");
                }
                else if (component is Collider collider)
                {
                    sb.Append($" (Enabled: {collider.enabled}, Trigger: {collider.isTrigger})");
                }

                sb.AppendLine();
            }

            // Recursively describe children
            foreach (Transform child in obj.transform)
            {
                DescribeGameObject(child.gameObject, sb, indent);
            }
        }
    }
}