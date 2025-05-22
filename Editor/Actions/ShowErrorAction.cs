using System;
using System.Collections.Generic;

namespace GPTUnity.Actions
{
    public class ShowErrorAction : GPTActionBase
    {
        private string description;
        public Exception Exception { get; }
        public IGPTAction Action { get; }
        public string Name { get; }

        public override string Content => Exception.Message;
        
        public override string Description => $"Error using {Action?.GetType().Name ?? Name}: <color=red>{Content}</color>";

        public ShowErrorAction(IGPTAction action, Exception exception)
        {
            Exception = exception;
            Action = action;
        }
        
        public ShowErrorAction(string name, Exception exception)
        {
            Exception = exception;
            Name = name;
        }

        public override void Execute()
        {
            //  
        }
    }
}