using System.Collections.Generic;

public interface IGPTAction
{
    string Content { get; }
    
    string Description { get; }
    string Result { get; set; }

    void InitializeParameters(Dictionary<string, string> arguments);
    void Execute();
}