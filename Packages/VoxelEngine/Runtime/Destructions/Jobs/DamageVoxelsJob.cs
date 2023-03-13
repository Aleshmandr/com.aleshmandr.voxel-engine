using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile]
    public struct DamageVoxelsJob : IJob
    {
        public int Radius;
        public Vector3Int LocalPoint;
        public NativeArray3d<int> Voxels;
        public NativeList<VoxelData> Result;

        public void Execute() {
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
                            if(Voxels.IsCoordsValid(x, y, z)) {
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
