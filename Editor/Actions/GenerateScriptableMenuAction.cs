namespace GPTUnity.Actions
{
    [GPTAction("Generates a scriptable menu or custom Editor window with specified actions.")]
    public class GenerateScriptableMenuAction : CreateFileActionBase
    {
        [GPTParameter("Menu name/title")] public string MenuName { get; set; }

        [GPTParameter("Actions or UI elements")]
        public string MenuActions { get; set; }
    }
}