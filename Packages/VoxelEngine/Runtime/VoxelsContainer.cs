namespace VoxelEngine
{
    public class VoxelsContainer
    {
        public readonly int fromX;
        public readonly int fromZ;
        public readonly int fromY;
        public readonly int toX;
        public readonly int toZ;
        public readonly int toY;

        public readonly int[,,] Blocks;

        public VoxelsContainer(int x, int y, int z) {
            Blocks = new int[x + 1, y + 1, z + 1];
            fromX = 0;
            fromY = 0;
            fromZ = 0;
            toX = x;
            toY = y;
            toZ = z;
        }
    }
}
