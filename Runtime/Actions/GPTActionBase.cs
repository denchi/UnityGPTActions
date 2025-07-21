using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEngine;

namespace GPTUnity.Actions
{
    public abstract class GPTActionBase : IGPTAction
    {
        public virtual string Result { get; set; }

        public abstract Task<string> Execute();

        public void InitializeParameters(Dictionary<string, string> arguments)
        {
            // Loop through all properties of this class
            foreach (var property in GetType().GetProperties())
            {
                // Check if the property is marked with the GPTParameter attribute
                var attribute = property.GetCustomAttribute<GPTParameterAttribute>();
                if (attribute == null)
                    continue;

                // Try to find the argument in the dictionary that matches the property name
                if (arguments.TryGetValue(property.Name, out var argumentValue))
                {
                    SetPropertyValue(property, argumentValue);
                    continue;
                }

                // Debug.LogWarning($"No argument found for parameter '{property.Name}', using default value.");
            }
        }

        private void SetPropertyValue(PropertyInfo property, string argumentValue)
        {
            // Convert and set the property value based on the property type
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(this, argumentValue);
            }
            else if (property.PropertyType == typeof(int) && int.TryParse(argumentValue, out int intValue))
            {
                property.SetValue(this, intValue);
            }
            else if (property.PropertyType == typeof(float) && float.TryParse(argumentValue, out float floatValue))
            {
                property.SetValue(this, floatValue);
            }
            else if (property.PropertyType == typeof(bool) && bool.TryParse(argumentValue, out bool boolValue))
            {
                property.SetValue(this, boolValue);
            }
            else if (property.PropertyType.IsEnum && Enum.TryParse(property.PropertyType, argumentValue, out var value))
            {
                property.SetValue(this, value);
            }
            
            // if the type is an object
            else if (property.PropertyType.IsClass && !string.IsNullOrEmpty(argumentValue))
            {
                // Attempt to deserialize the argument value into the property type
                try
                {
                    var deserializedValue = JsonUtility.FromJson(argumentValue, property.PropertyType);
                    property.SetValue(this, deserializedValue);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to deserialize '{argumentValue}' into {property.PropertyType.Name}: {ex.Message}");
                }
            }
            
            // else if (property.PropertyType.IsArray && Enum.TryParse(property.PropertyType, argumentValue, out var value))
            // {
            //     property.SetValue(this, value);
            // }
            else
            {
                Debug.LogWarning(
                    $"Property {property.Name} could not be set to {argumentValue}. Unsupported type or invalid argument value.");
            }
        }
    }
}