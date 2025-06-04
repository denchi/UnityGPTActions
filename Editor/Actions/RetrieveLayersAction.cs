using System.Threading.Tasks;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves all layers.")]
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