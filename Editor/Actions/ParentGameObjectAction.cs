using System;
using GPTUnity.Helpers;

namespace GPTUnity.Actions
{
    [GPTAction("Parents one GameObject to another, or unparents if parent is empty.")]
    public class ParentGameObjectAction : GPTActionBase
    {
        [GPTParameter("Child GameObject name")]
        public string ChildObject { get; set; }

        [GPTParameter("Parent GameObject name, empty to unparent")]
        public string ParentObject { get; set; }

        public override string Content => string.IsNullOrEmpty(ParentObject)
            ? $"Unparented '{Highlight(ChildObject)}'"
            : $"Parented '{Highlight(ChildObject)}' to '{Highlight(ParentObject)}'";

        public override void Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ChildObject, out var child))
            {
                throw new Exception($"Child GameObject '{ChildObject}' not found.");
            }

            if (string.IsNullOrEmpty(ParentObject))
            {
                // Unparent
                child.transform.SetParent(null);
                return;
            }

            if (!UnityAiHelpers.TryFindGameObject(ParentObject, out var parent))
            {
                throw new Exception($"Parent GameObject '{ParentObject}' not found.");
            }

            child.transform.SetParent(parent.transform);
        }
    }
}