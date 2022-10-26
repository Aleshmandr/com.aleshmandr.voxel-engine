using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelEngine.Destructions;

namespace VoxelEngine.Editor
{
    [CustomEditor(typeof(VoxelsClustersDestructionContainer))]
    [CanEditMultipleObjects]
    public class VoxelsClustersDestructionContainerEditor : UnityEditor.Editor
    {
        private VoxelsClustersDestructionContainer container;
        private SerializedProperty connectionsUpdateDelayProperty;
        private SerializedProperty updateConnectionsInRuntimeProperty;
        private SerializedProperty updateIntegrityInRuntimeProperty;
        private SerializedProperty connectionsProperty;

        private void OnEnable() {
            container = target as VoxelsClustersDestructionContainer;
            if(container == null) {
                return;
            }
            connectionsUpdateDelayProperty = serializedObject.FindProperty("connectionsUpdateDelay");
            updateConnectionsInRuntimeProperty = serializedObject.FindProperty("updateConnectionsInRuntime");
            updateIntegrityInRuntimeProperty = serializedObject.FindProperty("updateIntegrityInRuntime");
            connectionsProperty = serializedObject.FindProperty("connections");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            connectionsUpdateDelayProperty.floatValue = EditorGUILayout.FloatField(connectionsUpdateDelayProperty.displayName, connectionsUpdateDelayProperty.floatValue);
            updateConnectionsInRuntimeProperty.boolValue = EditorGUILayout.Toggle(updateConnectionsInRuntimeProperty.displayName, updateConnectionsInRuntimeProperty.boolValue);
            updateIntegrityInRuntimeProperty.boolValue = EditorGUILayout.Toggle(updateIntegrityInRuntimeProperty.displayName, updateIntegrityInRuntimeProperty.boolValue);
           
            
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Bake (Preserve Fixed)")) {
                var fixedParts = new List<DestructableVoxels>();
                for(int i = 0; i < container.Connections.Length; i++) {
                    if(container.Connections[i].IsFixed) {
                        fixedParts.Add(container.Connections[i].Root);
                    }
                }
                container.BakeConnectionsWithJobs();
                for(int i = 0; i < container.Connections.Length; i++) {
                    var root = container.Connections[i].Root;
                    if(fixedParts.Contains(root)) {
                        container.Connections[i].IsFixed = true;
                        fixedParts.Remove(root);
                    }
                }
            }
            if(GUILayout.Button("Bake")) {
                container.BakeConnectionsWithJobs();
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(connectionsProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
