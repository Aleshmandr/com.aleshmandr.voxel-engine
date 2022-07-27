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
        private const string CenterToParentIconName = "d_ToolHandleCenter@2x";

        private void OnEnable() {
            voxelsContainer = target as VoxelsContainer;
            if(voxelsContainer == null || voxelsContainer.Asset == null) {
                return;
            }
            assetProperty = serializedObject.FindProperty("Asset");
            loadOnStartProperty = serializedObject.FindProperty("loadOnStart");
            voxelsContainer.Data = NativeArray3dSerializer.Deserialize<int>(voxelsContainer.Asset.bytes);
        }

        private void OnDisable() {
            voxelsContainer.Data.Dispose();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(assetProperty);
            loadOnStartProperty.boolValue = EditorGUILayout.Toggle(loadOnStartProperty.displayName, loadOnStartProperty.boolValue);
            serializedObject.ApplyModifiedProperties();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(EditorGUIUtility.IconContent(CenterToParentIconName))) {
                AlignCenterWithParent();
            }
            GUILayout.EndHorizontal();
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
            var voxelsCenterWorldPos = voxelsContainer.transform.TransformPoint(new Vector3(minX + maxX - 1, minY + maxY - 1, minZ + maxZ - 1) * 0.5f);
            var worldMoveVector = targetWorldPos - voxelsCenterWorldPos;
            voxelsContainer.transform.position += worldMoveVector;
        }
    }
}
