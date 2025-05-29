namespace GPTUnity.Data
{
    public class GPTChoice
    {
        public GPTMessage message { get; set; }
        public FinishReason finish_reason { get; set; }
    }

    public enum FinishReason
    {
        stop,
        length,
        content_filter,
        tool_calls,
        function_call
    }
}