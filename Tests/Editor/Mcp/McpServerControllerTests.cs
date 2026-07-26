using System.Diagnostics;
using System.Reflection;
using Mcp;
using NUnit.Framework;

namespace GPTUnity.Tests.Editor.Mcp
{
    public class McpServerControllerTests
    {
        [Test]
        public void StopPythonServer_WhenProcessWasNeverStarted_DoesNotThrow()
        {
            var controllerType = typeof(McpServerController);
            var processField = controllerType.GetField("_pythonProcess", BindingFlags.NonPublic | BindingFlags.Static);
            var stopMethod = controllerType.GetMethod("StopPythonServer", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(processField, Is.Not.Null);
            Assert.That(stopMethod, Is.Not.Null);

            processField.SetValue(null, new Process());

            Assert.DoesNotThrow(() => stopMethod.Invoke(null, new object[] { null }));
            Assert.That(processField.GetValue(null), Is.Null);
        }
    }
}
