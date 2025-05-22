using System;
using GPTUnity.Helpers;

namespace GPTUnity.Actions
{
    [GPTAction("Toggles a GameObject's active state on/off.")]
    public class ToggleGameObjectActiveStateAction : GPTActionBase
    {
        [GPTParameter("GameObject name")] public string ObjectName { get; set; }

        [GPTParameter("true to activate, false to deactivate")]
        public bool ActiveState { get; set; }

        public override string Content => ActiveState
            ? $"Activated GameObject '{Highlight(ObjectName)}'"
            : $"Deactivated GameObject '{Highlight(ObjectName)}'";

        public override void Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"Child GameObject '{ObjectName}' not found.");

            go.SetActive(ActiveState);
        }
    }
}