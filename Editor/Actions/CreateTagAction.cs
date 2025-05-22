using System;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a new tag.")]
    public class CreateTagAction : GPTActionBase
    {
        [GPTParameter("Name of the new tag")] public string TagName { get; set; }

        public override string Content => $"Created new tag: {Highlight(TagName)}";

        public override void Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(TagName))
                throw new Exception("Tag name cannot be empty.");

            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            if (Array.Exists(tags, tag => tag == TagName))
                return;

            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                if (tag.stringValue == TagName)
                    return; // Tag already exists
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = TagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"Tag '{TagName}' created.");
#endif
        }
    }
}