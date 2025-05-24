using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction(@"Assigns a primitive mesh to a GameObject's MeshFilter using built-in Unity resources")]
    public class AssignPrimitiveMeshAction : GPTActionBase
    {
        private string description;

        [GPTParameter("The type of the primitive. Possible values: Cube, Sphere, Capsule, Cylinder, Plane, Quad")]
        public PrimitiveType PrimitiveType { get; set; }

        [GPTParameter("Name of the GameObject")]
        public string ObjectName { get; set; }

        public override string Description =>
            $"Assigned {Highlight(PrimitiveType.ToString())} mesh to {Highlight(ObjectName)}.";

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (!UnityAiHelpers.TryFindGameObject(ObjectName, out var gameObject))
            {
                throw new Exception($"GameObject '{ObjectName}' not found.");
            }

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning($"GameObject '{ObjectName}' has no MeshFilter. Adding one.");
                
                if (gameObject.TryGetComponent<SpriteRenderer>(out var _))
                {
                    throw new Exception($"GameObject '{ObjectName}' has a SpriteRenderer. Cannot add MeshFilter.");
                }
                
                meshFilter = gameObject.AddComponent<MeshFilter>();
                if (!meshFilter)
                {
                    throw new Exception($"Could not add a mesh filter to {ObjectName}");
                }
            }

            Mesh mesh = GetBuiltinMesh(PrimitiveType);
            if (mesh == null)
            {
                throw new Exception($"Could not find built-in mesh for PrimitiveType '{PrimitiveType}'");
            }

            meshFilter.sharedMesh = mesh;

            return $"Assigned {PrimitiveType} mesh to '{ObjectName}'.";
#endif
        }

        private Mesh GetBuiltinMesh(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Cube:
                    return Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                case PrimitiveType.Sphere:
                    return Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                case PrimitiveType.Capsule:
                    return Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                case PrimitiveType.Cylinder:
                    return Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
                case PrimitiveType.Plane:
                    return Resources.GetBuiltinResource<Mesh>("Plane.fbx");
                case PrimitiveType.Quad:
                    return Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                default:
                    return null;
            }
        }
    }
}