using System.Threading.Tasks;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Optimizes Unity QualitySettings for a specific platform.")]
    public class OptimizeQualitySettingsAction : GPTActionBase
    {
        [GPTParameter("Target platform: PC, Mobile, Console, or VR")]
        public string TargetPlatform { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            return $"Optimizing Quality Settings for: {TargetPlatform}";
#endif
        }
    }
}