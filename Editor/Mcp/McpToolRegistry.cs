using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GPTUnity.Settings;
using GPTUnity.Actions;
using UnityEngine;

namespace Mcp
{
    public static class McpToolRegistry
    {
        private static readonly Type BaseActionType = typeof(GPTAssistantAction);

        public static List<McpToolDefinition> GetTools(bool enabledOnly = false)
        {
            var tools = new List<McpToolDefinition>();
            var settings = ChatSettings.instance;
            foreach (var actionType in CollectActionTypes())
            {
                if (enabledOnly && settings != null && !settings.IsMcpToolEnabled(actionType.Name))
                    continue;

                var description = GetTypeDescription(actionType);
                var parameters = GetFunctionParameters(actionType);
                var required = GetRequiredParameterNames(actionType);

                var inputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = parameters,
                    ["required"] = required
                };

                tools.Add(new McpToolDefinition
                {
                    name = actionType.Name,
                    description = description ?? "Dynamically discovered action class",
                    inputSchema = inputSchema,
                    enabled = settings == null || settings.IsMcpToolEnabled(actionType.Name)
                });
            }

            return tools.OrderBy(tool => tool.name).ToList();
        }

        private static IEnumerable<Type> CollectActionTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic);

            var actions = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var assignableTypes = types.Where(t => BaseActionType.IsAssignableFrom(t));
                    var actionTypes = assignableTypes.Where(t =>
                        !t.IsAbstract &&
                        t.GetCustomAttribute<GPTActionAttribute>() != null);

                    actions.AddRange(actionTypes);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MCP] Error scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return actions.Distinct();
        }

        private static Dictionary<string, object> GetFunctionParameters(Type actionType)
        {
            var parameters = new Dictionary<string, object>();

            foreach (var property in actionType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute == null)
                    continue;

                if (property.PropertyType.IsEnum)
                {
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

        private static List<string> GetRequiredParameterNames(Type actionType)
        {
            var required = new List<string>();
            foreach (var property in actionType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute != null && attribute.Required)
                {
                    required.Add(property.Name);
                }
            }

            return required;
        }

        private static string GetTypeDescription(Type actionType)
        {
            var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
            return attribute != null ? attribute.Description : null;
        }
    }
}
