using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Legacy combined Rigidbody/collider action.", Expose = false)]
    public class GenerateRigidbodyAndColliderAction : GPTAssistantAction
    {
        [GPTParameter("Name of the GameObject")]
        public string ObjectName { get; set; }

        [GPTParameter("Type of collider: BoxCollider, SphereCollider, etc.")]
        public string ColliderType { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
            {
                throw new Exception($"GameObject '{ObjectName}' not found.");
            }

            if (!go.GetComponent<Rigidbody>())
            {
                go.AddComponent<Rigidbody>();
            }

            if (!UnityAiHelpers.TryGetComponentTypeByType(ColliderType, out var type))
            {
                throw new Exception($"Collider type '{ColliderType}' not found.");
            }

            if (!go.GetComponent(type))
            {
                go.AddComponent(type);
            }

            return $"Added Rigidbody and '{Highlight(ColliderType)}' to '{Highlight(ObjectName)}'";
        }
    }

    [GPTAction("Adds a Rigidbody to a GameObject if one is not already present.", Name = "add_rigidbody")]
    public class AddRigidbodyAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true)]
        public string ObjectNameOrPath { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectNameOrPath, out var go))
                throw new Exception($"GameObject '{ObjectNameOrPath}' not found.");

            if (go.GetComponent<Rigidbody>())
                return $"GameObject '{ObjectNameOrPath}' already has a Rigidbody.";

            go.AddComponent<Rigidbody>();
            return $"Added Rigidbody to '{ObjectNameOrPath}'.";
        }
    }

    [GPTAction("Adds a collider component to a GameObject if it is not already present.", Name = "add_collider")]
    public class AddColliderAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true)]
        public string ObjectNameOrPath { get; set; }

        [GPTParameter("Collider type to add, for example 'BoxCollider' or 'SphereCollider'.", true)]
        public string ColliderTypeName { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectNameOrPath, out var go))
                throw new Exception($"GameObject '{ObjectNameOrPath}' not found.");

            if (!UnityAiHelpers.TryGetComponentTypeByType(ColliderTypeName, out var type))
                throw new Exception($"Collider type '{ColliderTypeName}' not found.");

            if (!typeof(Collider).IsAssignableFrom(type))
                throw new Exception($"Type '{ColliderTypeName}' is not a Collider.");

            if (go.GetComponent(type))
                return $"GameObject '{ObjectNameOrPath}' already has collider '{ColliderTypeName}'.";

            go.AddComponent(type);
            return $"Added collider '{ColliderTypeName}' to '{ObjectNameOrPath}'.";
        }
    }
}
