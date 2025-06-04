using System.Collections.Generic;
using System.Threading.Tasks;
using GPTUnity.Data;
using UnityEngine;

namespace GPTUnity.Api
{
    public interface IGPTServiceApi
    {
        IReadOnlyList<string> Models { get; }
        
        Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null, object schema = null);
        Task<T> Get<T>(IReadOnlyCollection<GPTMessage> messages, string model, object schema, object[] tools = null);
    }
}