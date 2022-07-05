using System.IO;

namespace VoxelEngine
{
    [System.Serializable]
    public struct VoxelData
    {
        public byte X;
        public byte Y;
        public byte Z;
        public byte Color;

        public VoxelData(BinaryReader stream) {
            X = stream.ReadByte();
            Y = stream.ReadByte();
            Z = stream.ReadByte();
            Color = stream.ReadByte();
        }
    }
}
