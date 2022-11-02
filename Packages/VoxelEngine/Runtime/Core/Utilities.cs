using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public static class Utilities
    {
        private const int MaxMeshSizeUInt16 = 65535;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSameColor(int voxel1, int voxel2) {
            return ((voxel1 >> 8) & 0xFFFFFF) == ((voxel2 >> 8) & 0xFFFFFF) && voxel1 != 0 && voxel2 != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color32 VoxelColor(int voxel) {
            return new Color32((byte)((voxel >> 24) & 0xFF),
                               (byte)((voxel >> 16) & 0xFF),
                               (byte)((voxel >> 8) & 0xFF),
                               255);
        }

        public static Mesh GenerateMesh(NativeArray3d<int> voxels, Mesh mesh = null) {
            List<Vector3> vertices = new List<Vector3>();
            List<Color32> colors = new List<Color32>();
            List<int> triangles = new List<int>();

            // Block structure
            // BLOCK: [R-color][G-color][B-color][00][below_back_left_right_above_front]
            //           8bit    8bit     8it  2bit(not used)   6bit(faces)

            // Reset faces
            for(int y = 0; y < voxels.SizeY; y++) {
                for(int x = 0; x < voxels.SizeX; x++) {
                    for(int z = 0; z < voxels.SizeZ; z++) {
                        if(voxels[x, y, z] != 0) {
                            voxels[x, y, z] &= ~(1 << 0);
                            voxels[x, y, z] &= ~(1 << 1);
                            voxels[x, y, z] &= ~(1 << 2);
                            voxels[x, y, z] &= ~(1 << 3);
                            voxels[x, y, z] &= ~(1 << 4);
                            voxels[x, y, z] &= ~(1 << 5);
                        }
                    }
                }
            }

            for(int x = 0; x < voxels.SizeX; x++) {
                for(int y = 0; y < voxels.SizeY; y++) {
                    for(int z = 0; z < voxels.SizeZ; z++) {
                        if(voxels[x, y, z] == 0) {
                            continue; // Skip empty blocks
                        }

                        // Check if hidden
                        bool left = false, right = false, above = false, front = false, back = false, below = false;
                        if(z > 0) {
                            if(voxels[x, y, z - 1] != 0) {
                                back = true;
                                voxels[x, y, z] |= 0x10;
                            }
                        }
                        if(z < voxels.SizeZ - 1) {
                            if(voxels[x, y, z + 1] != 0) {
                                front = true;
                                voxels[x, y, z] |= 0x1;
                            }
                        }

                        if(x > 0) {
                            if(voxels[x - 1, y, z] != 0) {
                                left = true;
                                voxels[x, y, z] |= 0x8;
                            }
                        }
                        if(x < voxels.SizeX - 1) {
                            if(voxels[x + 1, y, z] != 0) {
                                right = true;
                                voxels[x, y, z] |= 0x4;
                            }
                        }

                        if(y > 0) {
                            if(voxels[x, y - 1, z] != 0) {
                                below = true;
                                voxels[x, y, z] |= 0x20;
                            }
                        }
                        if(y < voxels.SizeY - 1) {
                            if(voxels[x, y + 1, z] != 0) {
                                above = true;
                                voxels[x, y, z] |= 0x2;
                            }
                        }

                        if(front && left && right && above && back && below) {
                            continue; // Block is hidden
                        }

                        // Draw block
                        if(!below) {
                            if((voxels[x, y, z] & 0x20) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x20) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < voxels.SizeZ; zi++) {
                                        if((voxels[xi, y, zi] & 0x20) == 0 && IsSameColor(voxels[xi, y, zi], voxels[x, y, z])) {
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
                                        voxels[xi, y, zi] |= 0x20;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                int idx = vertices.Count;

                                vertices.Add(new Vector3(x + maxX, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y - 1, z - 1));

                                // Add triangle indices
                                triangles.Add(idx + 2);
                                triangles.Add(idx + 1);
                                triangles.Add(idx);
                                
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 3);
                                triangles.Add(idx);

                                for(int n = 0; n < 4; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           255
                                               ));
                                }
                            }
                        }

                        if(!above) {
                            // Get above (0010)
                            if((voxels[x, y, z] & 0x2) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x2) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < voxels.SizeZ; zi++) {
                                        if((voxels[xi, y, zi] & 0x2) == 0 && IsSameColor(voxels[xi, y, zi], voxels[x, y, z])) {
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
                                        voxels[xi, y, zi] = voxels[xi, y, zi] | 0x2;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                int idx = vertices.Count;

                                vertices.Add(new Vector3(x + maxX, y, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y, z - 1));
                                vertices.Add(new Vector3(x - 1, y, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y, z - 1));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                triangles.Add(idx);
                                triangles.Add(idx + 3);
                                triangles.Add(idx + 1);

                                for(int n = 0; n < 4; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           255
                                               ));
                                }
                            }
                        }

                        if(!back) {
                            // back  10000
                            if((voxels[x, y, z] & 0x10) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x10) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[xi, yi, z] & 0x10) == 0 && IsSameColor(voxels[xi, yi, z], voxels[x, y, z])) {
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
                                        voxels[xi, yi, z] |= 0x10;
                                    }
                                }
                                maxX--;
                                maxY--;

                                int idx = vertices.Count;

                                vertices.Add(new Vector3(x + maxX, y + maxY, z - 1));
                                vertices.Add(new Vector3(x + maxX, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y + maxY, z - 1));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);
                                
                                triangles.Add(idx + 3);
                                triangles.Add(idx);
                                triangles.Add(idx + 2);
                                
                                for(int n = 0; n < 4; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           255
                                               ));
                                }
                            }
                        }

                        if(!front) {
                            // front 0001
                            if((voxels[x, y, z] & 0x1) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x1) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[xi, yi, z] & 0x1) == 0 && IsSameColor(voxels[xi, yi, z], voxels[x, y, z])) {
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
                                        voxels[xi, yi, z] |= 0x1;
                                    }
                                }
                                maxX--;
                                maxY--;

                                int idx = vertices.Count;

                                vertices.Add(new Vector3(x + maxX, y + maxY, z));
                                vertices.Add(new Vector3(x - 1, y + maxY, z));
                                vertices.Add(new Vector3(x + maxX, y - 1, z));
                                vertices.Add(new Vector3(x - 1, y - 1, z));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);
                                
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 3);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 4; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           255
                                               ));
                                }
                            }
                        }

                        if(!left) {
                            if((voxels[x, y, z] & 0x8) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < voxels.SizeZ; zi++) {
                                    // Check not drawn + same color
                                    if((voxels[x, y, zi] & 0x8) == 0 && IsSameColor(voxels[x, y, zi], voxels[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[x, yi, zi] & 0x8) == 0 && IsSameColor(voxels[x, yi, zi], voxels[x, y, z])) {
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
                                        voxels[x, yi, zi] |= 0x8;
                                    }
                                }
                                maxZ--;
                                maxY--;

                                int idx = vertices.Count;

                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y + maxY, z + maxZ));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);
                                
                                vertices.Add(new Vector3(x - 1, y + maxY, z - 1));
                                
                                triangles.Add(idx + 3);
                                triangles.Add(idx);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 4; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           255
                                               ));
                                }
                            }
                        }

                        if(!right) {
                            if((voxels[x, y, z] & 0x4) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < voxels.SizeZ; zi++) {
                                    // Check not drawn + same color
                                    if((voxels[x, y, zi] & 0x4) == 0 && IsSameColor(voxels[x, y, zi], voxels[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[x, yi, zi] & 0x4) == 0 && IsSameColor(voxels[x, yi, zi], voxels[x, y, z])) {
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
                                        voxels[x, yi, zi] |= 0x4;
                                    }
                                }
                                maxZ--;
                                maxY--;

                                int idx = vertices.Count;

                                vertices.Add(new Vector3(x, y - 1, z - 1));
                                vertices.Add(new Vector3(x, y + maxY, z + maxZ));
                                vertices.Add(new Vector3(x, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x, y + maxY, z - 1));
                                
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);
                                
                                triangles.Add(idx);
                                triangles.Add(idx + 3);
                                triangles.Add(idx + 1);
                                
                                for(int n = 0; n < 4; n++) {
                                    colors.Add(VoxelColor(voxels[x, y, z]));
                                }
                            }
                        }
                    }
                }
            }

            if(mesh == null) {
                mesh = new Mesh();
                mesh.MarkDynamic();
            } else {
                mesh.Clear();
            }

            mesh.indexFormat = vertices.Count > MaxMeshSizeUInt16 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors32 = colors.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            mesh.UploadMeshData(true);
            return mesh;
        }
        
        public static Mesh GenerateOptimizedMesh(NativeArray3d<int> voxels, Mesh mesh = null) {
            List<Vector3> vertices = new List<Vector3>();
            List<Color32> colors = new List<Color32>();
            List<int> triangles = new List<int>();

            // Block structure
            // BLOCK: [R-color][G-color][B-color][00][below_back_left_right_above_front]
            //           8bit    8bit     8it  2bit(not used)   6bit(faces)

            // Reset faces
            for(int y = 0; y < voxels.SizeY; y++) {
                for(int x = 0; x < voxels.SizeX; x++) {
                    for(int z = 0; z < voxels.SizeZ; z++) {
                        if(voxels[x, y, z] != 0) {
                            voxels[x, y, z] &= ~(1 << 0);
                            voxels[x, y, z] &= ~(1 << 1);
                            voxels[x, y, z] &= ~(1 << 2);
                            voxels[x, y, z] &= ~(1 << 3);
                            voxels[x, y, z] &= ~(1 << 4);
                            voxels[x, y, z] &= ~(1 << 5);
                        }
                    }
                }
            }

            for(int x = 0; x < voxels.SizeX; x++) {
                for(int y = 0; y < voxels.SizeY; y++) {
                    for(int z = 0; z < voxels.SizeZ; z++) {
                        if(voxels[x, y, z] == 0) {
                            continue; // Skip empty blocks
                        }

                        // Check if hidden
                        bool left = false, right = false, above = false, front = false, back = false, below = false;
                        if(z > 0) {
                            if(voxels[x, y, z - 1] != 0) {
                                back = true;
                                voxels[x, y, z] |= 0x10;
                            }
                        }
                        if(z < voxels.SizeZ - 1) {
                            if(voxels[x, y, z + 1] != 0) {
                                front = true;
                                voxels[x, y, z] |= 0x1;
                            }
                        }

                        if(x > 0) {
                            if(voxels[x - 1, y, z] != 0) {
                                left = true;
                                voxels[x, y, z] |= 0x8;
                            }
                        }
                        if(x < voxels.SizeX - 1) {
                            if(voxels[x + 1, y, z] != 0) {
                                right = true;
                                voxels[x, y, z] |= 0x4;
                            }
                        }

                        if(y > 0) {
                            if(voxels[x, y - 1, z] != 0) {
                                below = true;
                                voxels[x, y, z] |= 0x20;
                            }
                        }
                        if(y < voxels.SizeY - 1) {
                            if(voxels[x, y + 1, z] != 0) {
                                above = true;
                                voxels[x, y, z] |= 0x2;
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

                        var color = new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                255
                        );

                        // Draw block
                        if(!below) {
                            if((voxels[x, y, z] & 0x20) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x20) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < voxels.SizeZ; zi++) {
                                        if((voxels[xi, y, zi] & 0x20) == 0 && IsSameColor(voxels[xi, y, zi], voxels[x, y, z])) {
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
                                        voxels[xi, y, zi] |= 0x20;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                prevMaxX = maxX;
                                prevMaxZ = maxZ;

                                int idx = vertices.Count;
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
                            if((voxels[x, y, z] & 0x2) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x2) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < voxels.SizeZ; zi++) {
                                        if((voxels[xi, y, zi] & 0x2) == 0 && IsSameColor(voxels[xi, y, zi], voxels[x, y, z])) {
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
                                        voxels[xi, y, zi] = voxels[xi, y, zi] | 0x2;
                                    }
                                }
                                maxX--;
                                maxZ--;
                                
                                int idx = vertices.Count;
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
                            if((voxels[x, y, z] & 0x10) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x10) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[xi, yi, z] & 0x10) == 0 && IsSameColor(voxels[xi, yi, z], voxels[x, y, z])) {
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
                                        voxels[xi, yi, z] |= 0x10;
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

                                int idx = vertices.Count;

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
                            if((voxels[x, y, z] & 0x1) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < voxels.SizeX; xi++) {
                                    // Check not drawn + same color
                                    if((voxels[xi, y, z] & 0x1) == 0 && IsSameColor(voxels[xi, y, z], voxels[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[xi, yi, z] & 0x1) == 0 && IsSameColor(voxels[xi, yi, z], voxels[x, y, z])) {
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
                                        voxels[xi, yi, z] |= 0x1;
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

                                int idx = vertices.Count;

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
                            if((voxels[x, y, z] & 0x8) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < voxels.SizeZ; zi++) {
                                    // Check not drawn + same color
                                    if((voxels[x, y, zi] & 0x8) == 0 && IsSameColor(voxels[x, y, zi], voxels[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[x, yi, zi] & 0x8) == 0 && IsSameColor(voxels[x, yi, zi], voxels[x, y, z])) {
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
                                        voxels[x, yi, zi] |= 0x8;
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

                                int idx = vertices.Count;

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
                            if((voxels[x, y, z] & 0x4) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < voxels.SizeZ; zi++) {
                                    // Check not drawn + same color
                                    if((voxels[x, y, zi] & 0x4) == 0 && IsSameColor(voxels[x, y, zi], voxels[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < voxels.SizeY; yi++) {
                                        if((voxels[x, yi, zi] & 0x4) == 0 && IsSameColor(voxels[x, yi, zi], voxels[x, y, z])) {
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
                                        voxels[x, yi, zi] |= 0x4;
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
                                
                                int idx = vertices.Count;

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

            if(mesh == null) {
                mesh = new Mesh();
                mesh.MarkDynamic();
            } else {
                mesh.Clear();
            }

            mesh.indexFormat = vertices.Count > MaxMeshSizeUInt16 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors32 = colors.ToArray();

            mesh.Optimize();

            mesh.RecalculateBounds();

            mesh.UploadMeshData(true);
            return mesh;
        }
    }
    
}
