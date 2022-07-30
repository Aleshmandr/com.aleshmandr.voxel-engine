using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
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

                                // Add triangle indices
                                triangles.Add(idx + 2);
                                triangles.Add(idx + 1);
                                triangles.Add(idx);

                                idx = vertices.Count;

                                vertices.Add(new Vector3(x + maxX, y - 1, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));

                                triangles.Add(idx + 2);
                                triangles.Add(idx + 1);
                                triangles.Add(idx);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
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

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Count;

                                vertices.Add(new Vector3(x + maxX, y, z + maxZ));
                                vertices.Add(new Vector3(x + maxX, y, z - 1));
                                vertices.Add(new Vector3(x - 1, y, z - 1));


                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
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

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Count;


                                vertices.Add(new Vector3(x + maxX, y + maxY, z - 1));
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y + maxY, z - 1));

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
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

                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Count;
                                vertices.Add(new Vector3(x - 1, y + maxY, z));
                                vertices.Add(new Vector3(x - 1, y - 1, z));
                                vertices.Add(new Vector3(x + maxX, y - 1, z));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
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

                                idx = vertices.Count;
                                vertices.Add(new Vector3(x - 1, y - 1, z - 1));
                                vertices.Add(new Vector3(x - 1, y + maxY, z + maxZ));
                                vertices.Add(new Vector3(x - 1, y + maxY, z - 1));

                                // Add triangle indeces
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);


                                for(int n = 0; n < 6; n++) {
                                    colors.Add(new Color32((byte)((voxels[x, y, z] >> 24) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 16) & 0xFF),
                                                           (byte)((voxels[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
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
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                idx = vertices.Count;
                                vertices.Add(new Vector3(x, y + maxY, z + maxZ));
                                vertices.Add(new Vector3(x, y - 1, z - 1));
                                vertices.Add(new Vector3(x, y + maxY, z - 1));

                                // Add triangle indices
                                triangles.Add(idx);
                                triangles.Add(idx + 1);
                                triangles.Add(idx + 2);

                                for(int n = 0; n < 6; n++) {
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
            MeshUtility.Optimize(mesh);
            MeshUtility.SetMeshCompression(mesh, ModelImporterMeshCompression.High);
            return mesh;
        }
    }
}
