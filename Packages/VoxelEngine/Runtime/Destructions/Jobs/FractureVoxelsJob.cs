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
        public bool CollapseHangingVoxels;
        public Vector3Int LocalPoint;
        public NativeArray3d<int> Voxels;
        public NativeList<int> ResultClusters;
        public NativeList<FractureVoxelData> ResultVoxels;
        
        public NativeQueue<int3> IntegrityQueue;
        
        public NativeArray<bool> IntergrityCheck;
        
        private int currentDirection;
        private int collapseClusterStartIndex;
        private int collapseClusterSize;
        
        public void Execute() {
            Break();
            if(CollapseHangingVoxels) {
                Collapse();
            }
        }

        [BurstCompile]
        private void Break() {
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
                                
                                int targetClusterSize = random.NextInt(MinSize, MaxSize);
                                int3 neighbourVoxel = new int3(x, y, z);
                                int3 sucNeeghbour = neighbourVoxel;
                                MoveVoxelToCluster(neighbourVoxel);
                                int clusterSize = 1;

                                currentDirection = random.NextInt(0, 6);

                                int triesNum = 0;
                                int skipNumThresh = 0;
                                while(clusterSize < targetClusterSize) {
                                    skipNumThresh++;
                                    if(skipNumThresh > 12) {
                                        break;
                                    }
                                    
                                    if(triesNum > 5) {
                                        triesNum = 0;
                                        neighbourVoxel = sucNeeghbour;
                                    }
                                    var tempNeighbourVoxel = GetRandomNeighbourIndex(neighbourVoxel);
                                    if(tempNeighbourVoxel.x < 0) {
                                        currentDirection++;
                                        triesNum++;
                                        continue;
                                    }
                                    skipNumThresh = 0;
                                    sucNeeghbour = tempNeighbourVoxel;
                                    MoveVoxelToCluster(sucNeeghbour);
                                    clusterSize++;
                                }

                                ResultClusters.Add(clusterSize);
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile] [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveVoxelToCluster(int3 voxelPos) {
            int voxelIndex = Voxels.CoordToIndex(voxelPos.x, voxelPos.y, voxelPos.z);
            ResultVoxels.Add(new FractureVoxelData {
                Position = voxelPos,
                Color = Voxels.NativeArray[voxelIndex]
            });
            Voxels.NativeArray[voxelIndex] = 0;
        }

        [BurstCompile]
        private int3 GetRandomNeighbourIndex(int3 voxelPos) {
            for(int i = 0; i < 6; i++) {
                if(currentDirection > 5) {
                    currentDirection = 0;
                }
                
                int3 dir = new int3(0, 0, 0) {
                    [currentDirection % 3] = 1
                };

                if(currentDirection > 2) {
                    dir = -dir;
                }

                int3 neighbourPos = voxelPos + dir;

                if(Voxels.IsCoordsValid(neighbourPos.x, neighbourPos.y, neighbourPos.z) && Voxels[neighbourPos.x, neighbourPos.y, neighbourPos.z] != 0) {
                    return neighbourPos;
                }

                currentDirection++;
            }

            return -1;
        }

        private void Collapse() {
            for(int i = 0; i < Voxels.NativeArray.Length; i++) {
                IntergrityCheck[i] = Voxels.NativeArray[i] != 0;
            }

            for(int i = 0; i < Voxels.SizeX; i++) {
                for(int j = 0; j < Voxels.SizeY; j++) {
                    for(int k = 0; k < Voxels.SizeZ; k++) {
                        if(IntergrityCheck[Voxels.CoordToIndex(i, j, k)]) {
                            bool isFixed = false;
                            collapseClusterSize = 0;
                            collapseClusterStartIndex = ResultVoxels.Length;
                            StartTrace(i, j, k, ref isFixed);
                            if(isFixed) {
                                if(collapseClusterSize > 0) {
                                    ResultVoxels.RemoveRangeSwapBack(collapseClusterStartIndex, collapseClusterSize);
                                }
                            } else {
                                if(collapseClusterSize > 0) {
                                    for(int l = 0; l < collapseClusterSize; l++) {
                                        int3 voxPos = ResultVoxels[collapseClusterStartIndex + l].Position;
                                        Voxels[voxPos.x, voxPos.y, voxPos.z] = 0;
                                    }
                                    ResultClusters.Add(collapseClusterSize);
                                }
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private void StartTrace(int i, int j, int k, ref bool isFixed) {
            TryEnqueue(i, j, k);
            TraceWithStack(ref isFixed);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryEnqueue(int i, int j, int k) {
            if(Voxels.IsCoordsValid(i, j, k) && Voxels[i, j, k] != 0) {
                IntegrityQueue.Enqueue(new int3(i, j, k));
            }
        }
        
        [BurstCompile]
        private void TraceWithStack(ref bool isFixed) {
            while(!IntegrityQueue.IsEmpty()) {
                var voxel = IntegrityQueue.Dequeue();
                int i = voxel.x;
                int j = voxel.y;
                int k = voxel.z;
                int index = Voxels.CoordToIndex(i, j, k);

                if(!IntergrityCheck[index]) {
                    continue;
                }

                IntergrityCheck[index] = false;
                
                isFixed |= j == 0;
                if(!isFixed) {
                    ResultVoxels.Add(new FractureVoxelData {
                        Position = voxel,
                        Color = Voxels.NativeArray[index]
                    });
                    collapseClusterSize++;
                }

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
    }
}
