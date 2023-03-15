using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine.Jobs
{
    [BurstCompile]
    public struct VoxelOptimizedMeshGenerationJob : IJob
    {
        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public Mesh.MeshData MeshData;
        public NativeArray<int> Voxels;

        public void Execute() {
            var vertices = new NativeList<Vector3>(Allocator.Temp);
            var colors = new NativeList<Color32>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);

            // Block structure
            // BLOCK: [R-color][G-color][B-color][00][below_back_left_right_above_front]
            //           8bit    8bit     8it  2bit(not used)   6bit(faces)

            // Reset faces
            for(int i = 0; i < Voxels.Length; i++) {
                if(Voxels[i] != 0) {
                    Voxels[i] &= ~(1 << 0);
                    Voxels[i] &= ~(1 << 1);
                    Voxels[i] &= ~(1 << 2);
                    Voxels[i] &= ~(1 << 3);
                    Voxels[i] &= ~(1 << 4);
                    Voxels[i] &= ~(1 << 5);
                }
            }

            for(int x = 0; x < SizeX; x++) {
                for(int y = 0; y < SizeY; y++) {
                    for(int z = 0; z < SizeZ; z++) {
                        int indexXYZ = x + SizeX * (y + SizeY * z);

                        if(Voxels[indexXYZ] == 0) {
                            continue; // Skip empty blocks
                        }

                        // Check if hidden
                        bool left = false, right = false, above = false, front = false, back = false, below = false;
                        if(z > 0) {
                            int indexXYZm = x + SizeX * (y + SizeY * (z - 1));
                            if(Voxels[indexXYZm] != 0) {
                                back = true;
                                Voxels[indexXYZ] |= 0x10;
                            }
                        }
                        if(z < SizeZ - 1) {
                            int indexXYZp = x + SizeX * (y + SizeY * (z + 1));
                            if(Voxels[indexXYZp] != 0) {
                                front = true;
                                Voxels[indexXYZ] |= 0x1;
                            }
                        }

                        if(x > 0) {
                            if(Voxels[indexXYZ - 1] != 0) {
                                left = true;
                                Voxels[indexXYZ] |= 0x8;
                            }
                        }
                        if(x < SizeX - 1) {
                            if(Voxels[indexXYZ + 1] != 0) {
                                right = true;
                                Voxels[indexXYZ] |= 0x4;
                            }
                        }

                        if(y > 0) {
                            int indexXYmZ = x + SizeX * (y - 1 + SizeY * z);
                            if(Voxels[indexXYmZ] != 0) {
                                below = true;
                                Voxels[indexXYZ] |= 0x20;
                            }
                        }
                        if(y < SizeY - 1) {
                            int indexXYpZ = x + SizeX * (y + 1 + SizeY * z);
                            if(Voxels[indexXYpZ] != 0) {
                                above = true;
                                Voxels[indexXYZ] |= 0x2;
                            }
                        }

                        if(front && left && right && above && back && below) {
                            continue; // Block is hidden
                        }
                        
                        int v0 = -1;
                        int v1 = -1;
                        int v2 = -1;
                        int v3 = -1;
                        int v4 = -1;
                        int v5 = -1;
                        int v6 = -1;
                        int v7 = -1;
                        int prevMaxX = -1;
                        int prevMaxY = -1;
                        int prevMaxZ = -1;

                        var currentVoxel = Voxels[indexXYZ];
                        var color = new Color32((byte)(currentVoxel >> 24 & 0xFF),
                                                (byte)(currentVoxel >> 16 & 0xFF),
                                                (byte)(currentVoxel >> 8 & 0xFF),
                                                255
                        );

                        // Draw block
                        if(!below) {
                            if((Voxels[indexXYZ] & 0x20) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < SizeX; xi++) {
                                    int indexXiYZ = xi + SizeX * (y + SizeY * z); 
                                    // Check not drawn + same color
                                    if((Voxels[indexXiYZ] & 0x20) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], currentVoxel)) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < SizeZ; zi++) {
                                        int indexXiYZi = xi + SizeX * (y + SizeY * zi); 
                                        if((Voxels[indexXiYZi] & 0x20) == 0 && Utilities.IsSameColor(Voxels[indexXiYZi], currentVoxel)) {
                                            tmpZ++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if(tmpZ < maxZ || maxZ == 0) {
                                        maxZ = tmpZ;
                                    }
                                }

                                for(int xi = x; xi < x + maxX; xi++) {
                                    for(int zi = z; zi < z + maxZ; zi++) {
                                        int indexXiYZi = xi + SizeX * (y + SizeY * zi); 
                                        Voxels[indexXiYZi] |= 0x20;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                int idx = vertices.Length;

                                v0 = idx;
                                v6 = idx + 1;
                                v7 = idx + 2;
                                v3 = idx + 3;

                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y - 1, z - 1));
                                
                                colors.Add(color);
                                colors.Add(color);
                                colors.Add(color);
                                colors.Add(color);

                                // Add triangle indices
                                triangles.Add(v0);
                                triangles.Add(v3);
                                triangles.Add(v6);

                                triangles.Add(v3);
                                triangles.Add(v7);
                                triangles.Add(v6);
                            }
                        }

                        if(!above) {
                            // Get above (0010)
                            if((Voxels[indexXYZ] & 0x2) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < SizeX; xi++) {
                                    int indexXiYZ = xi + SizeX * (y + SizeY * z); 
                                    // Check not drawn + same color
                                    if((Voxels[indexXiYZ] & 0x2) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], currentVoxel)) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < SizeZ; zi++) {
                                        int indexXiYZi = xi + SizeX * (y + SizeY * zi); 
                                        if((Voxels[indexXiYZi] & 0x2) == 0 && Utilities.IsSameColor(Voxels[indexXiYZi], currentVoxel)) {
                                            tmpZ++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if(tmpZ < maxZ || maxZ == 0) {
                                        maxZ = tmpZ;
                                    }
                                }
                                for(int xi = x; xi < x + maxX; xi++) {
                                    for(int zi = z; zi < z + maxZ; zi++) {
                                        int indexXiYZi = xi + SizeX * (y + SizeY * zi); 
                                        Voxels[indexXiYZi] |= 0x2;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                int idx = vertices.Length;
                                v1 = idx;
                                v4 = idx + 1;
                                v5 = idx + 2;
                                v2 = idx + 3;

                                vertices.Add(new Vector3(x - 1, y, z - 1));
                                vertices.Add(new Vector3(x - 1, y, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y, z - 1));
                                
                                colors.Add(color);
                                colors.Add(color);
                                colors.Add(color);
                                colors.Add(color);

                                // Add triangle indices
                                triangles.Add(v1);
                                triangles.Add(v4);
                                triangles.Add(v2);

                                triangles.Add(v4);
                                triangles.Add(v5);
                                triangles.Add(v2);
                            }
                        }

                        if(!back) {
                            // back  10000
                            if((Voxels[indexXYZ] & 0x10) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < SizeX; xi++) {
                                    int indexXiYZ = xi + SizeX * (y + SizeY * z); 
                                    // Check not drawn + same color
                                    if((Voxels[indexXiYZ] & 0x10) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], currentVoxel)) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXiYiZ = xi + SizeX * (yi + SizeY * z); 
                                        if((Voxels[indexXiYiZ] & 0x10) == 0 && Utilities.IsSameColor(Voxels[indexXiYiZ], currentVoxel)) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if(tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for(int xi = x; xi < x + maxX; xi++) {
                                    for(int yi = y; yi < y + maxY; yi++) {
                                        int indexXiYiZ = xi + SizeX * (yi + SizeY * z); 
                                        Voxels[indexXiYiZ] |= 0x10;
                                    }
                                }
                                maxX--;
                                maxY--;
                                
                                if(prevMaxX != maxX) {
                                    prevMaxX = maxX;
                                    v2 = -1;
                                    v3 = -1;
                                }

                                if(prevMaxY != maxY) {
                                    prevMaxY = maxY;
                                    v1 = -1;
                                    v2 = -1;
                                }

                                int idx = vertices.Length;

                                if(v0 < 0) {
                                    vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                    colors.Add(color);
                                    v0 = idx;
                                    idx++;
                                }

                                if(v1 < 0) {
                                    vertices.Add(new Vector3(x - 1, y + maxY, z - 1));
                                    colors.Add(color);
                                    v1 = idx;
                                    idx++;
                                }

                                if(v2 < 0) {
                                    vertices.Add(new Vector3(x + maxX, y + maxY, z - 1));
                                    colors.Add(color);
                                    v2 = idx;
                                    idx++;
                                }

                                if(v3 < 0) {
                                    vertices.Add(new Vector3(x + maxX, y - 1, z - 1));
                                    colors.Add(color);
                                    v3 = idx;
                                }

                                triangles.Add(v0);
                                triangles.Add(v1);
                                triangles.Add(v2);

                                triangles.Add(v2);
                                triangles.Add(v3);
                                triangles.Add(v0);
                            }
                        }

                        if(!front) {
                            // front 0001
                            if((Voxels[indexXYZ] & 0x1) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < SizeX; xi++) {
                                    int indexXiYZ = xi + SizeX * (y + SizeY * z); 
                                    // Check not drawn + same color
                                    if((Voxels[indexXiYZ] & 0x1) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], currentVoxel)) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXiYiZ = xi + SizeX * (yi + SizeY * z); 
                                        if((Voxels[indexXiYiZ] & 0x1) == 0 && Utilities.IsSameColor(Voxels[indexXiYiZ], currentVoxel)) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if(tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for(int xi = x; xi < x + maxX; xi++) {
                                    for(int yi = y; yi < y + maxY; yi++) {
                                        int indexXiYiZ = xi + SizeX * (yi + SizeY * z); 
                                        Voxels[indexXiYiZ] |= 0x1;
                                    }
                                }
                                maxX--;
                                maxY--;
                                
                                if(prevMaxX != maxX) {
                                    v5 = -1;
                                    v7 = -1;
                                }

                                if(prevMaxY != maxY) {
                                    prevMaxY = maxY;
                                    v4 = -1;
                                    v5 = -1;
                                }

                                int idx = vertices.Length;

                                if(v4 < 0) {
                                    vertices.Add(new Vector3(x - 1, y + maxY, z));
                                    colors.Add(color);
                                    v4 = idx;
                                    idx++;
                                }

                                if(v5 < 0) {
                                    vertices.Add(new Vector3(x + maxX, y + maxY, z));
                                    colors.Add(color);
                                    v5 = idx;
                                    idx++;
                                }

                                if(v7 < 0) {
                                    vertices.Add(new Vector3(x + maxX, y - 1, z));
                                    colors.Add(color);
                                    v7 = idx;
                                    idx++;
                                }

                                if(v6 < 0) {
                                    vertices.Add(new Vector3(x - 1, y - 1, z));
                                    colors.Add(color);
                                    v6 = idx;
                                }

                                triangles.Add(v4);
                                triangles.Add(v6);
                                triangles.Add(v7);

                                triangles.Add(v5);
                                triangles.Add(v4);
                                triangles.Add(v7);
                            }
                        }

                        if(!left) {
                            if((Voxels[indexXYZ] & 0x8) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < SizeZ; zi++) {
                                    int indexXYZi = x + SizeX * (y + SizeY * zi);

                                    // Check not drawn + same color
                                    if((Voxels[indexXYZi] & 0x8) == 0 && Utilities.IsSameColor(Voxels[indexXYZi], currentVoxel)) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXYiZi = x + SizeX * (yi + SizeY * zi);
                                        if((Voxels[indexXYiZi] & 0x8) == 0 && Utilities.IsSameColor(Voxels[indexXYiZi], currentVoxel)) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if(tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for(int zi = z; zi < z + maxZ; zi++) {
                                    for(int yi = y; yi < y + maxY; yi++) {
                                        int indexXYiZi = x + SizeX * (yi + SizeY * zi);
                                        Voxels[indexXYiZi] |= 0x8;
                                    }
                                }
                                maxZ--;
                                maxY--;
                                
                                if(prevMaxY != maxY) {
                                    v1 = -1;
                                    v4 = -1;
                                }

                                if(prevMaxZ != maxZ) {
                                    v6 = -1;
                                    v4 = -1;
                                }

                                int idx = vertices.Length;

                                if(v0 < 0) {
                                    vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                    colors.Add(color);
                                    v0 = idx;
                                    idx++;
                                }

                                if(v1 < 0) {
                                    vertices.Add(new Vector3(x - 1, y + maxY, z - 1));
                                    colors.Add(color);
                                    v1 = idx;
                                    idx++;
                                }

                                if(v4 < 0) {
                                    vertices.Add(new Vector3(x - 1, y + maxY, z + maxZ));
                                    colors.Add(color);
                                    v4 = idx;
                                    idx++;
                                }

                                if(v6 < 0) {
                                    vertices.Add(new Vector3(x - 1, y - 1, z + maxZ));
                                    colors.Add(color);
                                    v6 = idx;
                                }

                                triangles.Add(v0);
                                triangles.Add(v6);
                                triangles.Add(v4);

                                triangles.Add(v0);
                                triangles.Add(v4);
                                triangles.Add(v1);
                            }
                        }

                        if(!right) {
                            if((Voxels[indexXYZ] & 0x4) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < SizeZ; zi++) {
                                    // Check not drawn + same color
                                    int indexXYZi = x + SizeX * (y + SizeY * zi);
                                    if((Voxels[indexXYZi] & 0x4) == 0 && Utilities.IsSameColor(Voxels[indexXYZi], currentVoxel)) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXYiZi = x + SizeX * (yi + SizeY * zi);
                                        if((Voxels[indexXYiZi] & 0x4) == 0 && Utilities.IsSameColor(Voxels[indexXYiZi], currentVoxel)) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if(tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for(int zi = z; zi < z + maxZ; zi++) {
                                    for(int yi = y; yi < y + maxY; yi++) {
                                        int indexXYiZi = x + SizeX * (yi + SizeY * zi);
                                        Voxels[indexXYiZi] |= 0x4;
                                    }
                                }
                                maxZ--;
                                maxY--;
                                
                                if(prevMaxY != maxY) {
                                    v5 = -1;
                                    v2 = -1;
                                }
                                
                                if(prevMaxZ != maxZ) {
                                    v5 = -1;
                                    v7 = -1;
                                }

                                int idx = vertices.Length;

                                if(v3 < 0) {
                                    vertices.Add(new Vector3(x, y - 1, z - 1));
                                    colors.Add(color);
                                    v3 = idx;
                                    idx++;
                                }

                                if(v2 < 0) {
                                    vertices.Add(new Vector3(x, y + maxY, z - 1));
                                    colors.Add(color);
                                    v2 = idx;
                                    idx++;
                                }

                                if(v5 < 0) {
                                    vertices.Add(new Vector3(x, y + maxY, z + maxZ));
                                    colors.Add(color);
                                    v5 = idx;
                                    idx++;
                                }

                                if(v7 < 0) {
                                    vertices.Add(new Vector3(x, y - 1, z + maxZ));
                                    colors.Add(color);
                                    v7 = idx;
                                }

                                triangles.Add(v3);
                                triangles.Add(v2);
                                triangles.Add(v5);

                                triangles.Add(v7);
                                triangles.Add(v3);
                                triangles.Add(v5);
                            }
                        }
                    }
                }
            }

            var attributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
            attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position);
            attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, stream: 1);
            MeshData.SetVertexBufferParams(vertices.Length, attributes);

            var positions = MeshData.GetVertexData<Vector3>();
            var vertexColor = MeshData.GetVertexData<Color>(stream: 1);
            for(int i = 0; i < vertices.Length; i++) {
                positions[i] = vertices[i];
                vertexColor[i] = colors[i];
            }

            MeshData.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            var indexes = MeshData.GetIndexData<uint>();
            for(int i = 0; i < triangles.Length; i++) {
                indexes[i] = (uint)triangles[i];
            }

            // One sub-mesh with all the indices.
            MeshData.subMeshCount = 1;
            MeshData.SetSubMesh(0, new SubMeshDescriptor(0, indexes.Length));
        }
    }
}
