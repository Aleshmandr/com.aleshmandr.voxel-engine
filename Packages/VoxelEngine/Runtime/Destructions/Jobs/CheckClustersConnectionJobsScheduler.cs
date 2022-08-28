using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.Destructions.Jobs
{
    public class CheckClustersConnectionJobsScheduler
    {
        private JobHandle lastJobHandle;

        public async Task<bool> Run(DestructableVoxels clusterA, DestructableVoxels clusterB, bool waitPrevious) {

            var result = new NativeArray<bool>(1, Allocator.TempJob);

            var clusterADataCopy = clusterA.VoxelsContainer.Data.Copy(Allocator.TempJob);
            var clusterBDataCopy = clusterB.VoxelsContainer.Data.Copy(Allocator.TempJob);

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
                await Task.Yield();
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
