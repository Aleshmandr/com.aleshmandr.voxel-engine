namespace VoxelEngine.Destructions
{
    public interface IDestructibleVoxels
    {
        public bool IsInitialized { get; }
        public int InitialVoxelsCount { get; }
        public int VoxelsCount { get; }
        public void Recover();
    }
}
