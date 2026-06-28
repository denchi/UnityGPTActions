using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTAction
    {
        string Result { get; set; }

        void InitializeParameters(JObject arguments);

        Task<string> Execute();
    }
}
