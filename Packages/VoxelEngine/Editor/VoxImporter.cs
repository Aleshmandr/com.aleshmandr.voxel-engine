using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoxelEngine.Editor.Jobs;
using VoxReader.Interfaces;

namespace VoxelEngine.Editor
{
    public class VoxImporter : EditorWindow
    {
        private bool generateMeshAssets;
        private ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Low;
        private bool clusterize;
        private int clusterMaxVoxels = 2400;
        private bool optimizeVertices;
        private bool stepBasedDispersion = true;
        private int clusterDispersion;

        [MenuItem("Tools/VoxelEngine/Magica Voxel Importer (.vox)", false)]
        public static void ShowWindow() {
            GetWindow(typeof(VoxImporter));
        }

        private void OnGUI() {
            titleContent.text = "Magica Voxel Importer";
            EditorGUILayout.BeginVertical("Box");

            clusterize = EditorGUILayout.Toggle("Clusterize", clusterize);
            generateMeshAssets = EditorGUILayout.Toggle("Generate Mesh Assets", generateMeshAssets);
            if(generateMeshAssets) {
                optimizeVertices = EditorGUILayout.Toggle("Optimize Vertices", optimizeVertices);
                meshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup("Compression", meshCompression);
            }
            if(clusterize) {
                clusterMaxVoxels = EditorGUILayout.IntField("Max Voxels Per Cluster", clusterMaxVoxels);
            }
            EditorGUILayout.LabelField("Import .vox file");
            if(GUILayout.Button("Import")) {
                string filePath = EditorUtility.OpenFilePanel("Import file", "", "vox");
                if(string.IsNullOrEmpty(filePath)) {
                    return;
                }
                try {
                    EditorUtility.DisplayProgressBar("Importing .vox file", "Reading file", 0f);
                    List<RawVoxelsData> rawVoxelsDatas = LoadVoxFile(filePath);
                    var assetName = Path.GetFileNameWithoutExtension(filePath);
                    if(clusterize) {
                        CreatePartialClusterizedGameObject(rawVoxelsDatas, assetName);
                    } else {
                        var assetsDatas = GenerateAssets(assetName, rawVoxelsDatas);
                        CreateGameObject(assetsDatas);
                    }
                }
                finally {
                    EditorUtility.ClearProgressBar();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void CreatePartialClusterizedGameObject(List<RawVoxelsData> rawVoxelsDatas, string assetName) {
            GameObject parentObject = null;
            var generatePartIndex = rawVoxelsDatas.Count > 1;

            for(int partIndex = 0; partIndex < rawVoxelsDatas.Count; partIndex++) {

                var clusters = GenerateClusters(clusterMaxVoxels, rawVoxelsDatas[partIndex]);

                var clustersAssetsData = new List<GeneratedAssetsData>();

                for(int i = 0; i < clusters.Length; i++) {
                    EditorUtility.DisplayProgressBar("Importing .vox file", "Importing", ((float)(i + 1) / clusters.Length) / rawVoxelsDatas.Count);
                    RecalculateClusterPivot(clusters[i]);
                    var assetData = GenerateAssets(assetName, clusters[i], generatePartIndex ? partIndex : -1, i);
                    clustersAssetsData.Add(assetData);
                }
                if(rawVoxelsDatas.Count > 1) {
                    if(parentObject == null) {
                        parentObject = new GameObject(assetName);
                    }
                    CreateClusterizedGameObject(clustersAssetsData, $"{assetName}_{partIndex}", rawVoxelsDatas[partIndex].Position, parentObject.transform);
                } else {
                    CreateClusterizedGameObject(clustersAssetsData, assetName, Vector3.zero);
                }
            }
        }

        private void RecalculateClusterPivot(RawVoxelsData cluster) {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var minZ = int.MaxValue;
            foreach(var voxelData in cluster.Voxels) {
                if(voxelData.X < minX) {
                    minX = voxelData.X;
                }
                if(voxelData.Y < minY) {
                    minY = voxelData.Y;
                }
                if(voxelData.Z < minZ) {
                    minZ = voxelData.Z;
                }
            }

            foreach(var voxelData in cluster.Voxels) {
                voxelData.X -= minX;
                voxelData.Y -= minY;
                voxelData.Z -= minZ;
            }
            
            cluster.Position += new Vector3Int(minX, minZ, minY);
        }
        
        private List<RawVoxelsData> LoadVoxFile(string filePath) {
            IVoxFile voxFileData = VoxReader.VoxReader.Read(filePath);
            var parts = new List<RawVoxelsData>();
            foreach(IModel model in voxFileData.Models) {
                RawVoxelData[] modelVoxels = new RawVoxelData[model.Voxels.Length];

                for(int v = 0; v < model.Voxels.Length; v++) {
                    var voxel = model.Voxels[v];
                    int color = Utils.ConvertByteColorToInt(voxel.Color.R, voxel.Color.G, voxel.Color.B);
                    modelVoxels[v] = new RawVoxelData(voxel.Position.X, voxel.Position.Y, voxel.Position.Z, color);
                }
                
                RawVoxelsData rawVoxelsData = new RawVoxelsData(modelVoxels, new Vector3Int(model.Position.X, model.Position.Y, model.Position.Z));
                parts.Add(rawVoxelsData);
            }

            return parts;
        }
        
        private GeneratedAssetsData GenerateAssets(string originalAssetName, RawVoxelsData rawData, int partIndex = -1, int clusterIndex = -1) {
            if(rawData == null) {
                return null;
            }

            var size = rawData.Size;
            var data = new NativeArray3d<int>(size.x, size.z, size.y);
            for(int i = 0; i < rawData.Voxels.Count; i++) {
                data[rawData.Voxels[i].X, rawData.Voxels[i].Z, rawData.Voxels[i].Y] = rawData.Voxels[i].Color;
            }

            Mesh generatedMesh = null;
            if(generateMeshAssets) {
                generatedMesh = optimizeVertices ? Utilities.GenerateOptimizedMesh(data) : Utilities.GenerateMesh(data);
                MeshUtility.Optimize(generatedMesh);
                MeshUtility.SetMeshCompression(generatedMesh, meshCompression);
            }

            var bytes = NativeArray3dSerializer.Serialize(data);

            var assetParentFolderName = originalAssetName;

            string assetName;
            if(partIndex >= 0) {
                assetName = clusterIndex >= 0 ? $"{originalAssetName}_{partIndex}_{clusterIndex}" : $"{originalAssetName}_{partIndex}";
            } else {
                assetName = clusterIndex >= 0 ? $"{originalAssetName}_{clusterIndex}" : $"{originalAssetName}";
            }

            var localAssetDirectory = $"Imported/{assetParentFolderName}";
            var assetDirectoryPath = Application.dataPath + $"/{localAssetDirectory}";
            if(!Directory.Exists(assetDirectoryPath)) {
                Directory.CreateDirectory(assetDirectoryPath);
            }

            File.WriteAllBytes($"{assetDirectoryPath}/{assetName}.bytes", bytes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if(generatedMesh != null) {
                var meshAssetPath = $"Assets/{localAssetDirectory}/{assetName}.asset";
                Utils.SaveMeshAsset(ref generatedMesh, meshAssetPath);
            }

            var voxelsData = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/{localAssetDirectory}/{assetName}.bytes");

            data.Dispose();

            return new GeneratedAssetsData(originalAssetName, assetName, generatedMesh, voxelsData, rawData.Position);
        }

        private List<GeneratedAssetsData> GenerateAssets(string originalAssetName, List<RawVoxelsData> rawDatas) {
            if(rawDatas == null || rawDatas.Count == 0) {
                return null;
            }

            var assetsDatas = new List<GeneratedAssetsData>();

            var generatePartIndex = rawDatas.Count > 1;
            for(int partIndex = 0; partIndex < rawDatas.Count; partIndex++) {
                var rawData = rawDatas[partIndex];
                assetsDatas.Add(GenerateAssets(originalAssetName, rawData, generatePartIndex ? partIndex : -1));
            }

            return assetsDatas;
        }

        private void CreateClusterizedGameObject(List<GeneratedAssetsData> clustersAssetsData, string objectName, Vector3 position, Transform parent = null) {
            if(clustersAssetsData == null || clustersAssetsData.Count == 0) {
                return;
            }
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            for(int i = 0; i < clustersAssetsData.Count; i++) {
                CreateGameObject(clustersAssetsData[i], clustersAssetsData[i].AssetName, root.transform);
            }
        }

        private void CreateGameObject(List<GeneratedAssetsData> assetsDatas) {
            if(assetsDatas == null || assetsDatas.Count == 0) {
                return;
            }

            if(assetsDatas.Count > 1) {
                GameObject gameObject = new GameObject(assetsDatas[0].OriginalAssetName);
                for(int i = 0; i < assetsDatas.Count; i++) {
                    CreateGameObject(assetsDatas[i], assetsDatas[i].AssetName, gameObject.transform);
                }
            } else {
                CreateGameObject(assetsDatas[0], assetsDatas[0].OriginalAssetName);
            }
        }

        private void CreateGameObject(GeneratedAssetsData assetsData, string objectName, Transform parent = null) {
            if(assetsData == null) {
                return;
            }
            GameObject gameObject = new GameObject(objectName);
            if(parent != null) {
                gameObject.transform.SetParent(parent);
            }
            gameObject.transform.localPosition = assetsData.Position;
            gameObject.AddComponent<MeshFilter>().mesh = assetsData.MeshAsset;
            gameObject.AddComponent<MeshCollider>().sharedMesh = assetsData.MeshAsset;
            var container = gameObject.AddComponent<VoxelsContainer>();
            container.Asset = assetsData.DataAsset;
            if(assetsData.MeshAsset == null) {
                container.EditorEnableLoadOnStart();
            }
        }

        private RawVoxelsData[] GenerateClusters(int maxVoxels, RawVoxelsData voxelsData) {
            var size = voxelsData.Size;
            var voxelsArray = new NativeArray3d<int>(size.x, size.y, size.z);
            foreach(RawVoxelData voxel in voxelsData.Voxels) {
                voxelsArray[voxel.X, voxel.Y, voxel.Z] = voxel.Color;
            }
            var clusters = new List<RawVoxelsData>();
            var clusterTraceJobsScheduler = new TraceVoxelsClusterJobsScheduler();

            for(int i = 0; i < voxelsArray.SizeX; i++) {
                for(int j = 0; j < voxelsArray.SizeY; j++) {
                    for(int k = 0; k < voxelsArray.SizeZ; k++) {
                        if(voxelsArray[i, j, k] != 0) {
                            var cluster = new RawVoxelsData();
                            clusters.Add(cluster);
                            clusterTraceJobsScheduler.Run(cluster, voxelsArray, i, j, k, maxVoxels);
                        }
                    }
                }
            }

            voxelsArray.Dispose();
            return clusters.ToArray();
        }
    }
}
