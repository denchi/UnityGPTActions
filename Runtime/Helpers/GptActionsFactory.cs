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
        private Action<IGPTAction> actionCreatedCallback;

        public void Init(GptTypesRegister typesRegister, Action<IGPTAction> actionCreatedCallback = null)
        {
            this.typesRegister = typesRegister;
            this.actionCreatedCallback = actionCreatedCallback;
        }

        public IGPTAction CreateActionFromFunctionCall(GPTFunctionCall functionCall)
        {
            if (!typesRegister.TryGetAction(functionCall.name, out var actionType))
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
            
            if (actionCreatedCallback != null)
            {
                actionCreatedCallback(actionInstance);
            }

            return actionInstance;
        }
    }
}