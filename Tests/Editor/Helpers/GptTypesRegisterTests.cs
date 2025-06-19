using System;
using System.Linq;
using NUnit.Framework;
using GPTUnity.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GPTUnity.Tests.Helpers
{
    // Mock attributes for testing
    [AttributeUsage(AttributeTargets.Class)]
    public class GPTActionAttribute : Attribute
    {
        public string Description { get; }
        
        public GPTActionAttribute(string description = null)
        {
            Description = description;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public class GPTParameterAttribute : Attribute
    {
        public string Description { get; }
        public bool Required { get; }
        
        public GPTParameterAttribute(string description = null, bool required = false)
        {
            Description = description;
            Required = required;
        }
    }

    // Base action type for tests
    public abstract class BaseGptAction
    {
        // Base class for actions
    }
    
    // Test enum for parameter testing
    public enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }
    
    // Mock action classes for testing
    [GPTAction("Test action description")]
    public class TestAction : BaseGptAction
    {
        [GPTParameter("A required string parameter", true)]
        public string RequiredParam { get; set; }
        
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
    
    public class NotAnAction : BaseGptAction
    {
        // This class should be ignored as it doesn't have the GPTActionAttribute
    }
    
    [GPTAction]
    public abstract class AbstractAction : BaseGptAction
    {
        // This class should be ignored as it's abstract
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
            // Convert tools to JSON for easier inspection
            string toolsJson = JsonConvert.SerializeObject(_typeRegister.Tools);
            JArray toolsArray = JArray.Parse(toolsJson);
            
            // Should find exactly two valid actions
            Assert.AreEqual(2, toolsArray.Count);
            
            // Verify all tools have the right structure
            foreach (var tool in toolsArray)
            {
                Assert.AreEqual("function", tool["type"].ToString());
                Assert.IsNotNull(tool["function"]);
                Assert.IsNotNull(tool["function"]["name"]);
                Assert.IsNotNull(tool["function"]["parameters"]);
            }
            
            // Find TestAction in the tools
            var testAction = toolsArray.FirstOrDefault(t => 
                t["function"]["name"].ToString() == nameof(TestAction));
            
            Assert.IsNotNull(testAction, "TestAction should be in the tools collection");
            Assert.AreEqual("Test action description", testAction["function"]["description"].ToString());
            
            // Check parameters
            var parameters = testAction["function"]["parameters"]["properties"];
            Assert.IsNotNull(parameters["RequiredParam"]);
            Assert.IsNotNull(parameters["OptionalParam"]);
            Assert.IsNull(parameters["NotAParameter"]);
            
            // Check required parameters
            var required = testAction["function"]["required"].ToObject<string[]>();
            Assert.IsTrue(required.Contains("RequiredParam"));
            Assert.IsFalse(required.Contains("OptionalParam"));
        }
        
        [Test]
        public void EnumParameters_ShouldBeCorrectlyFormatted()
        {
            string toolsJson = JsonConvert.SerializeObject(_typeRegister.Tools);
            JArray toolsArray = JArray.Parse(toolsJson);
            
            var enumAction = toolsArray.FirstOrDefault(t => 
                t["function"]["name"].ToString() == nameof(EnumTestAction));
                
            Assert.IsNotNull(enumAction, "EnumTestAction should be in the tools collection");
            
            // Check enum parameter format
            var enumParam = enumAction["function"]["parameters"]["properties"]["EnumParam"];
            Assert.IsNotNull(enumParam);
            Assert.AreEqual("string", enumParam["type"].ToString());
            
            // Verify enum values are included
            var enumValues = enumParam["enum"].ToObject<string[]>();
            Assert.AreEqual(3, enumValues.Length);
            Assert.IsTrue(enumValues.Contains("Option1"));
            Assert.IsTrue(enumValues.Contains("Option2"));
            Assert.IsTrue(enumValues.Contains("Option3"));
        }
        
        [Test]
        public void TryGetAction_ShouldReturnCorrectTypes()
        {
            // Test finding valid action
            bool foundTestAction = _typeRegister.TryGetAction(nameof(TestAction), out Type testActionType);
            Assert.IsTrue(foundTestAction);
            Assert.AreEqual(typeof(TestAction), testActionType);
            
            // Test finding enum action
            bool foundEnumAction = _typeRegister.TryGetAction(nameof(EnumTestAction), out Type enumActionType);
            Assert.IsTrue(foundEnumAction);
            Assert.AreEqual(typeof(EnumTestAction), enumActionType);
            
            // Test non-existent action
            bool foundNonExistent = _typeRegister.TryGetAction("NonExistentAction", out _);
            Assert.IsFalse(foundNonExistent);
            
            // Test non-action class
            bool foundNotAnAction = _typeRegister.TryGetAction(nameof(NotAnAction), out _);
            Assert.IsFalse(foundNotAnAction);
            
            // Test abstract action class
            bool foundAbstractAction = _typeRegister.TryGetAction(nameof(AbstractAction), out _);
            Assert.IsFalse(foundAbstractAction);
        }
    }
}
