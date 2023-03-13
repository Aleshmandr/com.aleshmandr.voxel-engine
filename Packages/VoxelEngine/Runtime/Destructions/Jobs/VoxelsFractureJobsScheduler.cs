using Cysharp.Threading.Tasks;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsFractureJobsScheduler
    {
        public async UniTask<FractureData> Run(NativeArray3d<int> voxels, int radius, int minSize, int maxSize, Vector3Int localPoint, Allocator allocator) {
            var result = new FractureData(allocator);
            var damageJob = new FractureVoxelsJob {
                Radius = radius,
                MinSize = minSize,
                MaxSize = maxSize,
                LocalPoint = localPoint,
                Voxels = voxels,
                ResultClusters = result.ClustersLengths,
                ResultVoxels = result.Voxels
            };

            //TODO: Jobs scheduling
            damageJob.Schedule().Complete();

            return result;
        }
    }

    public struct FractureVoxelData
    {
        public int3 Position;
        public int Color;
    }
    
    public struct FractureData : IDisposable
    {
        public NativeList<int> ClustersLengths;
        public NativeList<FractureVoxelData> Voxels;

        public FractureData(Allocator allocator) {
            ClustersLengths = new NativeList<int>(allocator);
            Voxels = new NativeList<FractureVoxelData>(allocator);
        }
        
        public void Dispose() {
            ClustersLengths.Dispose();
            Voxels.Dispose();
        }
    }
}
