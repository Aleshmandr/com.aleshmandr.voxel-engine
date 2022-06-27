using System.IO;

namespace VoxelEngine
{
    [System.Serializable]
    public struct VoxelData
    {
        public byte x;
        public byte y;
        public byte z;
        public byte color;

        public VoxelData(BinaryReader stream) {
            x = stream.ReadByte();
            y = stream.ReadByte();
            z = stream.ReadByte();
            color = stream.ReadByte();
        }
    }
}
