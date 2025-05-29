using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Modifies RectTransform properties of a UI element")]
    public class AdjustRectTransformAction : GPTActionBase
    {
        [GPTParameter("GameObject name")]
        public string ObjectName { get; set; }

        [GPTParameter("Anchored Position (x,y) - optional")]
        public string AnchoredPosition { get; set; }

        [GPTParameter("Size Delta (width,height) - optional")]
        public string SizeDelta { get; set; }

        [GPTParameter("Pivot (x,y) - optional")]
        public string Pivot { get; set; }

        [GPTParameter("Rotation (x,y,z) - optional")]
        public string Rotation { get; set; }

        [GPTParameter("Scale (x,y,z) - optional")]
        public string Scale { get; set; }

        [GPTParameter("Anchor Min (x,y) - optional")]
        public string AnchorMin { get; set; }

        [GPTParameter("Anchor Max (x,y) - optional")]
        public string AnchorMax { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
                throw new Exception($"GameObject '{ObjectName}' not found.");

            var rectTransform = go.GetComponent<RectTransform>();
            if (!rectTransform)
                throw new Exception($"GameObject '{ObjectName}' does not have a RectTransform component.");

            var changes = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(AnchoredPosition))
            {
                var pos = ParseVector2(AnchoredPosition);
                rectTransform.anchoredPosition = pos;
                changes.Add($"anchoredPosition to {pos}");
            }

            if (!string.IsNullOrEmpty(SizeDelta))
            {
                var size = ParseVector2(SizeDelta);
                rectTransform.sizeDelta = size;
                changes.Add($"sizeDelta to {size}");
            }

            if (!string.IsNullOrEmpty(Pivot))
            {
                var pivot = ParseVector2(Pivot);
                rectTransform.pivot = pivot;
                changes.Add($"pivot to {pivot}");
            }

            if (!string.IsNullOrEmpty(Rotation))
            {
                var rotation = ParseVector3(Rotation);
                rectTransform.localEulerAngles = rotation;
                changes.Add($"rotation to {rotation}");
            }

            if (!string.IsNullOrEmpty(Scale))
            {
                var scale = ParseVector3(Scale);
                rectTransform.localScale = scale;
                changes.Add($"scale to {scale}");
            }

            if (!string.IsNullOrEmpty(AnchorMin))
            {
                var anchorMin = ParseVector2(AnchorMin);
                rectTransform.anchorMin = anchorMin;
                changes.Add($"anchorMin to {anchorMin}");
            }

            if (!string.IsNullOrEmpty(AnchorMax))
            {
                var anchorMax = ParseVector2(AnchorMax);
                rectTransform.anchorMax = anchorMax;
                changes.Add($"anchorMax to {anchorMax}");
            }

            EditorUtility.SetDirty(go);

            return $"Modified RectTransform on '{ObjectName}': {string.Join(", ", changes)}";
        }

        private Vector2 ParseVector2(string value)
        {
            var parts = value.Split(',');
            return new Vector2(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim())
            );
        }

        private Vector3 ParseVector3(string value)
        {
            var parts = value.Split(',');
            return new Vector3(
                float.Parse(parts[0].Trim()),
                float.Parse(parts[1].Trim()),
                float.Parse(parts[2].Trim())
            );
        }
    }
}
