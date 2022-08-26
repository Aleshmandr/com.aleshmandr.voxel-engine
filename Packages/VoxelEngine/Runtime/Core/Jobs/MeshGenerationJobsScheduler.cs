using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    public class MeshGenerationJobsScheduler
    {
        private JobHandle lastJobHandle;

        public async Task<Mesh> Run(NativeArray3d<int> voxels, Mesh mesh = null) {

            // Allocate mesh data for one mesh.
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];
            var voxelsDataCopy = voxels.AllocateNativeDataCopy(Allocator.TempJob);

            var meshGenerationJob = new VoxelMeshGenerationJob {
                SizeX = voxels.SizeX,
                SizeY = voxels.SizeY,
                SizeZ = voxels.SizeZ,
                Voxels = voxelsDataCopy,
                MeshData = meshData
            };

            var jobHandle = lastJobHandle.IsCompleted ? meshGenerationJob.Schedule() : meshGenerationJob.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;

            while(!jobHandle.IsCompleted) {
                await Task.Yield();
            }

            jobHandle.Complete();

            if(mesh == null) {
                mesh = new Mesh();
                mesh.MarkDynamic();
            } else {
                mesh.Clear();
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
