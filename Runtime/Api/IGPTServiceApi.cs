using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPTUnity.Data;
using UnityEngine;

namespace GPTUnity.Api
{
    public interface IGPTServiceApi
    {
        IReadOnlyList<string> GetModels();
        Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null);
    }
}