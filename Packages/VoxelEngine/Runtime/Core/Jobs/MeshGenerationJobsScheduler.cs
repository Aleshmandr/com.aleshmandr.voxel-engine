using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    public class MeshGenerationJobsScheduler : IMeshGenerationJobScheduler
    {
        private JobHandle lastJobHandle;

        public async UniTask<Mesh> Run(NativeArray3d<int> voxels, CancellationToken cancellationToken, Mesh mesh = null) {

            // Allocate mesh data for one mesh.
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];
            var voxelsDataCopy = voxels.AllocateNativeDataCopy(Allocator.Persistent);

            var meshGenerationJob = new VoxelMeshGenerationJob {
                SizeX = voxels.SizeX,
                SizeY = voxels.SizeY,
                SizeZ = voxels.SizeZ,
                Voxels = voxelsDataCopy,
                MeshData = meshData
            };

            var jobHandle = lastJobHandle.IsCompleted ? meshGenerationJob.Schedule() : meshGenerationJob.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;
            
            
            while(true) {
                if(jobHandle.IsCompleted || cancellationToken.IsCancellationRequested) {
                    break;
                }
                await UniTask.Yield();
            }

            if(cancellationToken.IsCancellationRequested) {
                jobHandle.Complete();
                voxelsDataCopy.Dispose();
                meshDataArray.Dispose();
                return null;
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
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
