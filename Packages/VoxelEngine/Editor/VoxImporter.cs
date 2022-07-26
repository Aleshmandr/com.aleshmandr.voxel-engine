using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelEngine.Editor
{
    public class VoxImporter : EditorWindow
    {
        private static readonly ushort[] DefaultPalette = {
            32767, 25599, 19455, 13311, 7167, 1023, 32543, 25375, 19231, 13087, 6943, 799, 32351, 25183,
            19039, 12895, 6751, 607, 32159, 24991, 18847, 12703, 6559, 415, 31967, 24799, 18655, 12511, 6367, 223, 31775, 24607, 18463, 12319, 6175, 31,
            32760, 25592, 19448, 13304, 7160, 1016, 32536, 25368, 19224, 13080, 6936, 792, 32344, 25176, 19032, 12888, 6744, 600, 32152, 24984, 18840,
            12696, 6552, 408, 31960, 24792, 18648, 12504, 6360, 216, 31768, 24600, 18456, 12312, 6168, 24, 32754, 25586, 19442, 13298, 7154, 1010, 32530,
            25362, 19218, 13074, 6930, 786, 32338, 25170, 19026, 12882, 6738, 594, 32146, 24978, 18834, 12690, 6546, 402, 31954, 24786, 18642, 12498, 6354,
            210, 31762, 24594, 18450, 12306, 6162, 18, 32748, 25580, 19436, 13292, 7148, 1004, 32524, 25356, 19212, 13068, 6924, 780, 32332, 25164, 19020,
            12876, 6732, 588, 32140, 24972, 18828, 12684, 6540, 396, 31948, 24780, 18636, 12492, 6348, 204, 31756, 24588, 18444, 12300, 6156, 12, 32742,
            25574, 19430, 13286, 7142, 998, 32518, 25350, 19206, 13062, 6918, 774, 32326, 25158, 19014, 12870, 6726, 582, 32134, 24966, 18822, 12678, 6534,
            390, 31942, 24774, 18630, 12486, 6342, 198, 31750, 24582, 18438, 12294, 6150, 6, 32736, 25568, 19424, 13280, 7136, 992, 32512, 25344, 19200,
            13056, 6912, 768, 32320, 25152, 19008, 12864, 6720, 576, 32128, 24960, 18816, 12672, 6528, 384, 31936, 24768, 18624, 12480, 6336, 192, 31744,
            24576, 18432, 12288, 6144, 28, 26, 22, 20, 16, 14, 10, 8, 4, 2, 896, 832, 704, 640, 512, 448, 320, 256, 128, 64, 28672, 26624, 22528, 20480,
            16384, 14336, 10240, 8192, 4096, 2048, 29596, 27482, 23254, 21140, 16912, 14798, 10570, 8456, 4228, 2114, 1
        };

        private bool compress = true;
        private bool clusterize;
        private int clusterVoxelsStep = 20;
        private bool stepBasedDispersion = true;
        private int clusterDispersion;
        private int clusterGenerationSeed = 12345;

        [MenuItem("Tools/VoxelEngine/Magica Voxel Importer (.vox)", false)]
        public static void ShowWindow() {
            GetWindow(typeof(VoxImporter));
        }

        private void OnGUI() {
            titleContent.text = "Magica Voxel Importer";
            EditorGUILayout.BeginVertical("Box");

            compress = EditorGUILayout.Toggle("Compress", compress);
            clusterize = EditorGUILayout.Toggle("Clusterize", clusterize);
            if(clusterize) {
                clusterVoxelsStep = EditorGUILayout.IntField("Clusters Voxels Step", clusterVoxelsStep);
                clusterGenerationSeed = EditorGUILayout.IntField("Clusters Generation Seed", clusterGenerationSeed);
                stepBasedDispersion = EditorGUILayout.Toggle("Step Based Dispersion", stepBasedDispersion);
                if(!stepBasedDispersion) {
                    clusterDispersion = EditorGUILayout.IntField("Dispersion", clusterDispersion);
                }
            }
            EditorGUILayout.LabelField("Import .vox file");
            if(GUILayout.Button("Import")) {
                string filePath = EditorUtility.OpenFilePanel("Import file", "", "vox");
                if(string.IsNullOrEmpty(filePath)) {
                    return;
                }
                try {
                    EditorUtility.DisplayProgressBar("Importing .vox file", "Reading file", 0f);
                    var rawVoxelsData = LoadVoxFile(filePath);
                    var assetName = Path.GetFileNameWithoutExtension(filePath);
                    if(clusterize) {
                        var clusters = GenerateClusters(clusterVoxelsStep, clusterGenerationSeed, rawVoxelsData);
                        var clustersAssetsData = new List<GeneratedAssetsData>();
                        for(int i = 0; i < clusters.Length; i++) {
                            EditorUtility.DisplayProgressBar("Importing .vox file", "Importing", (float)i / clusters.Length);
                            RecalculateClusterPivot(clusters[i]);
                            var assetData = GenerateAssets(assetName, clusters[i], i);
                            clustersAssetsData.Add(assetData);
                        }
                        CreateClusterizedGameObject(clustersAssetsData);
                    } else {
                        EditorUtility.DisplayProgressBar("Importing .vox file", "Importing", 0.5f);
                        var assetsData = GenerateAssets(assetName, rawVoxelsData);
                        CreateGameObject(assetsData);
                    }
                }
                finally {
                    EditorUtility.ClearProgressBar();
                }
            }
            EditorGUILayout.EndVertical();
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
            cluster.Pivot = new Vector3Int(minX, minY, minZ);
        }

        private RawVoxelsData LoadVoxFile(string filePath) {
            var stream = new BinaryReader(File.OpenRead(filePath));

            int[] colors = null;
            RawVoxelData[] rawData = null;
            string magic = new string(stream.ReadChars(4));
            int version = stream.ReadInt32();

            if(magic != "VOX ") {
                return null;
            }

            while(stream.BaseStream.Position < stream.BaseStream.Length) {
                char[] chunkId = stream.ReadChars(4);
                int chunkSize = stream.ReadInt32();
                int childChunks = stream.ReadInt32();
                string chunkName = new string(chunkId);

                switch(chunkName) {
                    case "SIZE":
                        int sizeX = stream.ReadInt32();
                        int sizeY = stream.ReadInt32();
                        int sizeZ = stream.ReadInt32();
                        stream.ReadBytes(chunkSize - 4 * 3);
                        break;
                    case "XYZI":
                        int numVoxels = stream.ReadInt32();
                        rawData = new RawVoxelData[numVoxels];
                        for(int i = 0; i < rawData.Length; i++) {
                            rawData[i] = new RawVoxelData(stream);
                        }
                        break;
                    case "RGBA":
                        colors = new int[256];

                        for(int i = 0; i < 256; i++) {
                            byte r = stream.ReadByte();
                            byte g = stream.ReadByte();
                            byte b = stream.ReadByte();
                            byte a = stream.ReadByte();

                            //Convert RGBA to custom format (16 bits, 0RRR RRGG GGGB BBBB)
                            colors[i] = ((r & 0xFF) << 24) | ((g & 0xFF) << 16) | (b & 0xFF) << 8;
                        }
                        break;
                    default:
                        stream.ReadBytes(chunkSize);
                        break;
                }
            }

            stream.Close();

            if(rawData == null || rawData.Length == 0) {
                return null;
            }

            for(int i = 0; i < rawData.Length; i++) {
                int voxelColor = colors?[rawData[i].ColorCode - 1] ?? DefaultPalette[rawData[i].ColorCode - 1];
                rawData[i].Color = voxelColor;
            }

            return new RawVoxelsData(rawData);
        }

        private GeneratedAssetsData GenerateAssets(string originalAssetName, RawVoxelsData rawData, int clusterIndex = -1) {
            if(rawData == null) {
                return null;
            }

            var size = rawData.Size;
            var data = new NativeArray3d<int>(size.x, size.z, size.y);
            for(int i = 0; i < rawData.Voxels.Count; i++) {
                data[rawData.Voxels[i].X, rawData.Voxels[i].Z, rawData.Voxels[i].Y] = rawData.Voxels[i].Color;
            }

            var generatedMesh = Utilities.GenerateMesh(data);
            var bytes = NativeArray3dSerializer.Serialize(data, compress);

            var assetParentFolderName = originalAssetName;
            var assetName = clusterIndex >= 0 ? $"{originalAssetName}_{clusterIndex}" : originalAssetName;

            var localAssetDirectory = $"Imported/{assetParentFolderName}";
            var assetDirectoryPath = Application.dataPath + $"/{localAssetDirectory}";
            if(!Directory.Exists(assetDirectoryPath)) {
                Directory.CreateDirectory(assetDirectoryPath);
            }
            File.WriteAllBytes($"{assetDirectoryPath}/{assetName}.bytes", bytes);
            AssetDatabase.CreateAsset(generatedMesh, $"Assets/{localAssetDirectory}/{assetName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var voxelsData = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/{localAssetDirectory}/{assetName}.bytes");

            data.Dispose();

            return new GeneratedAssetsData(originalAssetName, assetName, generatedMesh, voxelsData, rawData.Pivot);
        }

        private void CreateClusterizedGameObject(List<GeneratedAssetsData> clustersAssetsData) {
            if(clustersAssetsData == null || clustersAssetsData.Count == 0) {
                return;
            }
            GameObject parentObject = new GameObject(clustersAssetsData[0].OriginalAssetName);
            for(int i = 0; i < clustersAssetsData.Count; i++) {
                CreateGameObject(clustersAssetsData[i], parentObject.transform);
            }
        }

        private void CreateGameObject(GeneratedAssetsData assetsData, Transform parent = null) {
            if(assetsData == null) {
                return;
            }
            GameObject gameObject = new GameObject(assetsData.AssetName);
            if(parent != null) {
                gameObject.transform.SetParent(parent);
            }
            gameObject.transform.localPosition = new Vector3(assetsData.Offset.x, assetsData.Offset.z, assetsData.Offset.y);
            gameObject.AddComponent<MeshFilter>().mesh = assetsData.MeshAsset;
            gameObject.AddComponent<MeshCollider>().sharedMesh = assetsData.MeshAsset;
            var container = gameObject.AddComponent<VoxelsContainer>();
            container.Asset = assetsData.DataAsset;
        }

        private RawVoxelsData[] GenerateClusters(int clusterStep, int seed, RawVoxelsData voxelsData) {
            Random.InitState(seed);
            var size = voxelsData.Size;
            Vector3Int boxSize = new Vector3Int(size.x, size.y, size.z);

            int stepsX = Mathf.Max(Mathf.CeilToInt(boxSize.x / (float)clusterStep), 1);
            int stepsY = Mathf.Max(Mathf.CeilToInt(boxSize.y / (float)clusterStep), 1);
            int stepsZ = Mathf.Max(Mathf.CeilToInt(boxSize.z / (float)clusterStep), 1);

            int clustersCount = stepsX * stepsY * stepsZ;
            Vector3Int[] clustersCenters = new Vector3Int[clustersCount];
            int dispersion = stepBasedDispersion ? clusterStep / 2 : clusterDispersion;

            for(int i = 0; i < stepsX; i++) {
                for(int j = 0; j < stepsY; j++) {
                    for(int k = 0; k < stepsZ; k++) {
                        int index = i + stepsX * j + stepsY * stepsX * k;
                        clustersCenters[index] = new Vector3Int(
                            i * clusterStep + Random.Range(0, dispersion),
                            j * clusterStep + Random.Range(0, dispersion),
                            k * clusterStep + Random.Range(0, dispersion)
                        );
                    }
                }
            }

            var clusters = new Dictionary<int, RawVoxelsData>();

            for(int i = 0; i < voxelsData.Voxels.Count; i++) {
                var vox = voxelsData.Voxels[i];
                int voxMinDist = int.MaxValue;
                int nearestClusterIndex = 0;
                for(int c = 0; c < clustersCenters.Length; c++) {
                    var cluster = clustersCenters[c];
                    int dist = (int)Vector3Int.Distance(cluster, new Vector3Int(vox.X, vox.Y, vox.Z));
                    if(dist < voxMinDist) {
                        voxMinDist = dist;
                        nearestClusterIndex = c;
                    }
                }

                if(!clusters.TryGetValue(nearestClusterIndex, out var clusterData)) {
                    clusterData = new RawVoxelsData();
                    clusters.Add(nearestClusterIndex, clusterData);
                }
                clusterData.Voxels.Add(vox);
            }

            return clusters.Values.ToArray();
        }
    }
}
