using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Duplicates a GameObject hierarchy, optionally reparents the duplicate, and preserves children and components.", Name = "duplicate_game_object_hierarchy")]
    public class DuplicateHierarchyAction : GPTAssistantAction
    {
        [GPTParameter("Source GameObject name or hierarchy path to duplicate.", true, Name = "source_object_name_or_path")]
        public string SourceObjectName { get; set; }

        [GPTParameter("Optional parent GameObject name or hierarchy path for the duplicate.", Name = "parent_object_name_or_path")]
        public string ParentObjectName { get; set; }

        [GPTParameter("Optional new name for the duplicated root GameObject.", Name = "new_name")]
        public string NewName { get; set; }

        [GPTParameter("Keep world transform when reparenting the duplicate.", Name = "keep_world_transform")]
        public bool KeepWorldTransform { get; set; } = true;

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(SourceObjectName, out var source))
                throw new Exception($"Source GameObject '{SourceObjectName}' not found.");

            Transform targetParent = source.transform.parent;
            if (!string.IsNullOrWhiteSpace(ParentObjectName))
            {
                if (!UnityAiHelpers.TryFindGameObject(ParentObjectName, out var parentObject))
                    throw new Exception($"Parent GameObject '{ParentObjectName}' not found.");

                targetParent = parentObject.transform;
            }

            var duplicate = UnityEngine.Object.Instantiate(source);
            if (targetParent != null)
                duplicate.transform.SetParent(targetParent, KeepWorldTransform);

            if (!string.IsNullOrWhiteSpace(NewName))
                duplicate.name = NewName.Trim();

            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate hierarchy");

            if (duplicate.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(duplicate.scene);

            return $"Duplicated '{SourceObjectName}' to '{ActionEditingUtilities.GetGameObjectHierarchyPath(duplicate)}'.";
        }
    }
}
