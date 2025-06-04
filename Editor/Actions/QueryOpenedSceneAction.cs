using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPTUnity.Actions
{
    [GPTAction(
        "Queries the currently opened Unity scene, providing detailed information about game objects, components, and hierarchy.")]
    public class QueryOpenedSceneAction : GPTAssistantAction
    {
        [GPTParameter("The game object name to query information about. Use if know the exact name of the GameObject. ")]
        public string TargetName { get; set; }

        [GPTParameter("The name of the type of the component to query information about")]
        public string TargetTypeName { get; set; }
        
        [GPTParameter("Should Include Transform Information")]
        public bool IncludeTransform { get; set; }
        
        [GPTParameter("Should Include Children GameObjects")]
        public bool IncludeChildren { get; set; }
        
        [GPTParameter("Should Include Components Information")]
        public bool IncludeOtherComponents { get; set; }

        public override async Task<string> Execute()
        {
            var activeScene = SceneManager.GetActiveScene();
            var sb = new StringBuilder();

            // Have a root game object - 
            if (!string.IsNullOrEmpty(TargetName))
            {
                // Search for specific GameObject
                if (UnityAiHelpers.TryFindGameObject(TargetName, out var target))
                {
                    sb.AppendLine($"Found GameObject '{TargetName}' in scene '{activeScene.name}':");
                    DescribeGameObject(target, sb, "");
                }
                else
                {
                    sb.AppendLine(
                        $"GameObject '{TargetName}' not found in scene '{activeScene.name}'.");
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

                    sb.AppendLine($"Found GameObjects in scene '{activeScene.name}':");
                    foreach (var go in gameObjects)
                    {
                        DescribeGameObject(go, sb, "");
                    }
                }
                else
                {
                    sb.AppendLine(
                        $"GameObject '{Highlight(TargetName)}' not found in scene '{activeScene.name}'.");
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

                var rootGameObjects = UnityAiHelpers.FindAllIncludingInactiveRootObjectInAllScenes();
                foreach (var go in rootGameObjects)
                {
                    DescribeGameObject(go, sb, "");
                }
            }

            return sb.ToString();
        }

        private void DescribeGameObject(GameObject obj, StringBuilder sb, string indent)
        {
            if (!string.IsNullOrEmpty(indent))
            {
                indent = $"{indent}/{obj.name}";
            }
            else
            {
                indent = obj.name;
            }
                
            sb.AppendLine(indent);

            // List components
            var components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;

                // Only include Transform if requested
                if (component is Transform)
                {
                    if (!IncludeTransform) continue;
                }
                else
                {
                    if (!IncludeOtherComponents) continue;
                }

                sb.Append($"\t[Component] {component.GetType().Name}");

                // Special handling for common components
                if (component is Transform transform && IncludeTransform)
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

            // Recursively describe children if requested
            if (IncludeChildren)
            {
                foreach (Transform child in obj.transform)
                {
                    DescribeGameObject(child.gameObject, sb, indent);
                }
            }
        }
    }
}
