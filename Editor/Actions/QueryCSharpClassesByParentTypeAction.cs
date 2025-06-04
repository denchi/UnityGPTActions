using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor; // Added for AssetDatabase

namespace GPTUnity.Actions
{
    [GPTAction("Searches for C# classes that inherit from a specified base class or implement a specified interface.")]
    public class QueryCSharpClassesByParentTypeAction : GPTAssistantAction
    {
        [GPTParameter("The name of the parent class or interface to search for (e.g., 'MonoBehaviour', 'IMyInterface')")]
        public string ParentTypeName { get; set; }

        public override async Task<string> Execute()
        {
            if (string.IsNullOrEmpty(ParentTypeName))
                return "ParentTypeName parameter is required.";

            var sb = new StringBuilder();
            var foundTypes = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var allTypes = assemblies.SelectMany(asm => asm.DefinedTypes);
            Type parentType = allTypes.FirstOrDefault(x => x.Name == ParentTypeName)?.AsType();
            
            // foreach (var asm in assemblies)
            // {
            //     parentType = asm.GetType(ParentTypeName, false);
            //     if (parentType != null)
            //         break;
            // }
            
            if (parentType == null)
                return $"Could not find type '{ParentTypeName}' in loaded assemblies.";

            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                {
                    if (type == null || !type.IsClass || type.IsAbstract)
                        continue;

                    if (parentType.IsInterface)
                    {
                        if (type.GetInterfaces().Any(i => i == parentType))
                            foundTypes.Add(type);
                    }
                    else
                    {
                        var t = type.BaseType;
                        while (t != null)
                        {
                            if (t == parentType)
                            {
                                foundTypes.Add(type);
                                break;
                            }
                            t = t.BaseType;
                        }
                    }
                }
            }

            if (foundTypes.Count == 0)
                return $"No classes found inheriting from or implementing '{ParentTypeName}'.";

            sb.AppendLine($"Found {foundTypes.Count} classes inheriting from or implementing '{ParentTypeName}':");
            foreach (var type in foundTypes)
            {
                // Try to find the script asset path for this type
                string scriptPath = FindScriptAssetPath(type);
                if (!string.IsNullOrEmpty(scriptPath))
                    sb.AppendLine($"{type.Name} - {scriptPath}");
                else
                    sb.AppendLine($"{type.Name} - [Script file not found]");
            }

            return sb.ToString();
        }

        // Helper to find the .cs file path for a given type using AssetDatabase
        private string FindScriptAssetPath(Type type)
        {
            // Only search for MonoScripts in the project
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (monoScript != null && monoScript.GetClass() == type)
                    return path;
            }
            return null;
        }
    }
}
