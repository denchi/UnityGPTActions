using System;
using System.Linq;
using NUnit.Framework;
using GPTUnity.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GPTUnity.Tests.Helpers
{
    public abstract class BaseGptAction
    {
    }

    public enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }

    [GPTAction("Test action description", Name = "test_action")]
    public class TestAction : BaseGptAction
    {
        [GPTParameter("A required string parameter", true, Name = "required_param")]
        public string RequiredParam { get; set; }

        [GPTParameter("An optional bool parameter", Name = "enabled")]
        public bool Enabled { get; set; }

        [GPTParameter("An optional int parameter")]
        public int Count { get; set; }

        [GPTParameter("An optional string parameter")]
        public string OptionalParam { get; set; }

        public string NotAParameter { get; set; }
    }

    [GPTAction("Action with enum")]
    public class EnumTestAction : BaseGptAction
    {
        [GPTParameter("An enum parameter", true)]
        public TestEnum EnumParam { get; set; }
    }

    [GPTAction("Hidden action", Expose = false)]
    public class HiddenAction : BaseGptAction
    {
        [GPTParameter("A hidden parameter")]
        public string Value { get; set; }
    }

    public class NotAnAction : BaseGptAction
    {
    }

    [GPTAction]
    public abstract class AbstractAction : BaseGptAction
    {
    }

    [TestFixture]
    public class GptTypesRegisterTests
    {
        private GptTypesRegister _typeRegister;

        [SetUp]
        public void Setup()
        {
            _typeRegister = new GptTypesRegister(typeof(BaseGptAction));
        }

        [Test]
        public void Tools_ShouldContainExpectedActions()
        {
            string toolsJson = JsonConvert.SerializeObject(_typeRegister.Tools);
            JArray toolsArray = JArray.Parse(toolsJson);

            Assert.AreEqual(2, toolsArray.Count);

            foreach (var tool in toolsArray)
            {
                Assert.AreEqual("function", tool["type"]?.ToString());
                Assert.IsNotNull(tool["function"]);
                Assert.IsNotNull(tool["function"]?["name"]);
                Assert.IsNotNull(tool["function"]?["parameters"]);
                Assert.AreEqual("false", tool["function"]?["parameters"]?["additionalProperties"]?.ToString().ToLowerInvariant());
            }

            var testAction = toolsArray.FirstOrDefault(t =>
                t["function"]?["name"]?.ToString() == "test_action");

            Assert.IsNotNull(testAction, "test_action should be in the tools collection");
            Assert.AreEqual("Test action description", testAction["function"]?["description"]?.ToString());

            var parameters = testAction["function"]?["parameters"]?["properties"];
            Assert.IsNotNull(parameters?["required_param"]);
            Assert.IsNotNull(parameters?["OptionalParam"]);
            Assert.IsNotNull(parameters?["enabled"]);
            Assert.IsNotNull(parameters?["Count"]);
            Assert.IsNull(parameters?["NotAParameter"]);

            var required = testAction["function"]?["required"]?.ToObject<string[]>();
            Assert.IsTrue(required.Contains("required_param"));
            Assert.IsFalse(required.Contains("OptionalParam"));
        }

        [Test]
        public void ParameterTypes_ShouldBeCorrectlyFormatted()
        {
            string toolsJson = JsonConvert.SerializeObject(_typeRegister.Tools);
            JArray toolsArray = JArray.Parse(toolsJson);

            var testAction = toolsArray.First(t => t["function"]?["name"]?.ToString() == "test_action");
            var properties = testAction["function"]?["parameters"]?["properties"];

            Assert.AreEqual("string", properties?["required_param"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties?["enabled"]?["type"]?.ToString());
            Assert.AreEqual("integer", properties?["Count"]?["type"]?.ToString());
        }

        [Test]
        public void EnumParameters_ShouldBeCorrectlyFormatted()
        {
            string toolsJson = JsonConvert.SerializeObject(_typeRegister.Tools);
            JArray toolsArray = JArray.Parse(toolsJson);

            var enumAction = toolsArray.FirstOrDefault(t =>
                t["function"]?["name"]?.ToString() == nameof(EnumTestAction));

            Assert.IsNotNull(enumAction, "EnumTestAction should be in the tools collection");

            var enumParam = enumAction["function"]?["parameters"]?["properties"]?["EnumParam"];
            Assert.IsNotNull(enumParam);
            Assert.AreEqual("string", enumParam["type"]?.ToString());

            var enumValues = enumParam["enum"]?.ToObject<string[]>();
            Assert.AreEqual(3, enumValues.Length);
            Assert.IsTrue(enumValues.Contains("Option1"));
            Assert.IsTrue(enumValues.Contains("Option2"));
            Assert.IsTrue(enumValues.Contains("Option3"));
        }

        [Test]
        public void TryGetAction_ShouldReturnCorrectTypes()
        {
            bool foundTestAction = _typeRegister.TryGetAction("test_action", out Type testActionType);
            Assert.IsTrue(foundTestAction);
            Assert.AreEqual(typeof(TestAction), testActionType);

            bool foundEnumAction = _typeRegister.TryGetAction(nameof(EnumTestAction), out Type enumActionType);
            Assert.IsTrue(foundEnumAction);
            Assert.AreEqual(typeof(EnumTestAction), enumActionType);

            bool foundHiddenAction = _typeRegister.TryGetAction(nameof(HiddenAction), out _);
            Assert.IsFalse(foundHiddenAction);

            bool foundNonExistent = _typeRegister.TryGetAction("NonExistentAction", out _);
            Assert.IsFalse(foundNonExistent);

            bool foundNotAnAction = _typeRegister.TryGetAction(nameof(NotAnAction), out _);
            Assert.IsFalse(foundNotAnAction);

            bool foundAbstractAction = _typeRegister.TryGetAction(nameof(AbstractAction), out _);
            Assert.IsFalse(foundAbstractAction);
        }
    }
}
