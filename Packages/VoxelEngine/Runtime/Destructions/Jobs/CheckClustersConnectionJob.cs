using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile]
    public struct CheckClustersConnectionJob : IJob
    {
        public NativeArray3d<int> ChunkOneData;
        public NativeArray3d<int> ChunkTwoData;
        public float3 ChunkOnePos;
        public float3 ChunkTwoPos;
        public NativeArray<bool> Result;
        private const float Epsilon = 0.01f;

        public void Execute() {
            var clustersDelta = ChunkTwoPos - ChunkOnePos;
            
            var dx = clustersDelta.x > 0f ? clustersDelta.x - ChunkOneData.SizeX 
                : math.abs(clustersDelta.x) - ChunkTwoData.SizeX;
            
            var dy = clustersDelta.y > 0f ? clustersDelta.y - ChunkOneData.SizeY 
                : math.abs(clustersDelta.y) - ChunkTwoData.SizeY;
            
            var dz = clustersDelta.z > 0f ? clustersDelta.z - ChunkOneData.SizeZ 
                : math.abs(clustersDelta.z) - ChunkTwoData.SizeZ;
            
            if(dx > Epsilon || dy > Epsilon || dz > Epsilon) {
                return;
            }

            for(int i1 = 0; i1 < ChunkOneData.SizeX; i1++) {
                for(int j1 = 0; j1 < ChunkOneData.SizeY; j1++) {
                    for(int k1 = 0; k1 < ChunkOneData.SizeZ; k1++) {
                        if(ChunkOneData[i1, j1, k1] == 0 || IsVoxelInner(ChunkOneData, i1, j1, k1)) {
                            continue;
                        }
                        
                        float vx = i1 + ChunkOnePos.x;
                        float vy = j1 + ChunkOnePos.y;
                        float vz = k1 + ChunkOnePos.z;

                        for(int i2 = 0; i2 < ChunkTwoData.SizeX; i2++) {
                            for(int j2 = 0; j2 < ChunkTwoData.SizeY; j2++) {
                                for(int k2 = 0; k2 < ChunkTwoData.SizeZ; k2++) {
                                    if(ChunkTwoData[i2, j2, k2] == 0 || IsVoxelInner(ChunkTwoData, i2, j2, k2)) {
                                        continue;
                                    }
                                    
                                    float ox = i2 + ChunkTwoPos.x;
                                    float oy = j2 + ChunkTwoPos.y;
                                    float oz = k2 + ChunkTwoPos.z;
                                    
                                    var dist = math.abs(vx - ox) + math.abs(vy - oy) + math.abs(vz - oz);
                                    if(dist <= 1f + Epsilon) {
                                        Result[0] = true;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsVoxelInner(NativeArray3d<int> voxels, int x, int y, int z) {
            return voxels.IsCoordsValid(x, y, z)
                && voxels.IsCoordsValid(x + 1, y, z) && voxels[x + 1, y, z] != 0
                && voxels.IsCoordsValid(x - 1, y, z) && voxels[x - 1, y, z] != 0
                && voxels.IsCoordsValid(x, y + 1, z) && voxels[x, y + 1, z] != 0
                && voxels.IsCoordsValid(x, y - 1, z) && voxels[x, y - 1, z] != 0
                && voxels.IsCoordsValid(x, y, z + 1) && voxels[x, y, z + 1] != 0
                && voxels.IsCoordsValid(x, y, z - 1) && voxels[x, y, z - 1] != 0;
        }
    }
}
