using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    public class VoxelsFractureJobsScheduler
    {
        private JobHandle lastJobHandle;

        public async UniTask<FractureData> Run(NativeArray3d<int> voxels, int radius, int minSize, int maxSize, Vector3Int localPoint, Allocator allocator) {
            
            var result = new FractureData(allocator);
            var intergrityCheckQueue = new NativeQueue<int3>(Allocator.Persistent);
            var intergrityCheck = new NativeArray<bool>(voxels.NativeArray.Length, Allocator.Persistent);
            var damageJob = new FractureVoxelsJob {
                Radius = radius,
                MinSize = minSize,
                MaxSize = maxSize,
                CollapseHangingVoxels = true,
                LocalPoint = localPoint,
                Voxels = voxels,
                IntergrityCheck = intergrityCheck,
                IntegrityQueue = intergrityCheckQueue,
                ResultClusters = result.ClustersLengths,
                ResultVoxels = result.Voxels
            };
            
            var jobHandle = damageJob.Schedule(lastJobHandle);
            lastJobHandle = jobHandle;
            
            while(!lastJobHandle.IsCompleted) {
                await UniTask.Yield();
            }
            
            lastJobHandle.Complete();
            intergrityCheckQueue.Dispose();
            intergrityCheck.Dispose();
            
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
        public static readonly FractureData Empty = new FractureData();
        
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
