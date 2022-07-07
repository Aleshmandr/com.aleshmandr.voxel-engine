using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public class VoxelMeshGenerationJob : IJob
    {
        public Mesh.MeshData MeshData;
        public NativeArray3d<int> blocks;

        public void Execute() {
            var vertices = new NativeList<Vector3>(Allocator.Temp);
            var colors = new NativeList<Color32>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);

            // Block structure
            // BLOCK: [R-color][G-color][B-color][00][below_back_left_right_above_front]
            //           8bit    8bit     8it  2bit(not used)   6bit(faces)

            // Reset faces
            for(int y = 0; y < blocks.SizeY; y++) {
                for(int x = 0; x < blocks.SizeX; x++) {
                    for(int z = 0; z < blocks.SizeZ; z++) {
                        if(blocks[x, y, z] != 0) {
                            blocks[x, y, z] &= ~(1 << 0);
                            blocks[x, y, z] &= ~(1 << 1);
                            blocks[x, y, z] &= ~(1 << 2);
                            blocks[x, y, z] &= ~(1 << 3);
                            blocks[x, y, z] &= ~(1 << 4);
                            blocks[x, y, z] &= ~(1 << 5);
                        }
                    }
                }
            }

            for(int x = 0; x < blocks.SizeX; x++) {
                for(int y = 0; y < blocks.SizeY; y++) {
                    for(int z = 0; z < blocks.SizeZ; z++) {
                        if(blocks[x, y, z] == 0) {
                            continue; // Skip empty blocks
                        }

                        // Check if hidden
                        bool left = false, right = false, above = false, front = false, back = false, below = false;
                        if(z > 0) {
                            if(blocks[x, y, z - 1] != 0) {
                                back = true;
                                blocks[x, y, z] |= 0x10;
                            }
                        }
                        if(z < blocks.SizeZ - 1) {
                            if(blocks[x, y, z + 1] != 0) {
                                front = true;
                                blocks[x, y, z] |= 0x1;
                            }
                        }

                        if(x > 0) {
                            if(blocks[x - 1, y, z] != 0) {
                                left = true;
                                blocks[x, y, z] |= 0x8;
                            }
                        }
                        if(x < blocks.SizeX - 1) {
                            if(blocks[x + 1, y, z] != 0) {
                                right = true;
                                blocks[x, y, z] |= 0x4;
                            }
                        }

                        if(y > 0) {
                            if(blocks[x, y - 1, z] != 0) {
                                below = true;
                                blocks[x, y, z] |= 0x20;
                            }
                        }
                        if(y < blocks.SizeY - 1) {
                            if(blocks[x, y + 1, z] != 0) {
                                above = true;
                                blocks[x, y, z] |= 0x2;
                            }
                        }

                        if(front && left && right && above && back && below) {
                            continue; // Block is hidden
                        }

                        // Draw block
                        if(!below) {
                            if((blocks[x, y, z] & 0x20) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < blocks.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x20) == 0 && IsSameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < blocks.SizeZ; zi++) {
                                        if((blocks[xi, y, zi] & 0x20) == 0 && IsSameColor(blocks[xi, y, zi], blocks[x, y, z])) {
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
                                        blocks[xi, y, zi] |= 0x20;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!above) {
                            // Get above (0010)
                            if((blocks[x, y, z] & 0x2) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < blocks.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x2) == 0 && IsSameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < blocks.SizeZ; zi++) {
                                        if((blocks[xi, y, zi] & 0x2) == 0 && IsSameColor(blocks[xi, y, zi], blocks[x, y, z])) {
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
                                        blocks[xi, y, zi] |= 0x2;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!back) {
                            // back  10000
                            if((blocks[x, y, z] & 0x10) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < blocks.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x10) == 0 && IsSameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < blocks.SizeY; yi++) {
                                        if((blocks[xi, yi, z] & 0x10) == 0 && IsSameColor(blocks[xi, yi, z], blocks[x, y, z])) {
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
                                        blocks[xi, yi, z] |= 0x10;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!front) {
                            // front 0001
                            if((blocks[x, y, z] & 0x1) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < blocks.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x1) == 0 && IsSameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < blocks.SizeY; yi++) {
                                        if((blocks[xi, yi, z] & 0x1) == 0 && IsSameColor(blocks[xi, yi, z], blocks[x, y, z])) {
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
                                        blocks[xi, yi, z] |= 0x1;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!left) {
                            if((blocks[x, y, z] & 0x8) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < blocks.SizeZ; zi++) {
                                    // Check not drawn + same color
                                    if((blocks[x, y, zi] & 0x8) == 0 && IsSameColor(blocks[x, y, zi], blocks[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < blocks.SizeY; yi++) {
                                        if((blocks[x, yi, zi] & 0x8) == 0 && IsSameColor(blocks[x, yi, zi], blocks[x, y, z])) {
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
                                        blocks[x, yi, zi] |= 0x8;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(!right) {
                            if((blocks[x, y, z] & 0x4) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < blocks.SizeZ; zi++) {
                                    // Check not drawn + same color
                                    if((blocks[x, y, zi] & 0x4) == 0 && IsSameColor(blocks[x, y, zi], blocks[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < blocks.SizeY; yi++) {
                                        if((blocks[x, yi, zi] & 0x4) == 0 && IsSameColor(blocks[x, yi, zi], blocks[x, y, z])) {
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
                                        blocks[x, yi, zi] |= 0x4;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }
                    }
                }
            }

            var attributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
            attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position);
            attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UInt32, 4, stream: 1);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSameColor(int block1, int block2) {
            return ((block1 >> 8) & 0xFFFFFF) == ((block2 >> 8) & 0xFFFFFF) && block1 != 0 && block2 != 0;
        }
    }
}
