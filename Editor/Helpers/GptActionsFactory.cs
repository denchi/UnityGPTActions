using System;
using System.Collections.Generic;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace GPTUnity.Helpers
{

    public class GptActionsFactory
    {
        private GptTypesRegister typesRegister;

        public void Init(GptTypesRegister typesRegister)
        {
            this.typesRegister = typesRegister;
        }

        public IGPTAction CreateActionFromFunctionCall(GPTFunctionCall functionCall)
        {
            if (!typesRegister.Actions.TryGetValue(functionCall.name, out var actionType))
            {
                throw new Exception("Action class not found: " + functionCall.name);
            }

            var actionInstance = (IGPTAction)Activator.CreateInstance(actionType);
            var arguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(functionCall.arguments))
            {
                try
                {
                    arguments = JsonConvert.DeserializeObject<Dictionary<string, string>>(functionCall.arguments);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing function arguments: {ex.Message}");
                    return null;
                }
            }

            actionInstance.InitializeParameters(arguments);

            return actionInstance;
        }
    }
}