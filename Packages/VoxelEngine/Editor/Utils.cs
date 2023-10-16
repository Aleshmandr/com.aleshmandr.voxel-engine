using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public static class Utils
    {
        public static void Trim(TextAsset voxelsDataAsset) {
            string relativePath = AssetDatabase.GetAssetPath(voxelsDataAsset);
            string absolutePath = Path.Combine(Application.dataPath, "../", relativePath);
            absolutePath = Path.GetFullPath(absolutePath);

            var data = NativeArray3dSerializer.Deserialize<int>(voxelsDataAsset.bytes);
            int3 min = int.MaxValue;
            int3 max = 0;

            for(int i = 0; i < data.SizeX; i++) {
                for(int j = 0; j < data.SizeY; j++) {
                    for(int k = 0; k < data.SizeZ; k++) {
                        if(data[i, j, k] != 0) {
                            if(i < min.x) {
                                min.x = i;
                            }
                            if(j < min.y) {
                                min.y = j;
                            }
                            if(k < min.z) {
                                min.z = k;
                            }
                            if(i > max.x) {
                                max.x = i;
                            }
                            if(j > max.y) {
                                max.y = j;
                            }
                            if(k > max.z) {
                                max.z = k;
                            }
                        }
                    }
                }
            }

            int3 trimSize = max - min + 1;
            var trimedData = new NativeArray3d<int>(trimSize.x, trimSize.y, trimSize.z);
            for(int i = 0; i < trimSize.x; i++) {
                for(int j = 0; j < trimSize.y; j++) {
                    for(int k = 0; k < trimSize.z; k++) {
                        trimedData[i, j, k] = data[i + min.x, j + min.y, k + min.z];
                    }
                }
            }

            var bytes = NativeArray3dSerializer.Serialize(trimedData);
            File.WriteAllBytes(absolutePath, bytes);

            trimedData.Dispose();
            data.Dispose();

            AssetDatabase.SaveAssetIfDirty(voxelsDataAsset);
            AssetDatabase.Refresh();
        }

        public static void SaveMeshAsset(ref Mesh mesh, string meshAssetPath) {
            var existingAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            if(existingAsset != null) {
                mesh.name = existingAsset.name;
                EditorUtility.CopySerialized(mesh, existingAsset);
                mesh = existingAsset;
            } else {
                AssetDatabase.CreateAsset(mesh, meshAssetPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
