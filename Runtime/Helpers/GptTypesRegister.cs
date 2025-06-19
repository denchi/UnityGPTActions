using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                var types = assembly
                    .GetTypes();
            
                var assignableTypes = types
                    .Where(t => baseActionType.IsAssignableFrom(t));
            
                var actionTypes = assignableTypes
                    .Where(t =>
                        !t.IsAbstract && CustomAttributeExtensions.GetCustomAttribute<GPTActionAttribute>((MemberInfo)t) != null);
                
                foreach (var actionType in actionTypes)
                {
                    actionsDict[actionType.Name] = actionType;
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
                        properties = parameters
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
                //Assembly.GetExecutingAssembly(), // this would be package assembly
                Assembly.GetAssembly(baseActionType)
            };
            
            // Add all assemblies in the current AppDomain
            assemblies.AddRange(AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && assembly.FullName.StartsWith("Assembly-CSharp")));

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
                if (attribute == null)
                    continue;
                
                if (property.PropertyType.IsEnum)
                {
                    // If the property is an enum, we can use its names as the enum values
                    var enumValues = Enum.GetNames(property.PropertyType);
                    parameters[property.Name] = new
                    {
                        type = "string",
                        @enum = enumValues,
                        description = attribute.Description
                    };
                    continue;
                }

                parameters[property.Name] = new
                {
                    type = "string",
                    description = attribute.Description
                };
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
                if (attribute != null)
                {
                    if (attribute.Required)
                    {
                        required.Add(property.Name);
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
    }
}