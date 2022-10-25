using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.Destructions.Jobs
{
    public class CheckClustersConnectionJobsScheduler
    {
        private JobHandle lastJobHandle;

        public async UniTask<bool> Run(DestructableVoxels clusterA, DestructableVoxels clusterB, bool waitPrevious) {

            var result = new NativeArray<bool>(1, Allocator.Persistent);

            var clusterADataCopy = clusterA.VoxelsContainer.Data.Copy(Allocator.Persistent);
            var clusterBDataCopy = clusterB.VoxelsContainer.Data.Copy(Allocator.Persistent);

            var job = new CheckClustersConnectionJob {
                ChunkOneData = clusterADataCopy,
                ChunkTwoData = clusterBDataCopy,
                ChunkOnePos = clusterA.transform.localPosition,
                ChunkTwoPos = clusterB.transform.localPosition,
                Result = result
            };
            
            var jobHandle = lastJobHandle.IsCompleted || !waitPrevious ? job.Schedule() : job.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;

            while(!jobHandle.IsCompleted) {
                await UniTask.Yield();
            }

            jobHandle.Complete();

            var areNeighbours = result[0];

            result.Dispose();
            clusterADataCopy.Dispose();
            clusterBDataCopy.Dispose();

            return areNeighbours;
        }
    }
}
