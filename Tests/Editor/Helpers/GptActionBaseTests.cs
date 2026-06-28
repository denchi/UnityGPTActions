using System.Threading.Tasks;
using GPTUnity.Actions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GPTUnity.Tests.Helpers
{
    public class GptActionBaseTests
    {
        private enum SampleMode
        {
            First,
            Second
        }

        private class SampleAction : GPTActionBase
        {
            [GPTParameter("A string value", Name = "title")]
            public string Title { get; set; }

            [GPTParameter("A boolean value", Name = "is_enabled")]
            public bool Enabled { get; set; }

            [GPTParameter("An integer value")]
            public int Count { get; set; }

            [GPTParameter("An enum value")]
            public SampleMode Mode { get; set; }

            public override Task<string> Execute()
            {
                return Task.FromResult("ok");
            }
        }

        [Test]
        public void InitializeParameters_ShouldAssignTypedValues()
        {
            var action = new SampleAction();
            var args = JObject.Parse(@"{""title"":""Hello"",""is_enabled"":true,""Count"":4,""Mode"":""Second""}");

            action.InitializeParameters(args);

            Assert.AreEqual("Hello", action.Title);
            Assert.IsTrue(action.Enabled);
            Assert.AreEqual(4, action.Count);
            Assert.AreEqual(SampleMode.Second, action.Mode);
        }
    }
}
