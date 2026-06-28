using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GPTUnity.Helpers
{
    /// <summary>
    /// Responsible for discovering and registering GPT action classes that can be exposed as function tools to the GPT API.
    /// This class scans assemblies to find all eligible action types and converts them into a format compatible with GPT.
    /// </summary>
    public class GptTypesRegister
    {
        /// <summary>
        /// Gets the collection of function definitions formatted for the GPT API.
        /// These represent all the available actions that GPT can invoke.
        /// </summary>
        public object[] Tools { get; }

        /// <summary>Dictionary mapping action names to their corresponding Types</summary>
        private readonly Dictionary<string, Type> actions;
        
        /// <summary>Base type that all GPT actions must inherit from</summary>
        private readonly Type baseActionType;
        
        /// <summary>
        /// Initializes a new instance of the GptTypesRegister class.
        /// </summary>
        /// <param name="rootType">The base type from which all GPT actions must derive</param>
        public GptTypesRegister(Type rootType)
        {
            baseActionType = rootType;
            actions = CollectGptActions();
            Tools = CreateFunctions();
        }

        /// <summary>
        /// Scans assemblies to find all classes that derive from the base action type
        /// and have the GPTActionAttribute applied.
        /// </summary>
        /// <returns>Dictionary mapping action names to their Type definitions</returns>
        private Dictionary<string, Type> CollectGptActions()
        {
            var assemblies = GetAvailableAssemblies();
            var actionsDict = new Dictionary<string, Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                
                    var assignableTypes = types
                        .Where(t => baseActionType.IsAssignableFrom(t));
                
                    var actionTypes = assignableTypes
                        .Where(t =>
                            !t.IsAbstract && CustomAttributeExtensions.GetCustomAttribute<GPTActionAttribute>((MemberInfo)t) is { Expose: true });
                    
                    foreach (var actionType in actionTypes)
                    {
                        var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
                        var actionName = string.IsNullOrWhiteSpace(attribute?.Name) ? actionType.Name : attribute.Name;
                        actionsDict[actionName] = actionType;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"<color=yellow>[GptTypesRegister]</color> Error scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return actionsDict;
        }
        
        /// <summary>
        /// Converts collected action types into function definitions compatible with GPT API.
        /// Each function defines a tool that GPT can use to interact with the system.
        /// </summary>
        /// <returns>Array of function objects formatted for GPT API</returns>
        private object[] CreateFunctions()
        {
            var functions = actions.Select(action =>
            {
                var parameters = GetFunctionParameters(action.Value);
                var description = GetTypeDescription(action.Value);
                var required = GetRequiredParameterNames(action.Value);

                return new
                {
                    name = action.Key, // The action ID is the class name
                    description = description ?? "Dynamically discovered action class",
                    parameters = new
                    {
                        type = "object",
                        properties = parameters,
                        additionalProperties = false
                    },
                    required,
                };
            });
            
            return functions
                .Select(x => new { type = "function", function = x })
                .ToArray<object>();
        }

        /// <summary>
        /// Collects relevant assemblies where GPT action types might be defined.
        /// Includes the assembly containing the base action type and user assemblies.
        /// </summary>
        /// <returns>List of assemblies to scan for GPT actions</returns>
        private List<Assembly> GetAvailableAssemblies()
        {
            var assemblies = new List<Assembly>
            {
                Assembly.GetAssembly(baseActionType) // The assembly containing the base action type
            };
            
            // Add all relevant assemblies in the current AppDomain
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic);
            
            foreach (var assembly in allAssemblies)
            {                
                if (!assemblies.Contains(assembly))
                {
                    assemblies.Add(assembly);
                }
            }

            return assemblies;
        }
        
        /// <summary>
        /// Extracts parameter information from an action type's properties that have
        /// the GPTParameterAttribute. Handles special case for enum properties.
        /// </summary>
        /// <param name="actionType">The action type to extract parameters from</param>
        /// <returns>Dictionary mapping parameter names to their definitions</returns>
        private Dictionary<string, object> GetFunctionParameters(Type actionType)
        {
            var parameters = new Dictionary<string, object>();

            foreach (var property in actionType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute == null || !attribute.Expose)
                    continue;

                var parameterName = string.IsNullOrWhiteSpace(attribute.Name)
                    ? property.Name
                    : attribute.Name;

                parameters[parameterName] = CreateJsonSchemaForType(property.PropertyType, attribute.Description);
            }

            return parameters;
        }

        /// <summary>
        /// Identifies which parameters are required for an action type based on
        /// the Required property of the GPTParameterAttribute.
        /// </summary>
        /// <param name="actionType">The action type to check for required parameters</param>
        /// <returns>List of required parameter names</returns>
        private List<String> GetRequiredParameterNames(Type actionType)
        {
            var required = new List<String>();
            foreach (var property in actionType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute != null && attribute.Expose)
                {
                    if (attribute.Required)
                    {
                        required.Add(string.IsNullOrWhiteSpace(attribute.Name)
                            ? property.Name
                            : attribute.Name);
                    }
                }
            }

            return required;
        }

        /// <summary>
        /// Retrieves the description of an action type from its GPTActionAttribute.
        /// </summary>
        /// <param name="actionType">The action type to get the description for</param>
        /// <returns>Description string or null if no attribute exists</returns>
        private string GetTypeDescription(Type actionType)
        {
            var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
            if (attribute != null)
            {
                return attribute.Description;
            }

            return null;
        }

        /// <summary>
        /// Attempts to find an action type by its function call name.
        /// </summary>
        /// <param name="functionCallName">Name of the function being called by GPT</param>
        /// <param name="type">When successful, contains the action type; otherwise, null</param>
        /// <returns>True if the action was found; otherwise, false</returns>
        public bool TryGetAction(string functionCallName, out Type type)
        {
            return actions.TryGetValue(functionCallName, out type);
        }

        private object CreateJsonSchemaForType(Type propertyType, string description)
        {
            if (propertyType.IsEnum)
            {
                return new
                {
                    type = "string",
                    @enum = Enum.GetNames(propertyType),
                    description
                };
            }

            if (propertyType == typeof(string))
                return new { type = "string", description };

            if (propertyType == typeof(bool))
                return new { type = "boolean", description };

            if (propertyType == typeof(int) || propertyType == typeof(long))
                return new { type = "integer", description };

            if (propertyType == typeof(float) || propertyType == typeof(double) || propertyType == typeof(decimal))
                return new { type = "number", description };

            if (propertyType.IsArray)
            {
                return new
                {
                    type = "array",
                    items = CreateJsonSchemaForType(propertyType.GetElementType(), null),
                    description
                };
            }

            if (propertyType.IsClass)
                return new { type = "object", description };

            return new { type = "string", description };
        }
    }
}
