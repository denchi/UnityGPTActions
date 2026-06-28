using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Gets the renderer bounds of a GameObject, optionally including all children.", Name = "get_object_bounds")]
    public class GetObjectBoundsAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }
        
        [GPTParameter("Include child GameObjects when calculating bounds.", Name = "include_children")]
        public bool IncludeChildren { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
            {
                throw new Exception($"Child GameObject '{ObjectName}' not found.");
            }

            var bounds = GetBounds(go);
            
            if (IncludeChildren)
            {
                CalculateBounds(go, ref bounds);
            }

            return $"Bounds of '{ObjectName}': Center: {bounds.center}, Size: {bounds.size}";
        }
        
        private Bounds GetBounds(GameObject go)
        {
            if (go.TryGetComponent<Renderer>(out var renderer))
            {
                return renderer.bounds;
            }
            
            return new Bounds(go.transform.position, Vector3.zero);
        }

        private void CalculateBounds(GameObject go, ref Bounds bounds)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                var other = GetBounds(child);
                bounds.Encapsulate(other);
                
                CalculateBounds(child, ref bounds);
            }
        }
    }
}
