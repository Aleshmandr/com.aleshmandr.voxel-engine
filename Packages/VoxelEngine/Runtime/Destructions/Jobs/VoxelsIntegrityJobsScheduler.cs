using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsIntegrityJobsScheduler
    {
        public async Task<bool> Run(NativeArray3d<int> voxels, int integralCount) {
            var voxelsDataCopy = voxels.Copy(Allocator.TempJob);
            var result = new NativeArray<int>(1, Allocator.TempJob);
            
            var job = new CheckVoxelsChunksIntegrityJob {
                Voxels = voxelsDataCopy,
                Result = result
            };

            var jobHandle = job.Schedule();
            
            while(!jobHandle.IsCompleted) {
                await Task.Yield();
            }
            
            jobHandle.Complete();
            int count = result[0];
            
            voxelsDataCopy.Dispose();
            result.Dispose();

            return count >= integralCount;
        }
    }
}
