using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPTUnity.Actions
{
    [GPTAction("Creates a new ScriptableObject asset of the specified type and saves it at the given path.")]
    public class CreateScriptableObjectAction : GPTActionBase, IActionThatRequiresReload
    {
        [GPTParameter("The full type name of the ScriptableObject to create (e.g., 'MyNamespace.MySOType')")]
        public string ScriptableObjectType { get; set; }

        [GPTParameter("The asset path to save the ScriptableObject (e.g., 'Assets/MySO.asset')")]
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
