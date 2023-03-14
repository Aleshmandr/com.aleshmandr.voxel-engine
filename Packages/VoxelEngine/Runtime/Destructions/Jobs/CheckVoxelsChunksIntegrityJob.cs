using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile]
    public struct CheckVoxelsChunksIntegrityJob : IJob
    {
        public NativeArray3d<int> Voxels;
        public NativeArray<int> Result;
        public NativeQueue<int3> Queue;

        public void Execute() {
            for(int i = 0; i < Voxels.SizeX; i++) {
                for(int j = 0; j < Voxels.SizeY; j++) {
                    for(int k = 0; k < Voxels.SizeZ; k++) {
                        if(Voxels[i, j, k] != 0) {
                            int sum = 0;
                            StartTrace(i, j, k, ref sum);
                            Result[0] = sum;
                            return;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private void StartTrace(int i, int j, int k, ref int sum) {
            TryEnqueue(i, j, k);
            TraceWithStack(ref sum);
        }

        [BurstCompile]
        private void TraceWithStack(ref int sum) {
            while(!Queue.IsEmpty()) {
                var voxel = Queue.Dequeue();
                int i = voxel.x;
                int j = voxel.y;
                int k = voxel.z;

                if(Voxels[i, j, k] == 0) {
                    continue;
                }

                Voxels[i, j, k] = 0;
                sum++;

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

        [BurstCompile]
        private void TraceRecursively(int i, int j, int k, ref int sum) {
            if(Voxels.IsCoordsValid(i, j, k) && Voxels[i, j, k] != 0) {
                sum++;
                Voxels[i, j, k] = 0;

                int left = i - 1;
                int right = i + 1;
                int up = j + 1;
                int down = j - 1;
                int forward = k + 1;
                int back = k - 1;

                TraceRecursively(left, j, k, ref sum);
                TraceRecursively(left, up, k, ref sum);
                TraceRecursively(left, down, k, ref sum);
                TraceRecursively(right, j, k, ref sum);
                TraceRecursively(right, up, k, ref sum);
                TraceRecursively(right, down, k, ref sum);
                TraceRecursively(i, up, k, ref sum);
                TraceRecursively(i, up, forward, ref sum);
                TraceRecursively(i, up, back, ref sum);
                TraceRecursively(i, down, k, ref sum);
                TraceRecursively(i, down, forward, ref sum);
                TraceRecursively(i, down, back, ref sum);
                TraceRecursively(i, j, forward, ref sum);
                TraceRecursively(left, j, forward, ref sum);
                TraceRecursively(right, j, forward, ref sum);
                TraceRecursively(i, j, back, ref sum);
                TraceRecursively(left, j, back, ref sum);
                TraceRecursively(right, j, back, ref sum);
                TraceRecursively(left, up, forward, ref sum);
                TraceRecursively(left, down, forward, ref sum);
                TraceRecursively(left, down, back, ref sum);
                TraceRecursively(left, up, back, ref sum);
                TraceRecursively(right, up, forward, ref sum);
                TraceRecursively(right, down, forward, ref sum);
                TraceRecursively(right, down, back, ref sum);
                TraceRecursively(right, up, back, ref sum);
            }
        }
    }
}
