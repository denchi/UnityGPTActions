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
        private static List<McpToolDefinition> _cachedToolDefinitions;

        public static List<McpToolDefinition> GetTools(bool enabledOnly = false)
        {
            EnsureCache();

            var tools = new List<McpToolDefinition>();
            var settings = ChatSettings.instance;
            foreach (var tool in _cachedToolDefinitions)
            {
                var isEnabled = settings == null || settings.IsMcpToolEnabled(tool.name);
                if (enabledOnly && !isEnabled)
                    continue;

                tools.Add(new McpToolDefinition
                {
                    name = tool.name,
                    description = tool.description,
                    inputSchema = tool.inputSchema,
                    enabled = isEnabled
                });
            }

            return tools.OrderBy(tool => tool.name).ToList();
        }

        public static void RefreshCache()
        {
            _cachedToolDefinitions = BuildToolDefinitions();
        }

        private static void EnsureCache()
        {
            if (_cachedToolDefinitions == null)
            {
                _cachedToolDefinitions = BuildToolDefinitions();
            }
        }

        private static List<McpToolDefinition> BuildToolDefinitions()
        {
            var tools = new List<McpToolDefinition>();
            foreach (var actionType in CollectActionTypes())
            {
                var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
                if (attribute == null || !attribute.Expose)
                    continue;

                var description = GetTypeDescription(actionType);
                var parameters = GetFunctionParameters(actionType);
                var required = GetRequiredParameterNames(actionType);
                var toolName = string.IsNullOrWhiteSpace(attribute.Name) ? actionType.Name : attribute.Name;

                var inputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = parameters,
                    ["required"] = required,
                    ["additionalProperties"] = false
                };

                tools.Add(new McpToolDefinition
                {
                    name = toolName,
                    description = description ?? "Dynamically discovered action class",
                    inputSchema = inputSchema,
                    enabled = true
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
                        !t.IsAbstract);

                    foreach (var actionType in actionTypes)
                    {
                        var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
                        if (attribute != null && attribute.Expose)
                        {
                            actions.Add(actionType);
                        }
                    }
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
                if (attribute == null || !attribute.Expose)
                    continue;

                var parameterName = string.IsNullOrWhiteSpace(attribute.Name)
                    ? property.Name
                    : attribute.Name;

                parameters[parameterName] = CreateJsonSchemaForType(property.PropertyType, attribute.Description);
            }

            return parameters;
        }

        private static List<string> GetRequiredParameterNames(Type actionType)
        {
            var required = new List<string>();
            foreach (var property in actionType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute != null && attribute.Expose && attribute.Required)
                {
                    required.Add(string.IsNullOrWhiteSpace(attribute.Name)
                        ? property.Name
                        : attribute.Name);
                }
            }

            return required;
        }

        private static string GetTypeDescription(Type actionType)
        {
            var attribute = actionType.GetCustomAttribute<GPTActionAttribute>();
            return attribute != null ? attribute.Description : null;
        }

        private static object CreateJsonSchemaForType(Type propertyType, string description)
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
