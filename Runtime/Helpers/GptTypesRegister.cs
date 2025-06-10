using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GPTUnity.Actions.Interfaces;
using UnityEngine;

namespace GPTUnity.Helpers
{
    public class GptTypesRegister
    {
        public Dictionary<string, Type> Actions
        {
            get
            {
                if (_actions == null || _actions.Count == 0)
                {
                    CollectGptActions();
                }

                return _actions;
            }
        }

        public List<object> Functions
        {
            get
            {
                if (_functions == null || _functions.Count == 0)
                {
                    CreateFunctions();
                }

                return _functions;
            }
        }

        public object[] Tools
        {
            get
            {
                if (_tools == null || _tools.Length == 0)
                {
                    CreateFunctions();
                }

                return _tools;
            }
        }

        private Dictionary<string, Type> _actions;
        private List<object> _functions;
        private object[] _tools;
        private object tools;
        private Type _gptActionType;
        
        public GptTypesRegister(Type rootType)
        {
            _gptActionType = rootType;
            
            CollectGptActions();
            CreateFunctions();
        }

        private void CollectGptActions()
        {
            var assemblies = new List<Assembly>
            {
                //Assembly.GetExecutingAssembly(), // this would be package assembly
                Assembly.GetAssembly(_gptActionType)
            };
            
            // Add all assemblies in the current AppDomain
            assemblies.AddRange(AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && a.FullName.StartsWith("Assembly-CSharp")));

            _actions = new Dictionary<string, Type>();

            foreach (var assembly in assemblies)
            {
                var types = assembly
                    .GetTypes();
            
                var assignableTypes = types
                    .Where(t => _gptActionType.IsAssignableFrom(t));
            
                var actionTypes = assignableTypes
                    .Where(t =>
                        !t.IsAbstract && CustomAttributeExtensions.GetCustomAttribute<GPTActionAttribute>((MemberInfo)t) != null);
                
                foreach (var actionType in actionTypes)
                {
                    _actions[actionType.Name] = actionType;
                }
            }
        }

        private void CreateFunctions()
        {
            var functions = Actions.Select(action =>
            {
                var parameters = GetFunctionParameters(action.Value);
                var description = GetTypeDescription(action.Value);
                var required = GetFunctionParametersRequired(action.Value);

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
                    // strict = true,
                    // additionalProperties = false,
                    // tool_choice: "required"
                };
            });

            _functions = functions.ToList<object>();
            _tools = functions.Select(x => new { type = "function", function = x }).ToArray();
        }

        private object GetFunctionParameters(Type actionType)
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

        private List<String> GetFunctionParametersRequired(Type actionType)
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

        private string GetTypeDescription(Type actionType)
        {
            var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
            if (attribute != null)
            {
                return attribute.Description;
            }

            return null;
        }
    }
}