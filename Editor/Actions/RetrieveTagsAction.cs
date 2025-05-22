namespace GPTUnity.Actions
{
    [GPTAction("Retrieves all tags.")]
    public class RetrieveTagsAction : GPTActionBase
    {
        public override string Content => $"Project tags: {Highlight(string.Join(',', _tags))}";

        private string[] _tags = new string[0];

        public override void Execute()
        {
#if UNITY_EDITOR
            _tags = UnityEditorInternal.InternalEditorUtility.tags;
#endif
        }
    }
}