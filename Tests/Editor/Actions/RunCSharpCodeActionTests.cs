using System;
using System.Linq;
using NUnit.Framework;

namespace GPTUnity.Tests.Actions
{
    public class RunCSharpCodeActionTests
    {
        [Test]
        public void BuildWrapperSource_ShouldWrapUserCodeInAsyncRunMethod()
        {
            const string code = "Debug.Log(\"hello\");\nreturn 42;";
            var source = GPTUnity.Actions.RunCSharpCodeAction.CSharpEvalRunner.BuildWrapperSource("EvalEntry_Test", code);

            StringAssert.Contains("public static async Task<object> Run()", source);
            StringAssert.Contains("Debug.Log(\"hello\");", source);
            StringAssert.Contains("return 42;", source);
            StringAssert.Contains("return null;", source);
            StringAssert.Contains("namespace GPTUnity.DynamicEval", source);
        }

        [Test]
        public void CollectReferencePaths_ShouldReturnDistinctAssemblyFiles()
        {
            var references = GPTUnity.Actions.RunCSharpCodeAction.CSharpEvalRunner.CollectReferencePaths();

            Assert.IsNotNull(references);
            Assert.IsNotEmpty(references);
            Assert.AreEqual(references.Length, references.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.IsTrue(references.All(System.IO.Path.IsPathRooted));
        }

        [Test]
        public void GptTypesRegister_ShouldExposeEvalCSharpAction()
        {
            var register = new GPTUnity.Helpers.GptTypesRegister(typeof(GPTUnity.Actions.GPTAssistantAction));

            var found = register.TryGetAction("eval_csharp", out var actionType);

            Assert.IsTrue(found);
            Assert.AreEqual(typeof(GPTUnity.Actions.RunCSharpCodeAction), actionType);
        }
    }
}
