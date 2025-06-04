namespace GPTUnity.Actions
{
    public abstract class GPTAssistantAction : GPTActionBase
    {
        public static string Highlight(string value)
        {
            return $"<color=#408DFF>{value}</color>";
        }

        protected static string Error(string value)
        {
            return $"<color=red>{value}</color>";
        }
    }
}