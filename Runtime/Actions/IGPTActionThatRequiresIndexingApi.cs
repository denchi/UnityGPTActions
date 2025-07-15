using GPTUnity.Indexing;

namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTActionThatRequiresIndexingApi
    {
        IIndexingServiceApi Indexing { get; set; }
    }
}