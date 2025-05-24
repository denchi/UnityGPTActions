using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Moves, rotates, or scales an existing GameObject.")]
    public class TransformGameObjectAction : GPTActionBase
    {
        [GPTParameter("Name of the GameObject to transform")]
        public string ObjectName { get; set; }

        [GPTParameter("New position in 'x,y,z' format. Leave empty if no change.")]
        public string Position { get; set; }

        [GPTParameter("New rotation in 'x,y,z' format. Leave empty if no change.")]
        public string Rotation { get; set; }

        [GPTParameter("New scale in 'x,y,z' format. Leave empty if no change.")]
        public string Scale { get; set; }

        public override async Task<string> Execute()
        {
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var go))
            {
                throw new Exception($"Child GameObject '{ObjectName}' not found.");
            }

            if (!string.IsNullOrEmpty(Position))
                go.transform.position = ParseVector3(Position);
            if (!string.IsNullOrEmpty(Rotation))
                go.transform.eulerAngles = ParseVector3(Rotation);
            if (!string.IsNullOrEmpty(Scale))
                go.transform.localScale = ParseVector3(Scale);
            
            return $"Transformed GameObject '{ObjectName}' with Position: {Position}, Rotation: {Rotation}, Scale: {Scale}.";
        }

        private Vector3 ParseVector3(string input)
        {
            var parts = input.Split(',');
            if (parts.Length != 3) return Vector3.zero;
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            float.TryParse(parts[2], out float z);
            return new Vector3(x, y, z);
        }
    }
}