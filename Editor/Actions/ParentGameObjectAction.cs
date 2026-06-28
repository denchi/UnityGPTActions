using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;

namespace GPTUnity.Actions
{
    [GPTAction("Sets or clears a GameObject parent in the scene hierarchy.", Name = "set_game_object_parent")]
    public class ParentGameObjectAction : GPTAssistantAction
    {
        [GPTParameter("Child GameObject name or hierarchy path.", true, Name = "child_object_name_or_path")]
        public string ChildObject { get; set; }

        [GPTParameter("Optional parent GameObject name or hierarchy path. Leave empty to unparent.", Name = "parent_object_name_or_path")]
        public string ParentObject { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ChildObject, out var child))
            {
                throw new Exception($"Child GameObject '{ChildObject}' not found.");
            }

            if (string.IsNullOrEmpty(ParentObject))
            {
                // Unparent
                child.transform.SetParent(null);
                return $"Unparented '{ChildObject}'.";
            }

            if (!UnityAiHelpers.TryFindGameObject(ParentObject, out var parent))
            {
                throw new Exception($"Parent GameObject '{ParentObject}' not found.");
            }

            child.transform.SetParent(parent.transform);
            
            return $"Parented '{ChildObject}' to '{ParentObject}'.";
        }
    }
}
