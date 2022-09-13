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
            32767, 25599, 19455, 13311, 7167, 1023, 32543, 25375, 19231, 13087, 6943, 799, 32351, 25183, 19039, 12895, 6751, 607, 32159, 24991, 18847, 12703, 6559, 415, 31967, 24799, 18655, 12511, 6367, 223, 31775, 24607, 18463, 12319, 6175, 31, 32760, 25592, 19448, 13304, 7160, 1016, 32536, 25368, 19224, 13080,
            6936, 792, 32344, 25176, 19032, 12888, 6744, 600, 32152, 24984, 18840, 12696, 6552, 408, 31960, 24792, 18648, 12504, 6360, 216, 31768, 24600, 18456, 12312, 6168, 24, 32754, 25586, 19442, 13298, 7154, 1010, 32530, 25362, 19218, 13074, 6930, 786, 32338, 25170, 19026, 12882, 6738, 594, 32146, 24978,
            18834, 12690, 6546, 402, 31954, 24786, 18642, 12498, 6354, 210, 31762, 24594, 18450, 12306, 6162, 18, 32748, 25580, 19436, 13292, 7148, 1004, 32524, 25356, 19212, 13068, 6924, 780, 32332, 25164, 19020, 12876, 6732, 588, 32140, 24972, 18828, 12684, 6540, 396, 31948, 24780, 18636, 12492, 6348, 204,
            31756, 24588, 18444, 12300, 6156, 12, 32742, 25574, 19430, 13286, 7142, 998, 32518, 25350, 19206, 13062, 6918, 774, 32326, 25158, 19014, 12870, 6726, 582, 32134, 24966, 18822, 12678, 6534, 390, 31942, 24774, 18630, 12486, 6342, 198, 31750, 24582, 18438, 12294, 6150, 6, 32736, 25568, 19424, 13280,
            7136, 992, 32512, 25344, 19200, 13056, 6912, 768, 32320, 25152, 19008, 12864, 6720, 576, 32128, 24960, 18816, 12672, 6528, 384, 31936, 24768, 18624, 12480, 6336, 192, 31744, 24576, 18432, 12288, 6144, 28, 26, 22, 20, 16, 14, 10, 8, 4, 2, 896, 832, 704, 640, 512, 448, 320, 256, 128, 64, 28672, 26624,
            22528, 20480, 16384, 14336, 10240, 8192, 4096, 2048, 29596, 27482, 23254, 21140, 16912, 14798, 10570, 8456, 4228, 2114, 1
        };

        private static readonly Dictionary<byte, Vector4> MagicTRansformMap = new Dictionary<byte, Vector4>() {
            {
                40, new Vector4(3, 0, 0, 0)
            }, {
                2, new Vector4(3, 3, 0, 0)
            }, {
                24, new Vector4(3, 2, 0, 0)
            }, {
                50, new Vector4(3, 1, 0, 0)
            }, {
                120, new Vector4(1, 0, 2, 0)
            }, {
                98, new Vector4(1, 0, 3, 0)
            }, {
                72, new Vector4(1, 0, 0, 0)
            }, {
                82, new Vector4(1, 0, 1, 0)
            }, {
                4, new Vector4(0, 0, 0, 0)
            }, {
                22, new Vector4(0, 0, 1, 0)
            }, {
                84, new Vector4(0, 0, 2, 0)
            }, {
                70, new Vector4(0, 0, 3, 0)
            }, {
                52, new Vector4(0, 2, 0, 0)
            }, {
                118, new Vector4(0, 2, 3, 0)
            }, {
                100, new Vector4(0, 2, 2, 0)
            }, {
                38, new Vector4(0, 2, 1, 0)
            }, {
                17, new Vector4(0, 3, 0, 0)
            }, {
                89, new Vector4(0, 3, 3, 0)
            }, {
                113, new Vector4(0, 3, 2, 0)
            }, {
                57, new Vector4(0, 3, 1, 0)
            }, {
                33, new Vector4(0, 1, 0, 0)
            }, {
                9, new Vector4(0, 1, 1, 0)
            }, {
                65, new Vector4(0, 1, 2, 0)
            }, {
                105, new Vector4(0, 1, 3, 0)
            }, {
                56, new Vector4(3, 0, 0, 1)
            }, {
                34, new Vector4(3, 3, 0, 1)
            }, {
                8, new Vector4(3, 2, 0, 1)
            }, {
                18, new Vector4(3, 1, 0, 1)
            }, {
                104, new Vector4(1, 0, 2, 1)
            }, {
                66, new Vector4(1, 0, 3, 1)
            }, {
                88, new Vector4(1, 0, 0, 1)
            }, {
                114, new Vector4(1, 0, 1, 1)
            }, {
                20, new Vector4(0, 0, 0, 1)
            }, {
                86, new Vector4(0, 0, 1, 1)
            }, {
                68, new Vector4(0, 0, 2, 1)
            }, {
                6, new Vector4(0, 0, 3, 1)
            }, {
                36, new Vector4(0, 2, 0, 1)
            }, {
                54, new Vector4(0, 2, 3, 1)
            }, {
                116, new Vector4(0, 2, 2, 1)
            }, {
                102, new Vector4(0, 2, 1, 1)
            }, {
                49, new Vector4(0, 3, 0, 1)
            }, {
                25, new Vector4(0, 3, 3, 1)
            }, {
                81, new Vector4(0, 3, 2, 1)
            }, {
                121, new Vector4(0, 3, 1, 1)
            }, {
                1, new Vector4(0, 1, 0, 1)
            }, {
                73, new Vector4(0, 1, 1, 1)
            }, {
                97, new Vector4(0, 1, 2, 1)
            }, {
                41, new Vector4(0, 1, 3, 1)
            },
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
                    List<RawVoxelsData> rawVoxelsDatas = LoadVoxFile(filePath);
                    var assetName = Path.GetFileNameWithoutExtension(filePath);
                    if(clusterize) {
                        CreatePartialClusterizedGameObject(rawVoxelsDatas, assetName);
                    } else {
                        EditorUtility.DisplayProgressBar("Importing .vox file", "Importing", 0.5f);
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
            for(int partIndex = 0; partIndex < rawVoxelsDatas.Count; partIndex++) {
                var clusters = GenerateClusters(clusterVoxelsStep, clusterGenerationSeed, rawVoxelsDatas[partIndex]);
                var clustersAssetsData = new List<GeneratedAssetsData>();
                EditorUtility.DisplayProgressBar("Importing .vox file", "Importing", (float) partIndex / rawVoxelsDatas.Count);
                for(int i = 0; i < clusters.Length; i++) {
                    RecalculateClusterPivot(clusters[i]);
                    var assetData = GenerateAssets(assetName, clusters[i], partIndex, i);
                    clustersAssetsData.Add(assetData);
                }
                if(rawVoxelsDatas.Count > 1) {
                    if(parentObject == null) {
                        parentObject = new GameObject(assetName);
                    }
                    CreateClusterizedGameObject(clustersAssetsData, $"{assetName}_{partIndex}", parentObject.transform);
                } else {
                    CreateClusterizedGameObject(clustersAssetsData, assetName);
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
            cluster.Pivot = new Vector3Int(minX, minY, minZ);
        }

        private List<RawVoxelsData> LoadVoxFile(string filePath) {
            var stream = new BinaryReader(File.OpenRead(filePath));
            var parts = new List<RawVoxelsData>();

            int[] colors = null;
            List<RawVoxelData[]> rawDatas = new List<RawVoxelData[]>();
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
                        var rawData = new RawVoxelData[numVoxels];
                        for(int i = 0; i < rawData.Length; i++) {
                            rawData[i] = new RawVoxelData(stream);
                        }
                        rawDatas.Add(rawData);
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
                    case "nTRN":
                        //TODO: Read positions
                        ReadTransform(stream);
                        break;
                    default:
                        stream.ReadBytes(chunkSize);
                        break;
                }
            }

            stream.Close();

            foreach(RawVoxelData[] rawData in rawDatas) {
                for(int i = 0; i < rawData.Length; i++) {
                    int voxelColor = colors?[rawData[i].ColorCode - 1] ?? DefaultPalette[rawData[i].ColorCode - 1];
                    rawData[i].Color = voxelColor;
                }

                parts.Add(new RawVoxelsData(rawData));
            }

            return parts;
        }

        private static Vector3 ReadTransform(BinaryReader stream) {
            int id = stream.ReadInt32();
            var dic = ReadDictionary(stream);
            int childId = stream.ReadInt32();
            int reservedId = stream.ReadInt32();
            int layerId = stream.ReadInt32();
            int frameNum = stream.ReadInt32();
            var frameData = new TransformData.FrameData[frameNum];

            for(int i = 0; i < frameNum; i++) {
                var frameDic = ReadDictionary(stream);
                VoxMatrixByteToTransform(TryGetByte(frameDic, "_r", 4), out Vector3 rot, out Vector3 scale);
                frameData[i] = new TransformData.FrameData() {
                    Position = TryGetVector3(frameDic, "_t", Vector3.zero), 
                    Rotation = rot, 
                    Scale = scale,
                };
                frameData[i].Position = SwipYZ(frameData[i].Position);
            }

            return frameData[0].Position;
        }

        private static Vector3 TryGetVector3(Dictionary<string, string> dic, string key, Vector3 defaultValue) {
            if(dic.ContainsKey(key)) {
                string[] valueStr = dic[key].Split(' ');
                Vector3 vector = Vector3.zero;
                if(valueStr.Length == 3) {
                    for(int i = 0; i < 3; i++) {
                        if(int.TryParse(valueStr[i], out int value)) {
                            vector[i] = value;
                        } else {
                            return defaultValue;
                        }
                    }
                    return vector;
                }
            }
            return defaultValue;
        }

        private static Vector3 SwipYZ(Vector3 vector) {
            (vector.y, vector.z) = (vector.z, vector.y);
            return vector;
        }

        private static void VoxMatrixByteToTransform(byte byteKey, out Vector3 rotation, out Vector3 scale) {
            if(MagicTRansformMap.ContainsKey(byteKey)) {
                var vector = MagicTRansformMap[byteKey];
                rotation = new Vector3(vector.x * 90f, vector.y * 90f, vector.z * 90f);
                scale = vector.w < 0.5f ? Vector3.one : new Vector3(-1f, 1f, 1f);
            } else {
                rotation = Vector3.zero;
                scale = Vector3.one;
            }
        }

        private static byte TryGetByte(Dictionary<string, string> dic, string key, byte defaultValue) {
            if(dic.ContainsKey(key) && byte.TryParse(dic[key], out byte res)) {
                return res;
            }
            return defaultValue;
        }

        private static Dictionary<string, string> ReadDictionary(BinaryReader br) {
            var dic = new Dictionary<string, string>();
            int len = br.ReadInt32();
            for(int i = 0; i < len; i++) {
                string key = ReadString(br);
                string value = ReadString(br);
                dic.Add(key, value);
            }
            return dic;
        }

        private static string ReadString(BinaryReader stream) {
            int len = stream.ReadInt32();
            byte[] bytes = stream.ReadBytes(len);
            string str = string.Empty;
            for(int i = 0; i < bytes.Length; i++) {
                str += (char)bytes[i];
            }
            return str;
        }
        
        private GeneratedAssetsData GenerateAssets(string originalAssetName, RawVoxelsData rawData, int partIndex, int clusterIndex = -1) {
            if(rawData == null) {
                return null;
            }

            var size = rawData.Size;
            var data = new NativeArray3d<int>(size.x, size.z, size.y);
            for(int i = 0; i < rawData.Voxels.Count; i++) {
                data[rawData.Voxels[i].X, rawData.Voxels[i].Z, rawData.Voxels[i].Y] = rawData.Voxels[i].Color;
            }

            var generatedMesh = Utilities.GenerateMesh(data);
            MeshUtility.Optimize(generatedMesh);
            MeshUtility.SetMeshCompression(generatedMesh, ModelImporterMeshCompression.High);
            var bytes = NativeArray3dSerializer.Serialize(data, compress);

            var assetParentFolderName = originalAssetName;
            var assetName = clusterIndex >= 0 ? $"{originalAssetName}_{partIndex}_{clusterIndex}" : $"{originalAssetName}_{partIndex}";

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

        private List<GeneratedAssetsData> GenerateAssets(string originalAssetName, List<RawVoxelsData> rawDatas) {
            if(rawDatas == null || rawDatas.Count == 0) {
                return null;
            }

            var assetsDatas = new List<GeneratedAssetsData>();
            
            for(int partIndex = 0; partIndex < rawDatas.Count; partIndex++) {
                var rawData = rawDatas[partIndex];
                assetsDatas.Add(GenerateAssets(originalAssetName, rawData, partIndex));
            }

            return assetsDatas;
        }

        private void CreateClusterizedGameObject(List<GeneratedAssetsData> clustersAssetsData, string objectName, Transform parent = null) {
            if(clustersAssetsData == null || clustersAssetsData.Count == 0) {
                return;
            }
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent);
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
