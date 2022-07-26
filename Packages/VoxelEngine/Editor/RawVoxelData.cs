using System.IO;

namespace VoxelEngine.Editor
{
    [System.Serializable]
    public class RawVoxelData
    {
        public int X;
        public int Y;
        public int Z;
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
