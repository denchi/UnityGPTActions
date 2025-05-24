using System.Threading.Tasks;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Explains a given compiler or runtime error message.")]
    public class ExplainErrorAction : GPTActionBase
    {
        [GPTParameter("The error message from Unity")]
        public string ErrorMessage { get; set; }

        [GPTParameter("The explanation of this error")]
        public string ErrorExplanation { get; set; }

        public override async Task<string> Execute()
        {
            // In a real scenario, you might parse the message, do some heuristics,
            // or even feed it back to ChatGPT. For now, just log it.
            return $"Error: {ErrorMessage}";
        }
    }
}