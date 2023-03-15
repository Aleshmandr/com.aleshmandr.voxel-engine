using Cysharp.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    public class OptimizedMeshGenerationJobsScheduler : IMeshGenerationJobScheduler
    {
        private JobHandle lastJobHandle;

        public async UniTask<Mesh> Run(NativeArray3d<int> voxels, CancellationToken cancellationToken, Mesh mesh = null) {

            // Allocate mesh data for one mesh.
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];
            var voxelsDataCopy = voxels.AllocateNativeDataCopy(Allocator.Persistent);

            var meshGenerationJob = new VoxelOptimizedMeshGenerationJob() {
                SizeX = voxels.SizeX,
                SizeY = voxels.SizeY,
                SizeZ = voxels.SizeZ,
                Voxels = voxelsDataCopy,
                MeshData = meshData
            };

            var jobHandle = lastJobHandle.IsCompleted ? meshGenerationJob.Schedule() : meshGenerationJob.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;

            while(!jobHandle.IsCompleted) {
                await UniTask.Yield();
            }

            jobHandle.Complete();
            voxelsDataCopy.Dispose();
            
            if(mesh == null) {
                mesh = new Mesh();
                mesh.MarkDynamic();
            } else {
                mesh.Clear();
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
