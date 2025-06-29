using System.Threading.Tasks;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves all tags.")]
    public class RetrieveTagsAction : GPTAssistantAction
    {
        private string[] _tags = new string[0];

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            _tags = UnityEditorInternal.InternalEditorUtility.tags;
            return $"Project tags: {string.Join(", ", _tags)}";
#endif
        }
    }
}