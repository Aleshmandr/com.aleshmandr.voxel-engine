using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
namespace VoxelEngine.Jobs
{
    [BurstCompile]
    public struct BakeMeshJob : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> MeshIds;
        public bool Convex;

        public void Execute(int index) {
            Physics.BakeMesh(MeshIds[index], Convex);
        }
    }
}
