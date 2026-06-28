using System;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPTUnity.Actions
{
    [GPTAction("Creates a ScriptableObject asset by type and saves it into the project.", Name = "create_scriptable_object_asset")]
    public class CreateScriptableObjectAction : GPTAssistantAction, IGPTActionThatRequiresReload
    {
        [GPTParameter("Full type name of the ScriptableObject to create, for example 'MyNamespace.MySOType'.", true, Name = "scriptable_object_type")]
        public string ScriptableObjectType { get; set; }

        [GPTParameter("Asset path where the ScriptableObject should be created, for example 'Assets/Data/MySO.asset'.", true, Name = "asset_path")]
        public string AssetPath { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(ScriptableObjectType))
                throw new Exception("ScriptableObjectType cannot be empty.");
            if (string.IsNullOrWhiteSpace(AssetPath))
                throw new Exception("AssetPath cannot be empty.");

            var type = Type.GetType(ScriptableObjectType);
            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
                throw new Exception($"Type '{ScriptableObjectType}' not found or is not a ScriptableObject.");

            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, AssetPath);
            AssetDatabase.SaveAssets();
            
            // AssetDatabase.Refresh();

            return $"Created ScriptableObject of type '{ScriptableObjectType}' at '{AssetPath}'.";
#else
            throw new Exception("This action can only be executed in the Unity Editor.");
#endif
        }
    }
}
