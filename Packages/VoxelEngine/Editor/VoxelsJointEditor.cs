using UnityEditor;
using UnityEngine;
using VoxelEngine.Destructions;

namespace VoxelEngine.Editor
{
    [CustomEditor(typeof(VoxelsJoint))]
    [CanEditMultipleObjects]
    public class VoxelsJointEditor : UnityEditor.Editor
    {
        private const float HandlesScale = 0.6f;
        private VoxelsJoint joint;
        private SerializedProperty radiusProperty;
        private SerializedProperty centerProperty;
        private SerializedProperty disableOnFixationBreakProperty;

        private void OnEnable() {
            joint = target as VoxelsJoint;
            if(joint == null) {
                return;
            }
            radiusProperty = serializedObject.FindProperty("fixationRadius");
            centerProperty = serializedObject.FindProperty("center");
            disableOnFixationBreakProperty = serializedObject.FindProperty("disableOnFixationBreak");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            radiusProperty.floatValue = EditorGUILayout.FloatField(radiusProperty.displayName, radiusProperty.floatValue);
            centerProperty.vector3Value = EditorGUILayout.Vector3Field(centerProperty.displayName, centerProperty.vector3Value);
            disableOnFixationBreakProperty.boolValue = EditorGUILayout.Toggle(disableOnFixationBreakProperty.displayName, disableOnFixationBreakProperty.boolValue);
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI() {
            EditorGUI.BeginChangeCheck();
            var startMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.Scale(Vector3.one * HandlesScale) * startMatrix;
            var jointPos = joint.transform.TransformPoint(joint.CenterEditor);

            Vector3 newJointPos = Handles.PositionHandle(jointPos / HandlesScale, joint.transform.rotation) *  HandlesScale;
            if(EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(joint, "Change Joint");
                joint.CenterEditor = joint.transform.InverseTransformPoint(newJointPos);
            }
            Handles.matrix = startMatrix;
        }
    }
}
