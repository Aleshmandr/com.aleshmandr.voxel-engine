using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsFractureJobsScheduler
    {
        public async UniTask<NativeList<int>> Run(NativeArray3d<int> voxels, int radius, int minSize, int maxSize, Vector3Int localPoint, Allocator allocator) {
            var damagedVoxels = new NativeList<int>(allocator);
            var damageJob = new FractureVoxelsJob {
                Radius = radius,
                MinSize = minSize,
                MaxSize = maxSize,
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
