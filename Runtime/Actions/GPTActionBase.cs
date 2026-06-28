using System;
using System.Reflection;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GPTUnity.Actions
{
    public abstract class GPTActionBase : IGPTAction
    {
        public virtual string Result { get; set; }

        public abstract Task<string> Execute();

        public void InitializeParameters(JObject arguments)
        {
            if (arguments == null)
                return;

            // Loop through all properties of this class
            foreach (var property in GetType().GetProperties())
            {
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute == null || !attribute.Expose)
                    continue;

                var parameterName = string.IsNullOrWhiteSpace(attribute.Name)
                    ? property.Name
                    : attribute.Name;

                if (arguments.TryGetValue(parameterName, StringComparison.Ordinal, out var argumentValue) ||
                    arguments.TryGetValue(property.Name, StringComparison.Ordinal, out argumentValue))
                {
                    SetPropertyValue(property, argumentValue);
                }
            }
        }

        private void SetPropertyValue(PropertyInfo property, JToken argumentValue)
        {
            if (argumentValue == null || argumentValue.Type == JTokenType.Null)
                return;

            // Convert and set the property value based on the property type
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(this, argumentValue.Type == JTokenType.String
                    ? argumentValue.Value<string>()
                    : argumentValue.ToString(Formatting.None));
            }
            else if (property.PropertyType == typeof(int))
            {
                property.SetValue(this, argumentValue.Value<int>());
            }
            else if (property.PropertyType == typeof(float))
            {
                property.SetValue(this, argumentValue.Value<float>());
            }
            else if (property.PropertyType == typeof(double))
            {
                property.SetValue(this, argumentValue.Value<double>());
            }
            else if (property.PropertyType == typeof(bool))
            {
                property.SetValue(this, argumentValue.Value<bool>());
            }
            else if (property.PropertyType.IsEnum &&
                     Enum.TryParse(property.PropertyType, argumentValue.Value<string>(), true, out var value))
            {
                property.SetValue(this, value);
            }
            else
            {
                try
                {
                    var deserializedValue = argumentValue.ToObject(property.PropertyType);
                    property.SetValue(this, deserializedValue);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to deserialize '{argumentValue}' into {property.PropertyType.Name}: {ex.Message}");
                }
            }
        }
    }
}
