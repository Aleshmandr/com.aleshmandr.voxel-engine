using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public class VoxImporter : EditorWindow
    {
        private static ushort[] DefaultPalette = {
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

        [MenuItem("Tools/VoxelEngine/Magica Voxel Importer (.vox)", false)]
        public static void ShowWindow() {
            EditorWindow.GetWindow(typeof(VoxImporter));
        }

        private void OnGUI() {
            titleContent.text = "Magica Voxel Importer";
            EditorGUILayout.BeginVertical("Box");

            compress = EditorGUILayout.Toggle("Compress", compress);
            EditorGUILayout.LabelField("Import .vox file");
            if(GUILayout.Button("Import")) {
                string filePath = EditorUtility.OpenFilePanel("Import file", "", "vox");
                if(string.IsNullOrEmpty(filePath)) {
                    return;
                }
                LoadModel(filePath);
            }
            EditorGUILayout.EndVertical();
        }

        private void LoadModel(string filePath) {
            var stream = new BinaryReader(File.OpenRead(filePath));

            int[] colors = null;
            VoxelData[] voxelData = null;
            string magic = new string(stream.ReadChars(4));
            int version = stream.ReadInt32();

            if(magic != "VOX ") {
                return;
            }

            int maxX = 0;
            int maxY = 0;
            int maxZ = 0;

            while(stream.BaseStream.Position < stream.BaseStream.Length) {
                char[] chunkId = stream.ReadChars(4);
                int chunkSize = stream.ReadInt32();
                int childChunks = stream.ReadInt32();
                string chunkName = new string(chunkId);

                switch(chunkName) {
                    case "SIZE":
                        maxX = stream.ReadInt32();
                        maxY = stream.ReadInt32();
                        maxZ = stream.ReadInt32();
                        stream.ReadBytes(chunkSize - 4 * 3);
                        break;
                    case "XYZI":
                        int numVoxels = stream.ReadInt32();
                        voxelData = new VoxelData[numVoxels];
                        for(int i = 0; i < voxelData.Length; i++) {
                            voxelData[i] = new VoxelData(stream);
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

            if(voxelData == null || voxelData.Length == 0) {
                return;
            }

            var data = new VoxelsData(maxX, maxZ, maxY);
            for(int i = 0; i < voxelData.Length; i++) {
                int voxelColor = colors?[voxelData[i].color - 1] ?? DefaultPalette[voxelData[i].color - 1];
                data.Blocks[voxelData[i].x, voxelData[i].z, voxelData[i].y] = voxelColor;
            }

            var generatedMesh = Utilities.GenerateMesh(data);

            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var bytes = Utilities.SerializeObject(data, compress);

            File.WriteAllBytes(Application.dataPath + $"/{fileName}.bytes", bytes);

            AssetDatabase.CreateAsset(generatedMesh, $"Assets/{fileName}.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
