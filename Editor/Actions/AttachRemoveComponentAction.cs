using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using Object = UnityEngine.Object;

namespace GPTUnity.Actions
{
    [GPTAction("Legacy combined add/remove component action.", Expose = false)]
    public class AttachRemoveComponentAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name to modify")]
        public string ObjectName { get; set; }

        [GPTParameter("Component type (e.g., 'Rigidbody')")]
        public string ComponentType { get; set; }

        [GPTParameter("Action: attach or remove")]
        public string Action { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentType, out var type))
                throw new Exception($"Component type '{ComponentType}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            if (Action.Equals("attach", StringComparison.OrdinalIgnoreCase))
            {
                if (!go.TryGetComponent(type, out var _))
                {
                    go.AddComponent(type);
                }
            }
            else if (Action.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                var comp = go.GetComponent(type);
                if (comp)
                {
                    Object.DestroyImmediate(comp);
                }
            }
            else
            {
                throw new Exception($"Unknown action: {Action}. Use 'attach' or 'remove'.");
            }

            return Action.Equals("attach", StringComparison.OrdinalIgnoreCase)
                ? $"Attached component '{Highlight(ComponentType)}' to '{Highlight(ObjectName)}'"
                : $"Removed component '{Highlight(ComponentType)}' from '{Highlight(ObjectName)}'";
        }
    }

    [GPTAction("Adds a component to an existing GameObject if it is not already present.", Name = "add_component")]
    public class AddComponentAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true)]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Component type to add, for example 'Rigidbody' or 'BoxCollider'.", true)]
        public string ComponentTypeName { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectNameOrPath, out var go))
                throw new Exception($"GameObject '{ObjectNameOrPath}' not found.");

            if (go.TryGetComponent(type, out var existing))
                return $"GameObject '{ObjectNameOrPath}' already has component '{ComponentTypeName}'.";

            go.AddComponent(type);
            return $"Added component '{ComponentTypeName}' to '{ObjectNameOrPath}'.";
        }
    }

    [GPTAction("Removes a component from an existing GameObject if it is present.", Name = "remove_component")]
    public class RemoveComponentAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true)]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Component type to remove.", true)]
        public string ComponentTypeName { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var type))
                throw new Exception($"Component type '{ComponentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectNameOrPath, out var go))
                throw new Exception($"GameObject '{ObjectNameOrPath}' not found.");

            var component = go.GetComponent(type);
            if (!component)
                return $"GameObject '{ObjectNameOrPath}' does not have component '{ComponentTypeName}'.";

            Object.DestroyImmediate(component);
            return $"Removed component '{ComponentTypeName}' from '{ObjectNameOrPath}'.";
        }
    }
}
