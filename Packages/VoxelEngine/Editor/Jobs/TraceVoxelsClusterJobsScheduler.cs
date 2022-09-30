using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Editor.Jobs
{
    public class TraceVoxelsClusterJobsScheduler
    {
        public void Run(RawVoxelsData cluster, NativeArray3d<int> voxels, int x, int y, int z, int maxVoxels) {
            
            var result = new NativeList<int4>(1, Allocator.TempJob);
            var taskQueue = new NativeQueue<int3>(Allocator.TempJob);
            
            var job = new TraceVoxelsClusterJob() {
                Voxels = voxels,
                Result = result,
                Queue = taskQueue,
                ClusterStart = new int3(x, y ,z),
                MaxVoxels = maxVoxels
            };

            job.Schedule().Complete();

            for(int i = 0; i < job.Result.Length; i++) {
                cluster.Voxels.Add(new RawVoxelData(job.Result[i].x, job.Result[i].y, job.Result[i].z, job.Result[i].w));
            }
            
            result.Dispose();
            taskQueue.Dispose();
        }
    }
}
