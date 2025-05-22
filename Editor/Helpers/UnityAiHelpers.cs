using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPTUnity.Helpers
{
    public static class UnityAiHelpers
    {
        public static GameObject FindIncludingInactiveRootObjectInAllScenes(string name)
        {
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    if (obj.name == name)
                        return obj;
                }
            }

            return null;
        }
        
        public static IDictionary<GameObject, string> FindAllIncludingInactiveRootObjectInAllScenes(string name)
        {
            IDictionary<GameObject, string> gameObjects = new Dictionary<GameObject, string>();
            
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    ParseObject(obj, "");
                }
            }

            return gameObjects;

            void ParseObject(GameObject root, string rootPath)
            {
                var newPath = rootPath + "/" + root.name;
                if (root.name == name)
                {
                    gameObjects.Add(root, newPath);
                }
                
                for (var i = 0; i < root.transform.childCount; i++)
                {
                    var childTrs = root.transform.GetChild(i);
                    ParseObject(childTrs.gameObject, newPath);
                }
            }
        }
        
        public static bool TryFindGameObject(string path, out GameObject gameObject)
        {
            if (string.IsNullOrEmpty(path))
            {
                gameObject = null;
                return false;
            }
            
            // Try scene path (GameObject/Component)
            if (path.Contains("/"))
            {
                var parts = path.TrimStart('/').Split('/');
                var current = FindIncludingInactiveRootObjectInAllScenes(parts[0]);
                if (current)
                {
                    var restOfThePath = string.Join("/", parts, 1, parts.Length - 1);
                    var targetTransform = current.transform.Find(restOfThePath);
                    if (targetTransform)
                    {
                        gameObject = targetTransform.gameObject;
                        return true;
                    }
                }
            } 
            
            // Find the gameobject even if it is deactivated
            // This is a workaround for Unity's limitation in finding inactive GameObjects
            // https://forum.unity.com/threads/how-to-find-inactive-gameobject-by-name.123456/
            
            gameObject = FindIncludingInactiveRootObjectInAllScenes(path);
            if (!gameObject)
            {
                var listOfSimilarGameObjects = FindAllIncludingInactiveRootObjectInAllScenes(path);
                throw new Exception($"Could not find game object at path: {path}. " +
                                    $"Found: {string.Join(',', listOfSimilarGameObjects.Values) } instead!");
            }
            
            return gameObject;
        }
        
        public static bool TryFindAsset(string path, Type type, out UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            // Try to find the asset using AssetDatabase 
            asset = AssetDatabase.LoadAssetAtPath(path, type);
#else
            asset = null;
#endif
            return asset;
        }
        
        public static bool TryFindComponent(string path, Type type, out UnityEngine.Object component)
        {
            if (TryFindGameObject(path, out var gameObject))
            {
                component = gameObject.GetComponent(type);
                return component;
            }
            
            component = null;
            return false;
        }

        public static bool TryGetComponentTypeByType(string typeName, out Type type)
        {
            #if UNITY_EDITOR
            
            // Search all loaded types that inherit from Component:
            type = TypeCache
                .GetTypesDerivedFrom<Component>()                  // gather all Component types
                .FirstOrDefault(t => 
                    string.Equals(t.Name, typeName, 
                        StringComparison.OrdinalIgnoreCase)
                );

            return type != null;
            
            #else
            type = Type.GetType(typeName) ??
                   Type.GetType("UnityEngine." + typeName) ??
                   Type.GetType("UnityEngine." + typeName + ", UnityEngine");
            return type != null;
            #endif
        }

        public static bool TryParseVector3(string vector3String, out Vector3 vector)
        {
            if (string.IsNullOrEmpty(vector3String))
            {
                vector = Vector3.zero;
                return false;
            }
            
            var parts = vector3String.Split(',');
            if (parts.Length != 3)
            {
                vector = Vector3.zero;
                return false;
            }
            
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            float.TryParse(parts[2], out float z);
            
            vector = new Vector3(x, y, z);
            return true;
        }
        
        public static void WaitThenRun(System.Action onReady)
        {
            EditorApplication.update += () =>
            {
                if (!EditorApplication.isCompiling)
                {
                    EditorApplication.update -= () => { };
                    onReady?.Invoke();
                }
            };
        }
    }
}