using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
            name = name.ToLower();
            
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

            void ParseObject(GameObject temp, string parentPath)
            {
                var newPath = parentPath + "/" + temp.name;
                if (temp.name.ToLower().Contains(name))
                {
                    gameObjects.Add(temp, newPath);
                }
                
                for (var i = 0; i < temp.transform.childCount; i++)
                {
                    var childTrs = temp.transform.GetChild(i);
                    ParseObject(childTrs.gameObject, newPath);
                }
            }
        }
        
        public static List<GameObject> FindAllIncludingInactiveRootObjectInAllScenes()
        {
            List<GameObject> gameObjects = new List<GameObject>();
            
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    gameObjects.Add(obj);
                }
            }

            return gameObjects;
        }
        
        public static bool TryFindGameObject(string path, out GameObject gameObject)
        {
            if (string.IsNullOrEmpty(path))
            {
                gameObject = null;
                return false;
            }

            var input = path.Trim();

            if (TryFindByHierarchyPath(input, out gameObject))
                return true;

            var allObjects = GetAllGameObjectsInLoadedScenes();

            var exactMatches = allObjects
                .Where(go => string.Equals(go.name, input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactMatches.Count == 1)
            {
                gameObject = exactMatches[0];
                return true;
            }

            if (exactMatches.Count > 1)
            {
                var exactPaths = exactMatches
                    .Take(8)
                    .Select(GetHierarchyPath);
                throw new Exception(
                    $"GameObject lookup '{path}' is ambiguous. Exact name matches: {string.Join(", ", exactPaths)}");
            }

            var listOfSimilarGameObjects = FindAllIncludingInactiveRootObjectInAllScenes(input);
            if (listOfSimilarGameObjects.Count == 1)
            {
                gameObject = listOfSimilarGameObjects.First().Key;
                return true;
            }

            if (listOfSimilarGameObjects.Count > 1)
            {
                var similar = listOfSimilarGameObjects.Values.Take(8);
                throw new Exception(
                    $"Could not find exact GameObject '{path}'. Similar matches: {string.Join(", ", similar)}");
            }

            throw new Exception($"Could not find game object: {path}.");
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

        public static bool TryGetChildGameObject(GameObject parentGameObject, string objectName, out GameObject result)
        {
            if (!parentGameObject)
            {
                for (int i = 0, sceneCount = SceneManager.sceneCount; i < sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    if (!scene.isLoaded)
                        continue;

                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (var obj in rootObjects)
                    {
                        if (obj.name == objectName)
                        {
                            result = obj;
                            return true;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0, childCount = parentGameObject.transform.childCount; i < childCount; i++)
                {
                    var child = parentGameObject.transform.GetChild(i);
                    if (child.name == objectName)
                    {
                        if (child.name == objectName)
                        {
                            result = child.gameObject;
                            return true;
                        }
                    }
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetObjectTypeByType(string typeName, out Type type) 
        {
#if UNITY_EDITOR
            
            // remove all but the type name from 
            typeName = typeName.Split(new char[] { '.', ',' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ??
                       typeName;
            
            // Search all loaded types that inherit from Component:
            type = TypeCache
                .GetTypesDerivedFrom<Object>()                  // gather all Component types
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

        private static bool TryFindByHierarchyPath(string path, out GameObject gameObject)
        {
            gameObject = null;
            if (!path.Contains("/"))
                return false;

            var parts = path.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            var roots = FindAllIncludingInactiveRootObjectInAllScenes()
                .Where(go => string.Equals(go.name, parts[0], StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var root in roots)
            {
                var current = root.transform;
                var failed = false;

                for (var i = 1; i < parts.Length; i++)
                {
                    Transform next = null;
                    for (var c = 0; c < current.childCount; c++)
                    {
                        var candidate = current.GetChild(c);
                        if (string.Equals(candidate.name, parts[i], StringComparison.OrdinalIgnoreCase))
                        {
                            next = candidate;
                            break;
                        }
                    }

                    if (next == null)
                    {
                        failed = true;
                        break;
                    }

                    current = next;
                }

                if (!failed)
                {
                    gameObject = current.gameObject;
                    return true;
                }
            }

            return false;
        }

        private static List<GameObject> GetAllGameObjectsInLoadedScenes()
        {
            var result = new List<GameObject>();
            var roots = FindAllIncludingInactiveRootObjectInAllScenes();
            foreach (var root in roots)
            {
                Collect(root, result);
            }

            return result;
        }

        private static void Collect(GameObject gameObject, List<GameObject> output)
        {
            output.Add(gameObject);
            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                Collect(gameObject.transform.GetChild(i).gameObject, output);
            }
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            var current = gameObject.transform;
            var path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = $"{current.name}/{path}";
            }

            return "/" + path;
        }
    }
}
