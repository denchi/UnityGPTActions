using System;
using System.Linq;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Gets the currently selected scene GameObjects from the Unity Hierarchy.", Name = "get_selected_game_objects")]
    public class GetSelectedHierarchyGameObjectAction : GPTAssistantAction
    {
        public override Task<string> Execute()
        {
            // Filter selection to only GameObjects in the hierarchy (scene objects)
            var selectedGameObjects = Selection.gameObjects;

            if (selectedGameObjects == null || selectedGameObjects.Length == 0)
            {
                return Task.FromResult("No GameObject is currently selected in the hierarchy view.");
            }

            var names = selectedGameObjects.Select(go => go.PathToGameObject() ).ToArray();
            var result = $"Selected GameObject(s) in hierarchy: {string.Join(", ", names)}";
            return Task.FromResult(result);
        }
    }
}
