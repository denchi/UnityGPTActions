using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Sets world position, world rotation, and/or local scale on an existing GameObject.", Name = "set_game_object_transform")]
    public class TransformGameObjectAction : GPTAssistantAction
    {
        [GPTParameter("GameObject name or hierarchy path to transform.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

        [GPTParameter("Optional world position in 'x,y,z' format. Leave empty to keep the current position.", Name = "position")]
        public string Position { get; set; }

        [GPTParameter("Optional world rotation Euler angles in 'x,y,z' format. Leave empty to keep the current rotation.", Name = "rotation")]
        public string Rotation { get; set; }

        [GPTParameter("Optional local scale in 'x,y,z' format. Leave empty to keep the current scale.", Name = "scale")]
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
