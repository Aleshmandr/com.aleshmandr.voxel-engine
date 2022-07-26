using UnityEditor;

namespace VoxelEngine.Destructions.Editor
{
    [CustomEditor(typeof(DestructableVoxels))]
    [CanEditMultipleObjects]
    public class DestructableVoxelsEditor : UnityEditor.Editor
    {
        private SerializedProperty voxelContainerProperty;
        private SerializedProperty collapseThreshProperty;
        private SerializedProperty makePhysicalOnCollapseProperty;
    
        private void OnEnable() {
            voxelContainerProperty = serializedObject.FindProperty("voxelsContainer");
            collapseThreshProperty = serializedObject.FindProperty("collapsePercentsThresh");
            makePhysicalOnCollapseProperty = serializedObject.FindProperty("makePhysicalOnCollapse");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(voxelContainerProperty);
            makePhysicalOnCollapseProperty.boolValue = EditorGUILayout.Toggle(makePhysicalOnCollapseProperty.displayName, makePhysicalOnCollapseProperty.boolValue);
            if(makePhysicalOnCollapseProperty.boolValue) {
                collapseThreshProperty.floatValue = EditorGUILayout.Slider(collapseThreshProperty.displayName, collapseThreshProperty.floatValue, 0f, 100f);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
