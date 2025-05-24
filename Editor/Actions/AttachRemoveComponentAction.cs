using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using Object = UnityEngine.Object;

namespace GPTUnity.Actions
{
    [GPTAction("Attaches or removes a component from an existing GameObject.")]
    public class AttachRemoveComponentAction : GPTActionBase
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
}