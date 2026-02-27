using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPTUnity.Actions
{
    [GPTAction("Applies multiple serialized-property edits atomically. Rolls back all edits if one fails.")]
    public class BatchTransactionAction : GPTAssistantAction
    {
        [Serializable]
        private class BatchPayload
        {
            public BatchOperation[] operations;
        }

        [Serializable]
        private class BatchOperation
        {
            public string objectName;
            public string componentTypeName;
            public string assetPath;
            public string propertyPath;
            public string value;
        }

        [GPTParameter("JSON payload: {\"operations\":[{\"objectName\":\"Player\",\"componentTypeName\":\"MyComp\",\"propertyPath\":\"_speed\",\"value\":\"10\"}]}")]
        public string OperationsJson { get; set; }

        [GPTParameter("Record prefab overrides for scene component edits")]
        public bool RecordPrefabOverrides { get; set; } = true;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<BatchPayload>(OperationsJson);
            if (payload == null || payload.operations == null || payload.operations.Length == 0)
                throw new Exception("OperationsJson must contain at least one operation.");

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("BatchTransactionAction");

            var touchedObjects = new HashSet<Object>();
            var touchedScenes = new HashSet<UnityEngine.SceneManagement.Scene>();

            try
            {
                for (var i = 0; i < payload.operations.Length; i++)
                {
                    var operation = payload.operations[i];
                    if (string.IsNullOrWhiteSpace(operation.propertyPath))
                        throw new Exception($"Operation {i} has no propertyPath.");

                    if (!string.IsNullOrWhiteSpace(operation.assetPath))
                    {
                        ApplyAssetOperation(operation, touchedObjects);
                    }
                    else
                    {
                        ApplySceneComponentOperation(operation, touchedObjects, touchedScenes);
                    }
                }

                Undo.CollapseUndoOperations(undoGroup);
            }
            catch (Exception ex)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw new Exception($"Batch transaction rolled back. {ex.Message}");
            }

            foreach (var obj in touchedObjects.Where(obj => obj != null))
                EditorUtility.SetDirty(obj);

            foreach (var scene in touchedScenes.Where(scene => scene.IsValid()))
                EditorSceneManager.MarkSceneDirty(scene);

            AssetDatabase.SaveAssets();
            return $"Batch transaction succeeded. Applied {payload.operations.Length} operation(s).";
#endif
        }

        private void ApplySceneComponentOperation(BatchOperation operation, HashSet<Object> touchedObjects, HashSet<UnityEngine.SceneManagement.Scene> touchedScenes)
        {
            if (string.IsNullOrWhiteSpace(operation.objectName))
                throw new Exception("Scene operation requires objectName.");

            if (string.IsNullOrWhiteSpace(operation.componentTypeName))
                throw new Exception("Scene operation requires componentTypeName.");

            if (!UnityAiHelpers.TryGetComponentTypeByType(operation.componentTypeName, out var componentType))
                throw new Exception($"Component type '{operation.componentTypeName}' not found.");

            if (!UnityAiHelpers.TryFindGameObject(operation.objectName, out var gameObject))
                throw new Exception($"GameObject '{operation.objectName}' not found.");

            var component = gameObject.GetComponent(componentType);
            if (!component)
                throw new Exception($"GameObject '{operation.objectName}' has no '{operation.componentTypeName}' component.");

            Undo.RegisterCompleteObjectUndo(component, "Batch transaction scene edit");

            var serializedObject = new SerializedObject(component);
            var property = ActionEditingUtilities.FindPropertyWithAliases(serializedObject, operation.propertyPath);
            if (property == null)
                throw new Exception($"Property '{operation.propertyPath}' not found on '{operation.componentTypeName}'.");

            ActionEditingUtilities.SetSerializedPropertyValue(property, operation.value);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (RecordPrefabOverrides && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);

            touchedObjects.Add(component);
            if (component.gameObject.scene.IsValid())
                touchedScenes.Add(component.gameObject.scene);
        }

        private void ApplyAssetOperation(BatchOperation operation, HashSet<Object> touchedObjects)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(operation.assetPath);
            if (asset == null)
                throw new Exception($"Asset not found at '{operation.assetPath}'.");

            Undo.RegisterCompleteObjectUndo(asset, "Batch transaction asset edit");

            var serializedObject = new SerializedObject(asset);
            var property = ActionEditingUtilities.FindPropertyWithAliases(serializedObject, operation.propertyPath);
            if (property == null)
                throw new Exception($"Property '{operation.propertyPath}' not found on asset '{operation.assetPath}'.");

            ActionEditingUtilities.SetSerializedPropertyValue(property, operation.value);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            touchedObjects.Add(asset);
        }
    }
}
