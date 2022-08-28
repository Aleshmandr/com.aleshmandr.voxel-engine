using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    [BurstCompile]
    public struct VoxelMeshGenerationBatchJob : IJobParallelForBatch
    {
        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public Mesh.MeshData MeshData;
        public NativeArray<int> Voxels;

        public void Execute(int startIndex, int count) {
            var vertices = new NativeList<Vector3>(Allocator.Temp);
            var colors = new NativeList<Color32>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
            int endIndex = startIndex + count;

            // Block structure
            // BLOCK: [R-color][G-color][B-color][00][below_back_left_right_above_front]
            //           8bit    8bit     8it  2bit(not used)   6bit(faces)

            // Reset faces
            for(int i = startIndex; i < endIndex; i++) {
                if(Voxels[i] != 0) {
                    Voxels[i] &= ~(1 << 0);
                    Voxels[i] &= ~(1 << 1);
                    Voxels[i] &= ~(1 << 2);
                    Voxels[i] &= ~(1 << 3);
                    Voxels[i] &= ~(1 << 4);
                    Voxels[i] &= ~(1 << 5);
                }
            }

            //TODO
        }
    }
}
