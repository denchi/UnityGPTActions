using System.Threading.Tasks;

namespace GPTUnity.Actions
{
    [GPTAction("Lists all Unity layers currently defined in the project.", Name = "list_layers")]
    public class RetrieveLayersAction : GPTAssistantAction
    {
        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            return $"Project layers: {string.Join(", ", layers)}";
#endif
        }
    }
}
