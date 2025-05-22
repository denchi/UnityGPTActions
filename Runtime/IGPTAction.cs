using System.Collections.Generic;

public interface IGPTAction
{
    string Content { get; }
    
    string Description { get; }
    
    void InitializeParameters(Dictionary<string, string> arguments);
    void Execute();
}