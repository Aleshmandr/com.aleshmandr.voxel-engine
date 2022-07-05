using System.IO;

namespace VoxelEngine.Editor
{
    [System.Serializable]
    public struct RawVoxelData
    {
        public byte X;
        public byte Y;
        public byte Z;
        public byte ColorCode;
        public int Color;

        public RawVoxelData(BinaryReader stream) {
            X = stream.ReadByte();
            Y = stream.ReadByte();
            Z = stream.ReadByte();
            ColorCode = stream.ReadByte();
            Color = 0;
        }
    }
}
