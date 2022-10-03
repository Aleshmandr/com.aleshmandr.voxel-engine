using UnityEditor;
using VoxelEngine.Destructions;

namespace VoxelEngine.Editor
{
    [CustomEditor(typeof(DestructableVoxels))]
    [CanEditMultipleObjects]
    public class DestructableVoxelsEditor : UnityEditor.Editor
    {
        private SerializedProperty voxelContainerProperty;
        private SerializedProperty collapseThreshProperty;
        private SerializedProperty makePhysicalOnCollapseProperty;
        private SerializedProperty interpolationProperty;
        private SerializedProperty destructionColliderProperty;
    
        private void OnEnable() {
            voxelContainerProperty = serializedObject.FindProperty("voxelsContainer");
            collapseThreshProperty = serializedObject.FindProperty("collapsePercentsThresh");
            makePhysicalOnCollapseProperty = serializedObject.FindProperty("makePhysicalOnCollapse");
            interpolationProperty = serializedObject.FindProperty("interpolation");
            destructionColliderProperty = serializedObject.FindProperty("destructionCollider");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(voxelContainerProperty);
            makePhysicalOnCollapseProperty.boolValue = EditorGUILayout.Toggle(makePhysicalOnCollapseProperty.displayName, makePhysicalOnCollapseProperty.boolValue);
            if(makePhysicalOnCollapseProperty.boolValue) {
                collapseThreshProperty.floatValue = EditorGUILayout.Slider(collapseThreshProperty.displayName, collapseThreshProperty.floatValue, 0f, 100f);
                EditorGUILayout.PropertyField(interpolationProperty);

                var destructionColliderType = (DestructionColliderType) destructionColliderProperty.enumValueIndex;
                destructionColliderProperty.enumValueIndex = (int)(DestructionColliderType) EditorGUILayout.EnumPopup(destructionColliderProperty.displayName, destructionColliderType);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
