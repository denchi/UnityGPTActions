using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Sets one or more RectTransform layout properties on a UI GameObject.", Name = "set_rect_transform")]
    public class AdjustRectTransformAction : GPTAssistantAction
    {
        [GPTParameter("UI GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

        [GPTParameter("Optional anchored position in 'x,y' format.", Name = "anchored_position")]
        public string AnchoredPosition { get; set; }

        [GPTParameter("Optional size delta in 'width,height' format.", Name = "size_delta")]
        public string SizeDelta { get; set; }

        [GPTParameter("Optional pivot in 'x,y' format.", Name = "pivot")]
        public string Pivot { get; set; }

        [GPTParameter("Optional local rotation Euler angles in 'x,y,z' format.", Name = "rotation")]
        public string Rotation { get; set; }

        [GPTParameter("Optional local scale in 'x,y,z' format.", Name = "scale")]
        public string Scale { get; set; }

        [GPTParameter("Optional anchor min in 'x,y' format.", Name = "anchor_min")]
        public string AnchorMin { get; set; }

        [GPTParameter("Optional anchor max in 'x,y' format.", Name = "anchor_max")]
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
