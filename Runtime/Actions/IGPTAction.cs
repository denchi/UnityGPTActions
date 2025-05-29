using System.Collections.Generic;
using System.Threading.Tasks;

namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTAction
    {
        // string Content { get; }

        //string Description { get; }
        string Result { get; set; }

        void InitializeParameters(Dictionary<string, string> arguments);

        Task<string> Execute();
    }
}