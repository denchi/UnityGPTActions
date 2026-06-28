using System;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPTUnity.Actions
{
    [GPTAction("Resolves a scene object, asset path, or GUID into stable Unity reference identifiers.", Name = "resolve_object_reference")]
    public class ResolveObjectReferenceAction : GPTAssistantAction
    {
        [GPTParameter("Scene object path or name, asset path under Assets, or asset GUID.", true, Name = "object_identifier")]
        public string ObjectIdentifier { get; set; }

        [GPTParameter("Optional expected object type such as 'GameObject', 'Material', or 'MyComponent'.", Name = "object_type_name")]
        public string ObjectTypeName { get; set; }

        [GPTParameter("Optional component type name when resolving a component attached to a scene object.", Name = "component_type_name")]
        public string ComponentTypeName { get; set; }

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(ObjectIdentifier))
                throw new Exception("ObjectIdentifier is required.");

            var expectedType = ResolveExpectedType();
            var resolved = ResolveObject(expectedType);
            if (resolved == null)
                throw new Exception($"Could not resolve object '{ObjectIdentifier}'.");

            var sb = new StringBuilder();
            sb.AppendLine($"name: {resolved.name}");
            sb.AppendLine($"type: {resolved.GetType().FullName}");
            sb.AppendLine($"instanceId: {resolved.GetInstanceID()}");
            sb.AppendLine($"globalObjectId: {GlobalObjectId.GetGlobalObjectIdSlow(resolved)}");

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(resolved, out string guid, out long localId))
            {
                sb.AppendLine($"guid: {guid}");
                sb.AppendLine($"localFileId: {localId}");
            }
            else
            {
                sb.AppendLine("guid: <none>");
                sb.AppendLine("localFileId: <none>");
            }

            var assetPath = AssetDatabase.GetAssetPath(resolved);
            sb.AppendLine($"assetPath: {(string.IsNullOrWhiteSpace(assetPath) ? "<scene>" : assetPath)}");

            if (resolved is Component component)
            {
                sb.AppendLine($"hierarchyPath: {ActionEditingUtilities.GetGameObjectHierarchyPath(component.gameObject)}");
            }
            else if (resolved is GameObject gameObject)
            {
                sb.AppendLine($"hierarchyPath: {ActionEditingUtilities.GetGameObjectHierarchyPath(gameObject)}");
            }

            return sb.ToString();
        }

        private Type ResolveExpectedType()
        {
            if (string.IsNullOrWhiteSpace(ObjectTypeName))
                return typeof(Object);

            if (UnityAiHelpers.TryGetObjectTypeByType(ObjectTypeName, out var objectType))
                return objectType;

            if (UnityAiHelpers.TryGetComponentTypeByType(ObjectTypeName, out var componentType))
                return componentType;

            throw new Exception($"Type '{ObjectTypeName}' could not be resolved.");
        }

        private Object ResolveObject(Type expectedType)
        {
            if (!string.IsNullOrWhiteSpace(ComponentTypeName))
            {
                if (!UnityAiHelpers.TryGetComponentTypeByType(ComponentTypeName, out var componentType))
                    throw new Exception($"Component type '{ComponentTypeName}' not found.");

                if (!UnityAiHelpers.TryFindGameObject(ObjectIdentifier, out var componentGo))
                    throw new Exception($"GameObject '{ObjectIdentifier}' not found.");

                return componentGo.GetComponent(componentType);
            }

            if (ActionEditingUtilities.TryResolveUnityObject(ObjectIdentifier, expectedType, out var resolved))
                return resolved;

            return null;
        }
    }
}
