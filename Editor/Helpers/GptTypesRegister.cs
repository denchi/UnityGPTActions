using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GPTUnity.Actions.Interfaces;

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

        private void CollectGptActions()
        {
            var actionTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    typeof(IGPTAction).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && CustomAttributeExtensions.GetCustomAttribute<GPTActionAttribute>((MemberInfo)t) != null);

            _actions = new Dictionary<string, Type>();
            foreach (var actionType in actionTypes)
            {
                _actions[actionType.Name] = actionType;
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