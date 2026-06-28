using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Renames an existing GameObject in the open scene.", Name = "rename_game_object")]
    public class RenameGameObjectAction : GPTAssistantAction
    {
        [GPTParameter("Current GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

        [GPTParameter("New GameObject name.", true, Name = "new_name")]
        public string NewName { get; set; }

        public override async Task<string> Execute()
        {
            if (string.IsNullOrEmpty(ObjectName))
                throw new Exception("ObjectName cannot be empty.");

            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            go.name = NewName;
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(go);
            #endif
            
            return $"Renamed GameObject '{ObjectName}' to '{NewName}'";
        }
    }
}
