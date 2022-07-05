namespace VoxelEngine
{
    [System.Serializable]
    public class VoxelsData
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        public readonly int[,,] Blocks;

        public VoxelsData(int sizeX, int sizeY, int sizeZ) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            Blocks = new int[SizeX, SizeY, SizeZ];
        }
    }
}
