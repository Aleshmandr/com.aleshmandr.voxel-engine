namespace VoxelEngine.Destructions
{
    public class DamageEventData<T> where T : IDamageData
    {
        public DestructableVoxels Voxels { get; }
        public IDamageData DamageData { get; }

        public DamageEventData(DestructableVoxels voxels, T damageData) {
            Voxels = voxels;
            DamageData = damageData;
        }
    }
}
