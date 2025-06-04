using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Adds a Rigidbody and a specified Collider to a GameObject.")]
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
}