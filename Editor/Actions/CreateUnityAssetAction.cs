using System;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEngine;

#if UNITY_EDITOR
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine.Audio;
#endif

namespace GPTUnity.Actions
{
    [GPTAction("Creates and saves a Unity asset instance by type at a specified asset path. Use this for generic asset creation when a more specific action does not exist.", Name = "create_unity_asset")]
    public class CreateUnityAssetAction : GPTAssistantAction, IGPTActionThatRequiresReload
    {
        [GPTParameter("Full type name of the UnityEngine.Object to create, for example 'UnityEngine.Material'.", true, Name = "object_type")]
        public string ObjectType { get; set; }

        [GPTParameter("Asset path where the new object should be saved, for example 'Assets/Data/MyAsset.asset'.", true, Name = "asset_path")]
        public string AssetPath { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(ObjectType))
                throw new Exception("ObjectType cannot be empty.");
            
            if (string.IsNullOrWhiteSpace(AssetPath))
                throw new Exception("AssetPath cannot be empty.");

            if (!UnityAiHelpers.TryGetObjectTypeByType(ObjectType, out var type))
                throw new Exception($"Type '{ObjectType}' not found.");
            
            if (type == null || !typeof(UnityEngine.Object).IsAssignableFrom(type))
                throw new Exception($"Type '{ObjectType}' not found or is not a UnityEngine.Object.");

            UnityEngine.Object instance = null;

            // Special handling for common Unity types
            if (type == typeof(Material))
                instance = new Material(Shader.Find("Standard"));
            else if (type == typeof(PhysicMaterial))
                instance = new PhysicMaterial();
            else if (type == typeof(PhysicsMaterial2D))
                instance = new PhysicsMaterial2D();
            else if (type == typeof(ComputeShader))
                throw new Exception("Cannot create ComputeShader assets via script.");
            else if (type == typeof(Texture2D))
                instance = new Texture2D(128, 128);
            else if (type == typeof(AudioMixer))
                throw new Exception("Cannot create AudioMixer assets via script.");
            else if (typeof(ScriptableObject).IsAssignableFrom(type))
                instance = ScriptableObject.CreateInstance(type);
            else
            {
                // Try to use Activator for other types
                try
                {
                    instance = Activator.CreateInstance(type) as UnityEngine.Object;
                }
                catch
                {
                    throw new Exception($"Could not create instance of type '{ObjectType}'.");
                }
            }

            if (instance == null)
                throw new Exception($"Failed to create instance of '{ObjectType}'.");

            AssetDatabase.CreateAsset(instance, AssetPath);
            AssetDatabase.SaveAssets();

            return $"Created asset of type '{ObjectType}' at '{AssetPath}'.";
#else
            throw new Exception("This action can only be executed in the Unity Editor.");
#endif
        }
    }
}
