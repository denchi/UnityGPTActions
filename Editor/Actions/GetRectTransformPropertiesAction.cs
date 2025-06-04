using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Gets RectTransform properties of a UI element")]
    public class GetRectTransformPropertiesAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name")]
        public string ObjectName { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var rectTransform = go.GetComponent<RectTransform>();
            if (!rectTransform)
                throw new Exception($"GameObject '{ObjectName}' does not have a RectTransform component.");

            var props = new System.Text.StringBuilder();
            props.AppendLine($"RectTransform properties for '{ObjectName}':");
            props.AppendLine($"- Anchored Position: {Format(rectTransform.anchoredPosition)}");
            props.AppendLine($"- Size Delta: {Format(rectTransform.sizeDelta)}");
            props.AppendLine($"- Pivot: {Format(rectTransform.pivot)}");
            props.AppendLine($"- Rotation: {Format(rectTransform.localEulerAngles)}");
            props.AppendLine($"- Scale: {Format(rectTransform.localScale)}");
            props.AppendLine($"- Anchors Min: {Format(rectTransform.anchorMin)}");
            props.AppendLine($"- Anchors Max: {Format(rectTransform.anchorMax)}");

            return props.ToString();
        }

        private string Format(Vector2 v) => $"{v.x:F2}, {v.y:F2}";
        private string Format(Vector3 v) => $"{v.x:F2}, {v.y:F2}, {v.z:F2}";
    }
}
