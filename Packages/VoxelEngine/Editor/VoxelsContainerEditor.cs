using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    [CustomEditor(typeof(VoxelsContainer))]
    [CanEditMultipleObjects]
    public class VoxelsContainerEditor : UnityEditor.Editor
    {
        private VoxelsContainer voxelsContainer;
        private SerializedProperty assetProperty;
        private SerializedProperty updateMeshOnStartProperty;
        private SerializedProperty useBakeJobProperty;
        private SerializedProperty isColliderDisabledProperty;
        private const string CenterToParentIconName = "d_ToolHandleCenter";
        private const string RefreshIconName = "d_Refresh";
        private const string TrimIconName = "d_ContentSizeFitter Icon";

        private void OnEnable() {
            voxelsContainer = target as VoxelsContainer;
            if(voxelsContainer == null) {
                return;
            }
            assetProperty = serializedObject.FindProperty("Asset");
            updateMeshOnStartProperty = serializedObject.FindProperty("updateMeshFilterOnStart");
            useBakeJobProperty = serializedObject.FindProperty("useBakeJob");
            isColliderDisabledProperty = serializedObject.FindProperty("isColliderDisabled");
        }
        
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.ObjectField(assetProperty);
            updateMeshOnStartProperty.boolValue = EditorGUILayout.Toggle(updateMeshOnStartProperty.displayName, updateMeshOnStartProperty.boolValue);
            useBakeJobProperty.boolValue = EditorGUILayout.Toggle(useBakeJobProperty.displayName, useBakeJobProperty.boolValue);
            isColliderDisabledProperty.boolValue = EditorGUILayout.Toggle(isColliderDisabledProperty.displayName, isColliderDisabledProperty.boolValue);
            serializedObject.ApplyModifiedProperties();
            if(!Application.isPlaying)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Button(EditorGUIUtility.IconContent(CenterToParentIconName))) {
                    AlignCenterWithParent();
                }
                if(GUILayout.Button(EditorGUIUtility.IconContent(TrimIconName), GUILayout.Height(20))) {
                    TrimAndRecenter().Forget();
                }
                if(GUILayout.Button(EditorGUIUtility.IconContent(RefreshIconName))) {
                    voxelsContainer.EditorRefresh();
                }
                GUILayout.EndHorizontal();
            }
        }

        private async UniTaskVoid TrimAndRecenter() {
            Utils.Trim(voxelsContainer.Asset);
            var startPos = voxelsContainer.MeshFilter.sharedMesh.bounds.center;
            await voxelsContainer.EditorRefreshAsync(false);
            var refreshedPos = voxelsContainer.MeshFilter.sharedMesh.bounds.center;
            var offset = startPos - refreshedPos;
            Undo.RegisterFullObjectHierarchyUndo(voxelsContainer.gameObject, "Recenter");
            var voxelsCenterWorldPos = voxelsContainer.transform.TransformVector(new Vector3(offset.x, offset.y, offset.z));
            voxelsContainer.transform.position += voxelsCenterWorldPos;
        }

        private void AlignCenterWithParent() {
            Undo.RegisterFullObjectHierarchyUndo(voxelsContainer.gameObject, "Align Center With Parent");
            var targetWorldPos = voxelsContainer.transform.parent == null ? Vector3.zero : voxelsContainer.transform.parent.position;
            var voxelsCenterWorldPos = voxelsContainer.transform.TransformPoint(voxelsContainer.MeshFilter.sharedMesh.bounds.center);
            var worldMoveVector = targetWorldPos - voxelsCenterWorldPos;
            voxelsContainer.transform.position += worldMoveVector;
        }
    }
}
