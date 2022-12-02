using UnityEditor;
using UnityEngine;
using VoxelEngine.Destructions;

namespace VoxelEngine.Editor
{
    [CustomEditor(typeof(VoxelsContainerJoint))]
    [CanEditMultipleObjects]
    public class VoxelsContainerJointEditor : UnityEditor.Editor
    {
        private const float HandlesScale = 0.6f;
        private VoxelsContainerJoint joint;
        private SerializedProperty jointsProperty;
        private SerializedProperty parentOnlyModeProperty;

        private void OnEnable() {
            joint = target as VoxelsContainerJoint;
            if(joint == null) {
                return;
            }
            jointsProperty = serializedObject.FindProperty("joints");
            parentOnlyModeProperty = serializedObject.FindProperty("parentOnlyMode");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            parentOnlyModeProperty.boolValue = EditorGUILayout.Toggle(parentOnlyModeProperty.displayName, parentOnlyModeProperty.boolValue);
            EditorGUILayout.PropertyField(jointsProperty);
            serializedObject.ApplyModifiedProperties();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Fix Joint")) {
                joint.FixJoint();
            }
            GUILayout.EndHorizontal();
        }

        private void OnSceneGUI() {
            JointData[] joints = joint.GetJointsEditor();
            if(joint == null) {
                return;
            }
            EditorGUI.BeginChangeCheck();
            var startMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.Scale(Vector3.one * HandlesScale) * startMatrix;
            for(int i = 0; i < joints.Length; i++) {

                var j = joints[i];
                var jointPos = joint.transform.TransformPoint(j.Center);

                Vector3 newJointPos = Handles.PositionHandle(jointPos / HandlesScale, joint.transform.rotation) *  HandlesScale;
                if(EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(joint, "Change Joint");
                    joints[i] = new JointData(j.Radius, joint.transform.InverseTransformPoint(newJointPos));
                }

            }
            Handles.matrix = startMatrix;
        }
    }
}
