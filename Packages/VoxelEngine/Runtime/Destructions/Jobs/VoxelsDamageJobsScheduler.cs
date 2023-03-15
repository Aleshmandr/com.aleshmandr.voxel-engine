using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsDamageJobsScheduler
    {
        private JobHandle lastJobHandle;
        
        public async UniTask<NativeList<VoxelData>> Run(NativeArray3d<int> voxels, int radius, Vector3Int localPoint, Allocator allocator) {
            var damagedVoxels = new NativeList<VoxelData>(allocator);
            var damageJob = new DamageVoxelsJob {
                Radius = radius,
                LocalPoint = localPoint,
                Voxels = voxels,
                Result = damagedVoxels
            };

            var jobHandle = damageJob.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;

            while(!lastJobHandle.IsCompleted) {
                await UniTask.Yield();
            }
            
            lastJobHandle.Complete();

            return damagedVoxels;
        }
    }
}
