namespace GPTUnity.Actions
{
    [GPTAction("Retrieves all layers.")]
    public class RetrieveLayersAction : GPTActionBase
    {
        public override string Content => $"Project layers: {Highlight(string.Join(',', _layers))}";

        private string[] _layers = new string[0];

        public override void Execute()
        {
#if UNITY_EDITOR
            _layers = UnityEditorInternal.InternalEditorUtility.layers;
#endif
        }
    }
}