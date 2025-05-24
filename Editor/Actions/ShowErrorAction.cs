using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GPTUnity.Actions
{
    public class ShowErrorAction : GPTActionBase
    {
        private string description;
        public Exception Exception { get; }
        public IGPTAction Action { get; }
        public string Name { get; }

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

        public override async Task<string> Execute()
        {
            return $"{Exception.Message}";
        }
    }
}