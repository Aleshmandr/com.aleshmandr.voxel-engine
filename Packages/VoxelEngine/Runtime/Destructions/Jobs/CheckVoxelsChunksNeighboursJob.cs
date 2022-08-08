using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Destructions.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct CheckVoxelsChunksNeighboursJob : IJob
    {
        public NativeArray3d<int> ChunkOneData;
        public NativeArray3d<int> ChunkTwoData;
        public Vector3 ChunkOnePos;
        public Vector3 ChunkTwoPos;
        public NativeArray<bool> Result;
        private const float Epsilon = 0.01f;
        private const float NearAxisCheckDistance = 1f + Epsilon;

        public void Execute() {
            var clustersDelta = ChunkTwoPos - ChunkOnePos;
            var dx = Mathf.Abs(clustersDelta.x) - (ChunkOneData.SizeX + ChunkTwoData.SizeX) * 0.5f;
            var dy = Mathf.Abs(clustersDelta.y) - (ChunkOneData.SizeY + ChunkTwoData.SizeY) * 0.5f;
            var dz = Mathf.Abs(clustersDelta.z) - (ChunkOneData.SizeZ + ChunkTwoData.SizeZ) * 0.5f;
            if(dx > NearAxisCheckDistance || dy > NearAxisCheckDistance || dz > NearAxisCheckDistance) {
                return;
            }

            for(int i1 = 0; i1 < ChunkOneData.SizeX; i1++) {
                for(int j1 = 0; j1 < ChunkOneData.SizeY; j1++) {
                    for(int k1 = 0; k1 < ChunkOneData.SizeZ; k1++) {
                        if(ChunkOneData[i1, j1, k1] == 0 || IsVoxelInner(ChunkOneData, i1, j1, k1)) {
                            continue;
                        }
                        var voxelPos = new Vector3(i1 + ChunkOnePos.x, j1 + ChunkOnePos.y, k1 + ChunkOnePos.z);

                        for(int i2 = 0; i2 < ChunkTwoData.SizeX; i2++) {
                            for(int j2 = 0; j2 < ChunkTwoData.SizeY; j2++) {
                                for(int k2 = 0; k2 < ChunkTwoData.SizeZ; k2++) {
                                    if(ChunkTwoData[i2, j2, k2] == 0 || IsVoxelInner(ChunkTwoData, i2, j2, k2)) {
                                        continue;
                                    }
                                    var otherVoxelPos = new Vector3(i2 + ChunkTwoPos.x, j2 + ChunkTwoPos.y, k2 + ChunkTwoPos.z);
                                    if((otherVoxelPos - voxelPos).sqrMagnitude <= 1f + Epsilon) {
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
