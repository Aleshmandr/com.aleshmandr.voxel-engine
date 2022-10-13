using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsDamageJobsScheduler
    {
        public async UniTask<NativeList<VoxelData>> Run(NativeArray3d<int> voxels, int radius, Vector3Int localPoint, Allocator allocator) {
            var damagedVoxels = new NativeList<VoxelData>(allocator);
            var damageJob = new DamageVoxelsJob {
                Radius = radius,
                LocalPoint = localPoint,
                Voxels = voxels,
                Result = damagedVoxels
            };

            //TODO: Jobs scheduling
            damageJob.Schedule().Complete();

            return damagedVoxels;
        }
    }
}
