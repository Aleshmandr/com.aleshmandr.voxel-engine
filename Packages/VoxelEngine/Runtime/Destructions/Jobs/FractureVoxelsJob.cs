using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile]
    public struct FractureVoxelsJob : IJob
    {
        public int Radius;
        public int MinSize;
        public int MaxSize;
        public Vector3Int LocalPoint;
        public NativeArray3d<int> Voxels;
        public NativeList<int> Result; //First number is cluster length, then cluster voxels, then next cluster length and so on...

        public void Execute() {
            var random = new Random((uint)(Radius + LocalPoint.x + LocalPoint.y + LocalPoint.z));
            int r2 = Radius * Radius;
            for(int i = -Radius; i <= Radius; i++) {
                int i2 = i * i;
                for(int j = -Radius; j <= Radius; j++) {
                    int i2j2 = i2 + j * j;
                    for(int k = -Radius; k <= Radius; k++) {
                        if(i2j2 + k * k <= r2) {
                            int x = i + LocalPoint.x;
                            int y = j + LocalPoint.y;
                            int z = k + LocalPoint.z;
                            if(Voxels.IsCoordsValid(x, y, z) && Voxels[x, y, z] != 0) {
                                int clusterSize = random.NextInt(MinSize, MaxSize);
                                int3 neighbourVoxel = new int3(x, y, z);
                                int clusterLengthIndex = Result.Length;
                                //Reserve place for cluster length
                                Result.Add(0);
                                int clisterSize = 0;
                                MoveVoxelToCluster(neighbourVoxel);
                                clisterSize++;

                                for(int c = 0; c < clusterSize; c++) {

                                    neighbourVoxel = GetRandomNeighbourIndex(neighbourVoxel, random.NextInt(0, 6));
                                    if(neighbourVoxel.x < 0) {
                                        
                                        continue;
                                    }
                                    MoveVoxelToCluster(neighbourVoxel);
                                    clisterSize++;
                                }

                                Result[clusterLengthIndex] = clisterSize;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile] [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveVoxelToCluster(int3 voxelPos) {
            int voxelIndex = Voxels.CoordToIndex(voxelPos.x, voxelPos.y, voxelPos.z);
            Result.Add(voxelIndex);
            Voxels.NativeArray[voxelIndex] = 0;
        }

        private int3 GetRandomNeighbourIndex(int3 voxelPos, int direction) {
            for(int i = 0; i < 6; i++) {
                int3 dir = new int3(0, 0, 0) {
                    [direction % 3] = 1
                };

                if(direction > 2) {
                    dir = -dir;
                }

                int3 neighbourPos = voxelPos + dir;

                if(Voxels.IsCoordsValid(neighbourPos.x, neighbourPos.y, neighbourPos.z) && Voxels[neighbourPos.x, neighbourPos.y, neighbourPos.z] != 0) {
                    return neighbourPos;
                }

                direction++;
                if(direction > 5) {
                    direction = 0;
                }
            }

            return -1;
        }
    }
}
