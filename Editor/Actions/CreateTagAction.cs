using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a new tag.")]
    public class CreateTagAction : GPTActionBase
    {
        [GPTParameter("Name of the new tag")] 
        public string TagName { get; set; }


        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(TagName))
                throw new Exception("Tag name cannot be empty.");

            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            if (Array.Exists(tags, tag => tag == TagName))
                return $"Tag '{TagName}' already exists.";

            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                if (tag.stringValue == TagName)
                    return $"Tag '{TagName}' already exists.";
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = TagName;
            tagManager.ApplyModifiedProperties();
            return $"Tag '{TagName}' created.";
#endif
        }
    }
}