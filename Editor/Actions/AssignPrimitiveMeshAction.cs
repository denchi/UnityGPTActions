using System;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction(@"Assigns a built-in Unity primitive mesh to a GameObject MeshFilter.", Name = "assign_primitive_mesh")]
    public class AssignPrimitiveMeshAction : GPTAssistantAction
    {
        private string description;

        [GPTParameter("Primitive mesh type to assign: Cube, Sphere, Capsule, Cylinder, Plane, or Quad.", true, Name = "primitive_type")]
        public PrimitiveType PrimitiveType { get; set; }

        [GPTParameter("GameObject name or hierarchy path.", true, Name = "object_name_or_path")]
        public string ObjectName { get; set; }

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
