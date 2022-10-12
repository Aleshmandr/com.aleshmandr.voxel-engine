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
        private SerializedProperty jointsProperty;

        private void OnEnable() {
            joint = target as VoxelsContainerJoint;
            if(joint == null) {
                return;
            }
            jointsProperty = serializedObject.FindProperty("joints");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(jointsProperty);
            serializedObject.ApplyModifiedProperties();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Fix Joint")) {
                joint.FixJoint();
            }
            GUILayout.EndHorizontal();
        }
    }
}
