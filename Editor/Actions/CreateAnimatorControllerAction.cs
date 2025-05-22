using System;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates an Animator Controller and assigns it to a GameObject.")]
    public class CreateAnimatorControllerAction : GPTActionBase
    {
        [GPTParameter("Name of the Animator Controller")]
        public string AnimatorName { get; set; }

        [GPTParameter("Name of the GameObject to attach Animator to")]
        public string ObjectName { get; set; }

        [GPTParameter("Comma-separated list of states to create in the controller (optional)")]
        public string States { get; set; }

        public override string Content =>
            $"Created Animator Controller '{Highlight(AnimatorName)}' and assigned to '{Highlight(ObjectName)}'";

        public override void Execute()
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
            Debug.Log($"Animator Controller '{AnimatorName}' created and assigned to '{ObjectName}'.");
#endif
        }
    }
}