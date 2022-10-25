using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsIntegrityJobsScheduler
    {
        public async UniTask<bool> Run(NativeArray3d<int> voxels, int integralCount) {
            var voxelsDataCopy = voxels.Copy(Allocator.Persistent);
            var result = new NativeArray<int>(1, Allocator.Persistent);
            var taskQueue = new NativeQueue<int3>(Allocator.Persistent);
            
            var job = new CheckVoxelsChunksIntegrityJob {
                Voxels = voxelsDataCopy,
                Result = result,
                Queue = taskQueue
            };

            var jobHandle = job.Schedule();
            
            while(!jobHandle.IsCompleted) {
                await UniTask.Yield();
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
