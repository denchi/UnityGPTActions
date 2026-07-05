namespace GPTUnity.Actions.Interfaces
{
    /// <summary>
    /// Marks actions that are safe to execute when explicitly requested,
    /// but should not be replayed automatically from restored editor history
    /// after a domain reload or recompile.
    /// </summary>
    public interface IGPTActionThatShouldNotReplayFromHistory
    {
    }
}
