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
        private SerializedProperty loadOnStartProperty;
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
            loadOnStartProperty = serializedObject.FindProperty("loadOnStart");
            updateMeshOnStartProperty = serializedObject.FindProperty("updateMeshFilterOnStart");
            useBakeJobProperty = serializedObject.FindProperty("useBakeJob");
            isColliderDisabledProperty = serializedObject.FindProperty("isColliderDisabled");
            if(!Application.isPlaying) {
                voxelsContainer.Data.Dispose();
                if(voxelsContainer.Asset != null) {
                    voxelsContainer.Data = NativeArray3dSerializer.Deserialize<int>(voxelsContainer.Asset.bytes);
                }
            }
        }

        private void OnDisable() {
            if(!Application.isPlaying) {
                voxelsContainer.Data.Dispose();
            }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.ObjectField(assetProperty);
            loadOnStartProperty.boolValue = EditorGUILayout.Toggle(loadOnStartProperty.displayName, loadOnStartProperty.boolValue);
            if(loadOnStartProperty.boolValue) {
                updateMeshOnStartProperty.boolValue = EditorGUILayout.Toggle(updateMeshOnStartProperty.displayName, updateMeshOnStartProperty.boolValue);
            }
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
            int minX, minY, minZ, maxX, maxY, maxZ;
            minX = minY = minZ = int.MaxValue;
            maxX = maxY = maxZ = int.MinValue;
            for(int x = 0; x < voxelsContainer.Data.SizeX; x++) {
                for(int y = 0; y < voxelsContainer.Data.SizeY; y++) {
                    for(int z = 0; z < voxelsContainer.Data.SizeZ; z++) {
                        if(voxelsContainer.Data[x, y, z] != 0) {
                            if(x < minX) {
                                minX = x;
                            }
                            if(x > maxX) {
                                maxX = x;
                            }

                            if(y < minY) {
                                minY = y;
                            }
                            if(y > maxY) {
                                maxY = y;
                            }

                            if(z < minZ) {
                                minZ = z;
                            }
                            if(z > maxZ) {
                                maxZ = z;
                            }
                        }
                    }
                }
            }

            Undo.RegisterFullObjectHierarchyUndo(voxelsContainer.gameObject, "Align Center With Parent");
            var targetWorldPos = voxelsContainer.transform.parent == null ? Vector3.zero : voxelsContainer.transform.parent.position;
            var voxelsCenterWorldPos = voxelsContainer.transform.TransformPoint(new Vector3(minX + maxX, minY + maxY, minZ + maxZ) * 0.5f);
            var worldMoveVector = targetWorldPos - voxelsCenterWorldPos;
            voxelsContainer.transform.position += worldMoveVector;
        }
    }
}
