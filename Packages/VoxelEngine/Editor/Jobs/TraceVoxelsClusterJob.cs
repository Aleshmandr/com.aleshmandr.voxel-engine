using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Editor.Jobs
{
    [BurstCompile]
    public struct TraceVoxelsClusterJob : IJob
    {
        public int3 ClusterStart;
        public int MaxVoxels;
        public NativeArray3d<int> Voxels;
        public NativeList<int4> Result;
        public NativeQueue<int3> Queue;

        public void Execute() {
            for(int i = 0; i < Voxels.SizeX; i++) {
                for(int j = 0; j < Voxels.SizeY; j++) {
                    for(int k = 0; k < Voxels.SizeZ; k++) {
                        if(Voxels[i, j, k] != 0) {
                            StartTrace(i, j, k);
                            return;
                        }
                    }
                }
            }
        }

        private void StartTrace(int i, int j, int k) {
            TryEnqueue(i, j, k);
            TraceWithStack();
        }

        private void TraceWithStack() {
            while(!Queue.IsEmpty()) {
                var voxel = Queue.Dequeue();
                int i = voxel.x;
                int j = voxel.y;
                int k = voxel.z;

                if(Voxels[i, j, k] == 0) {
                    continue;
                }

                if(Result.Length > MaxVoxels) {
                    break;
                }
                
                Result.Add(new int4(i, j, k, Voxels[i, j, k]));
                Voxels[i, j, k] = 0;

                int left = i - 1;
                int right = i + 1;
                int up = j + 1;
                int down = j - 1;
                int forward = k + 1;
                int back = k - 1;

                TryEnqueue(left, j, k);
                TryEnqueue(left, up, k);
                TryEnqueue(left, down, k);
                TryEnqueue(right, j, k);
                TryEnqueue(right, up, k);
                TryEnqueue(right, down, k);
                TryEnqueue(i, up, k);
                TryEnqueue(i, up, forward);
                TryEnqueue(i, up, back);
                TryEnqueue(i, down, k);
                TryEnqueue(i, down, forward);
                TryEnqueue(i, down, back);
                TryEnqueue(i, j, forward);
                TryEnqueue(left, j, forward);
                TryEnqueue(right, j, forward);
                TryEnqueue(i, j, back);
                TryEnqueue(left, j, back);
                TryEnqueue(right, j, back);
                TryEnqueue(left, up, forward);
                TryEnqueue(left, down, forward);
                TryEnqueue(left, down, back);
                TryEnqueue(left, up, back);
                TryEnqueue(right, up, forward);
                TryEnqueue(right, down, forward);
                TryEnqueue(right, down, back);
                TryEnqueue(right, up, back);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryEnqueue(int i, int j, int k) {
            if(Voxels.IsCoordsValid(i, j, k) && Voxels[i, j, k] != 0) {
                Queue.Enqueue(new int3(i, j, k));
            }
        }
    }
}
