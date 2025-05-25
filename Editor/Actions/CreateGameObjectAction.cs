using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a new GameObject with one or more predefined components.")]
    public class CreateGameObjectAction : GPTActionBase
    {
        [GPTParameter("Name of the new GameObject")]
        public string ObjectName { get; set; }

        [GPTParameter("Comma-separated list of component types to add (e.g. 'Rigidbody,BoxCollider')")]
        public string Components { get; set; }

        [GPTParameter("Parent GameObject name. Can be a path like Canvas/Panel/Button")]
        public string ParentObjectName { get; set; }

        [GPTParameter("New position in 'x,y,z' format. Leave empty if no change.")]
        public string Position { get; set; }

        [GPTParameter("New rotation in 'x,y,z' format. Leave empty if no change.")]
        public string Rotation { get; set; }

        [GPTParameter("New scale in 'x,y,z' format. Leave empty if no change.")]
        public string Scale { get; set; }

        [GPTParameter("New local position in 'x,y,z' format. Leave empty if no change.")]
        public string LocalPosition { get; set; }

        [GPTParameter("New local rotation in 'x,y,z' format. Leave empty if no change.")]
        public string LocalRotation { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR

            ObjectName = ObjectName.Trim('/');

            GameObject parentGameObject = null;
            
            if (ObjectName.Contains("/"))
            {
                // Create a new GameObject with the specified name and path
                var path = ObjectName.Substring(0, ObjectName.LastIndexOf('/'));

                if (!UnityAiHelpers.TryFindGameObject(path, out parentGameObject))
                    throw new Exception("Could not find parent GameObject: " + path);
                
                ObjectName = ObjectName.Substring(ObjectName.LastIndexOf('/') + 1);
            }
            
            if (!parentGameObject)
            {
                if (!string.IsNullOrEmpty(ParentObjectName) && !UnityAiHelpers.TryFindGameObject(ParentObjectName, out parentGameObject))
                    throw new Exception($"Parent GameObject '{ParentObjectName}' not found.");
            }

            if (UnityAiHelpers.TryGetChildGameObject(parentGameObject, ObjectName, out _))
            {
                throw new Exception($"GameObject '{ObjectName}' already exists{(parentGameObject!= null ? " under " + parentGameObject.name : string.Empty)}.");
            }
                
            var go = new GameObject(ObjectName);
            if (parentGameObject)
            {
                go.transform.SetParent(parentGameObject.transform);
            }

            var comps = Components?.Split(',');
            if (comps is { Length: > 0 })
            {
                foreach (var c in comps)
                {
                    var trimmed = c.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    if (!UnityAiHelpers.TryGetComponentTypeByType(trimmed, out var type))
                        throw new Exception($"Could not find component type: {trimmed}");

                    go.AddComponent(type);
                }
            }

            if (UnityAiHelpers.TryParseVector3(Position, out var position))
                go.transform.position = position;

            if (UnityAiHelpers.TryParseVector3(Rotation, out var rotation))
                go.transform.eulerAngles = rotation;

            if (UnityAiHelpers.TryParseVector3(Scale, out var scale))
                go.transform.localScale = scale;

            if (UnityAiHelpers.TryParseVector3(LocalPosition, out var localPosition))
                go.transform.localPosition = localPosition;

            if (UnityAiHelpers.TryParseVector3(LocalRotation, out var localRotation))
                go.transform.localEulerAngles = localRotation;

            Undo.RegisterCreatedObjectUndo(go, "Create GameObject");
            
            return $"GameObject '{ObjectName}' created at {go.PathToGameObject()}";
#endif
        }
    }
}