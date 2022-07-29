using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsDamageJobsScheduler
    {
        private JobHandle lastJobHandle;

        public async Task<NativeList<VoxelData>> Run(NativeArray3d<int> voxels, int radius, Vector3Int localPoint, Allocator allocator) {
            var damagedVoxels = new NativeList<VoxelData>(allocator);
            var damageJob = new DamageVoxelsJob {
                Radius = radius,
                LocalPoint = localPoint,
                Voxels = voxels,
                Result = damagedVoxels
            };

            var jobHandle = lastJobHandle.IsCompleted ? damageJob.Schedule() : damageJob.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;

            while(!jobHandle.IsCompleted) {
                await Task.Delay(1);
            }

            jobHandle.Complete();

            return damagedVoxels;
        }
    }
}
