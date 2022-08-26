using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    public struct CheckVoxelsChunksIntegrityJob : IJob
    {
        public NativeArray3d<int> Voxels;
        public NativeArray<int> Result;

        public void Execute() {
            
            for(int i = 0; i < Voxels.SizeX; i++) {
                for(int j = 0; j < Voxels.SizeY; j++) {
                    for(int k = 0; k < Voxels.SizeZ; k++) {
                        if(Voxels[i, j, k] != 0) {
                            int sum = 0;
                            TraceRecurcively(i, j, k, ref sum);
                            Result[0] = sum;
                            return;
                        }
                    }
                }
            }
        }
        
        private void TraceRecurcively(int i, int j, int k, ref int sum) {
            if(Voxels.IsCoordsValid(i, j, k) && Voxels[i, j, k] != 0) {
                sum++;
                Voxels[i, j, k] = 0;

                int left = i - 1;
                int right = i + 1;
                int up = j + 1;
                int down = j - 1;
                int forward = k + 1;
                int back = k - 1;

                TraceRecurcively(left, j, k, ref sum);
                TraceRecurcively(left, up, k, ref sum);
                TraceRecurcively(left, down, k, ref sum);
                TraceRecurcively(right, j, k, ref sum);
                TraceRecurcively(right, up, k, ref sum);
                TraceRecurcively(right, down, k, ref sum);
                TraceRecurcively(i, up, k, ref sum);
                TraceRecurcively(i, up, forward, ref sum);
                TraceRecurcively(i, up, back, ref sum);
                TraceRecurcively(i, down, k, ref sum);
                TraceRecurcively(i, down, forward, ref sum);
                TraceRecurcively(i, down, back, ref sum);
                TraceRecurcively(i, j, forward, ref sum);
                TraceRecurcively(left, j, forward, ref sum);
                TraceRecurcively(right, j, forward, ref sum);
                TraceRecurcively(i, j, back, ref sum);
                TraceRecurcively(left, j, back, ref sum);
                TraceRecurcively(right, j, back, ref sum);
                TraceRecurcively(left, up, forward, ref sum);
                TraceRecurcively(left, down, forward, ref sum);
                TraceRecurcively(left, down, back, ref sum);
                TraceRecurcively(left, up, back, ref sum);
                TraceRecurcively(right, up, forward, ref sum);
                TraceRecurcively(right, down, forward, ref sum);
                TraceRecurcively(right, down, back, ref sum);
                TraceRecurcively(right, up, back, ref sum);
            }
        }
    }
}
