using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct VoxelMeshGenerationJob : IJob
    {
        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public Mesh.MeshData MeshData;
        [DeallocateOnJobCompletion]
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

                        // Draw block
                        if(!below) {
                            if((Voxels[indexXYZ] & 0x20) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < SizeX; xi++) {
                                    int indexXiYZ = xi + SizeX * (y + SizeY * z); 
                                    // Check not drawn + same color
                                    if((Voxels[indexXiYZ] & 0x20) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], Voxels[indexXYZ])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < SizeZ; zi++) {
                                        int indexXiYZi = xi + SizeX * (y + SizeY * zi); 
                                        if((Voxels[indexXiYZi] & 0x20) == 0 && Utilities.IsSameColor(Voxels[indexXiYZi], Voxels[indexXYZ])) {
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

                                vertices.Add(new Vector3(x + maxX, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z + maxZ));

                                // Add triangle indices
                                triangles.Add(idx + 2);
                                triangles.Add(idx + 1);
                                triangles.Add(idx);

                                idx = vertices.Length;

                                vertices.Add(new Vector3(x + maxX, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));

                                triangles.Add(idx + 2);
                                triangles.Add(idx + 1);
                                triangles.Add(idx);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((Voxels[indexXYZ] >> 24) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 16) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
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
                                    if((Voxels[indexXiYZ] & 0x2) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], Voxels[indexXYZ])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < SizeZ; zi++) {
                                        int indexXiYZi = xi + SizeX * (y + SizeY * zi); 
                                        if((Voxels[indexXiYZi] & 0x2) == 0 && Utilities.IsSameColor(Voxels[indexXiYZi], Voxels[indexXYZ])) {
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

                                vertices.Add(new Vector3(x + maxX, y, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y, z - 1));
                                vertices.Add(new Vector3(x - 1, y, z + maxZ));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Length;

                                vertices.Add(new Vector3(x + maxX, y, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y, z - 1));
                                vertices.Add(new Vector3(x - 1, y, z - 1));


                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((Voxels[indexXYZ] >> 24) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 16) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
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
                                    if((Voxels[indexXiYZ] & 0x10) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], Voxels[indexXYZ])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXiYiZ = xi + SizeX * (yi + SizeY * z); 
                                        if((Voxels[indexXiYiZ] & 0x10) == 0 && Utilities.IsSameColor(Voxels[indexXiYiZ], Voxels[indexXYZ])) {
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

                                int idx = vertices.Length;

                                vertices.Add(new Vector3(x + maxX, y + maxY, z - 1));
                                vertices.Add(new Vector3(x + maxX, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Length;


                                vertices.Add(new Vector3(x + maxX, y + maxY, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y + maxY, z - 1));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((Voxels[indexXYZ] >> 24) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 16) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
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
                                    if((Voxels[indexXiYZ] & 0x1) == 0 && Utilities.IsSameColor(Voxels[indexXiYZ], Voxels[indexXYZ])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXiYiZ = xi + SizeX * (yi + SizeY * z); 
                                        if((Voxels[indexXiYiZ] & 0x1) == 0 && Utilities.IsSameColor(Voxels[indexXiYiZ], Voxels[indexXYZ])) {
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

                                int idx = vertices.Length;

                                vertices.Add(new Vector3(x + maxX, y + maxY, z));
                                vertices.Add(new Vector3(x - 1, y + maxY, z));
                                vertices.Add(new Vector3(x + maxX, y - 1, z));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Length;
                                vertices.Add(new Vector3(x - 1, y + maxY, z));
                                vertices.Add(new Vector3(x - 1, y - 1, z));
                                vertices.Add(new Vector3(x + maxX, y - 1, z));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((Voxels[indexXYZ] >> 24) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 16) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!left) {
                            if((Voxels[indexXYZ] & 0x8) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < SizeZ; zi++) {
                                    int indexXYZi = x + SizeX * (y + SizeY * zi);

                                    // Check not drawn + same color
                                    if((Voxels[indexXYZi] & 0x8) == 0 && Utilities.IsSameColor(Voxels[indexXYZi], Voxels[indexXYZ])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXYiZi = x + SizeX * (yi + SizeY * zi);
                                        if((Voxels[indexXYiZi] & 0x8) == 0 && Utilities.IsSameColor(Voxels[indexXYiZi], Voxels[indexXYZ])) {
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

                                int idx = vertices.Length;

                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y + maxY, z + maxZ));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Length;
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y + maxY, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y + maxY, z - 1));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);


                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((Voxels[indexXYZ] >> 24) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 16) & 0xFF),
                                                           (byte)((Voxels[indexXYZ] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!right) {
                            if((Voxels[indexXYZ] & 0x4) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < SizeZ; zi++) {
                                    // Check not drawn + same color
                                    int indexXYZi = x + SizeX * (y + SizeY * zi);
                                    if((Voxels[indexXYZi] & 0x4) == 0 && Utilities.IsSameColor(Voxels[indexXYZi], Voxels[indexXYZ])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < SizeY; yi++) {
                                        int indexXYiZi = x + SizeX * (yi + SizeY * zi);
                                        if((Voxels[indexXYiZi] & 0x4) == 0 && Utilities.IsSameColor(Voxels[indexXYiZi], Voxels[indexXYZ])) {
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

                                int idx = vertices.Length;

                                vertices.Add(new Vector3(x, y - 1, z - 1));
                                vertices.Add(new Vector3(x, y + maxY, z + maxZ));
                                vertices.Add(new Vector3(x, y - 1, z + maxZ));
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Length;
                                vertices.Add(new Vector3(x, y + maxY, z + maxZ));
                                vertices.Add(new Vector3(x, y - 1, z - 1));
                                vertices.Add(new Vector3(x, y + maxY, z - 1));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(Utilities.VoxelColor(Voxels[indexXYZ] ));
                                }
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
