using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsIntegrityJobsScheduler
    {
        public async Task<bool> Run(NativeArray3d<int> voxels, int integralCount) {
            var voxelsDataCopy = voxels.Copy(Allocator.TempJob);
            var result = new NativeArray<int>(1, Allocator.TempJob);
            var taskQueue = new NativeQueue<int3>(Allocator.TempJob);
            
            var job = new CheckVoxelsChunksIntegrityJob {
                Voxels = voxelsDataCopy,
                Result = result,
                Queue = taskQueue
            };

            var jobHandle = job.Schedule();
            
            while(!jobHandle.IsCompleted) {
                await Task.Yield();
            }
            
            jobHandle.Complete();
            int count = result[0];
            
            voxelsDataCopy.Dispose();
            result.Dispose();
            taskQueue.Dispose();
            
            return count >= integralCount;
        }
    }
}
