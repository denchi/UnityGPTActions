namespace GPTUnity.Data
{
    public class GPTToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public GPTFunctionCall function { get; set; }
    }
}