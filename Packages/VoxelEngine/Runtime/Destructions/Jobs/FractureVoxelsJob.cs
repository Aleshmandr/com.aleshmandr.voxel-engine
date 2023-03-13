using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile]
    public struct FractureVoxelsJob : IJob
    {
        public int Radius;
        public Vector3Int LocalPoint;
        public NativeArray3d<int> Voxels;
        public NativeList<VoxelData> Result;

        public void Execute() {
            for(int i = -Radius; i <= Radius; i++) {
                for(int j = -Radius; j <= Radius; j++) {
                    for(int k = -Radius; k <= Radius; k++) {
                        if(i * i + j * j + k * k <= Radius * Radius) {
                            int x = i + LocalPoint.x;
                            int y = j + LocalPoint.y;
                            int z = k + LocalPoint.z;
                            if(x >= 0 && x < Voxels.SizeX && y >= 0 && Voxels.SizeY > y && z >= 0 && Voxels.SizeZ > z) {
                                if(Voxels[x, y, z] != 0) {
                                    Result.Add(new VoxelData {
                                        Position = new Vector3(x, y, z),
                                        Color = Utilities.VoxelColor(Voxels[x, y, z])
                                    });
                                    
                                    Voxels[x, y, z] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ExecuteOld() {
            for(int i = -Radius; i <= Radius; i++) {
                for(int j = -Radius; j <= Radius; j++) {
                    for(int k = -Radius; k <= Radius; k++) {
                        if(i * i + j * j + k * k <= Radius * Radius) {
                            int x = i + LocalPoint.x;
                            int y = j + LocalPoint.y;
                            int z = k + LocalPoint.z;
                            if(x >= 0 && x < Voxels.SizeX && y >= 0 && Voxels.SizeY > y && z >= 0 && Voxels.SizeZ > z) {
                                if(Voxels[x, y, z] != 0) {
                                    Result.Add(new VoxelData {
                                        Position = new Vector3(x, y, z),
                                        Color = Utilities.VoxelColor(Voxels[x, y, z])
                                    });
                                    
                                    Voxels[x, y, z] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
