using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates an Animator Controller asset and assigns it to a GameObject Animator component.", Name = "create_animator_controller")]
    public class CreateAnimatorControllerAction : GPTAssistantAction
    {
        [GPTParameter("Animator Controller file name without extension.", true, Name = "animator_name")]
        public string AnimatorName { get; set; }

        [GPTParameter("GameObject name or hierarchy path to attach the Animator component to.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

        [GPTParameter("Optional comma-separated list of state names to create in the controller.", Name = "states")]
        public string States { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
            {
                throw new Exception($"GameObject '{ObjectName}' not found.");
            }

            // Create the controller as an asset
            var path = $"Assets/{AnimatorName}.controller";
            var animatorController = UnityEditor.Animations.AnimatorController
                .CreateAnimatorControllerAtPath(path);

            // Create states
            if (!string.IsNullOrEmpty(States))
            {
                var statesArray = States.Split(',');
                var rootStateMachine = animatorController.layers[0].stateMachine;
                foreach (var s in statesArray)
                {
                    rootStateMachine.AddState(s.Trim());
                }
            }

            // Assign to GO
            var animator = go.GetComponent<Animator>();
            if (!animator)
                animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            AssetDatabase.Refresh();
#endif
            
            return $"Animator Controller '{AnimatorName}' created and assigned to '{ObjectName}'.";
        }
    }
}
