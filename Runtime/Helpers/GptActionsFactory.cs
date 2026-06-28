using System;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var arguments = new JObject();

            if (!string.IsNullOrEmpty(functionCall.arguments))
            {
                try
                {
                    arguments = JsonConvert.DeserializeObject<JObject>(functionCall.arguments) ?? new JObject();
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
