using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;

namespace GPTUnity.Actions
{
    [GPTAction("Sets whether a GameObject is active in the scene hierarchy.", Name = "set_game_object_active")]
    public class ToggleGameObjectActiveStateAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")] public string ObjectName { get; set; }

        [GPTParameter("True to activate the GameObject, false to deactivate it.", true, Name = "is_active")]
        public bool ActiveState { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"Child GameObject '{ObjectName}' not found.");

            go.SetActive(ActiveState);
            
            return ActiveState
                ? $"GameObject '{ObjectName}' activated."
                : $"GameObject '{ObjectName}' deactivated.";
        }
    }
}
