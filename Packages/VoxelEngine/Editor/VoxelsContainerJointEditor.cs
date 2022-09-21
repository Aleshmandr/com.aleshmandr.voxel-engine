using UnityEditor;
using UnityEngine;
using VoxelEngine.Destructions;

namespace VoxelEngine.Editor
{
    [CustomEditor(typeof(VoxelsContainerJoint))]
    [CanEditMultipleObjects]
    public class VoxelsContainerJointEditor : UnityEditor.Editor
    {
        private VoxelsContainerJoint joint;
        private SerializedProperty radiusProperty;
        private SerializedProperty centerProperty;

        private void OnEnable() {
            joint = target as VoxelsContainerJoint;
            if(joint == null) {
                return;
            }
            radiusProperty = serializedObject.FindProperty("radius");
            centerProperty = serializedObject.FindProperty("center");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            radiusProperty.floatValue = EditorGUILayout.FloatField(radiusProperty.displayName, radiusProperty.floatValue);
            centerProperty.vector3Value = EditorGUILayout.Vector3Field(centerProperty.displayName, centerProperty.vector3Value);
            serializedObject.ApplyModifiedProperties();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Fix Joint")) {
                joint.FixJoint();
            }
            GUILayout.EndHorizontal();
        }
    }
}
