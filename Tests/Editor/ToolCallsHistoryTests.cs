using GPTUnity.Data;
using GPTUnity.Helpers;
using NUnit.Framework;

public class ToolCallsHistoryTests
{
    [Test]
    public void MarkToolCallExecuted_StoresId()
    {
        var history = new ToolCallsHistory();
        var call = new GPTToolCall { id = "1" };

        Assert.IsFalse(history.IsToolCallExecuted(call));
        history.MarkToolCallExecuted(call);
        Assert.IsTrue(history.IsToolCallExecuted(call));
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        var history = new ToolCallsHistory();
        var call = new GPTToolCall { id = "1" };
        history.MarkToolCallExecuted(call);

        history.Clear();

        Assert.IsFalse(history.IsToolCallExecuted(call));
    }
}
