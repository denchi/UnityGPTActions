using System.Linq;
using System.Collections.Generic;
using Mcp;
using NUnit.Framework;

namespace GPTUnity.Tests.Editor.Mcp
{
    public class McpToolRegistryTests
    {
        [Test]
        public void GetTools_UsesDeclaredGptActionNames()
        {
            McpToolRegistry.RefreshCache();
            var tools = McpToolRegistry.GetTools();

            Assert.That(tools.Any(tool => tool.name == "capture_game_view"), Is.True);
            Assert.That(tools.Any(tool => tool.name == "probe_frame_budget"), Is.True);
            Assert.That(tools.Any(tool => tool.name == "probe_memory_counters"), Is.True);
            Assert.That(tools.Any(tool => tool.name == nameof(PeekGameViewAction)), Is.False);
        }

        [Test]
        public void GetTools_UsesJsonSchemaTypesAndParameterAliases()
        {
            McpToolRegistry.RefreshCache();
            var frameBudgetTool = McpToolRegistry.GetTools().First(tool => tool.name == "probe_frame_budget");
            var schema = (Dictionary<string, object>)frameBudgetTool.inputSchema;
            var properties = (Dictionary<string, object>)schema["properties"];
            var required = (List<string>)schema["required"];

            Assert.That(properties.ContainsKey("sample_count"), Is.True);
            Assert.That(properties.ContainsKey("require_play_mode"), Is.True);
            Assert.That(properties.ContainsKey(nameof(ProbeFrameBudgetAction.SampleCount)), Is.False);
            Assert.That(required, Is.Empty);
        }
    }
}
