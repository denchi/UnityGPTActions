using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Renames a GameObject.")]
    public class RenameGameObjectAction : GPTAssistantAction
    {
        [GPTParameter("Name of the GameObject")]
        public string ObjectName { get; set; }

        [GPTParameter("New name for the GameObject")]
        public string NewName { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(ObjectName))
                throw new Exception("Current GameObject name cannot be empty.");
            
            if (string.IsNullOrEmpty(NewName))
                throw new Exception("New GameObject name cannot be empty.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            go.name = NewName;
            return $"Renamed GameObject '{ObjectName}' to '{NewName}'.";
#endif
        }
    }
}

