using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public class Utilities
    {
        public static bool SameColor(int block1, int block2) {
            return ((block1 >> 8) & 0xFFFFFF) == ((block2 >> 8) & 0xFFFFFF) && block1 != 0 && block2 != 0;
        }

        public static Mesh GenerateMesh(VoxelsContainer container) {
            List<Vector3> vertices = new List<Vector3>();
            List<Color32> colors = new List<Color32>();
            List<int> triangles = new List<int>();
            int[,,] blocks = container.Blocks;

            // Block structure
            // BLOCK: [R-color][G-color][B-color][0][00][back_left_right_above_front]
            //           8bit    8bit     8it    1bit(below-face)  2bit(floodfill)     5bit(faces)

            // Reset faces
            for(int y = container.fromY; y < container.toY; y++) {
                for(int x = container.fromX; x < container.toX; x++) {
                    for(int z = container.fromZ; z < container.toZ; z++) {
                        if(blocks[x, y, z] != 0) {
                            blocks[x, y, z] &= ~(1 << 0);
                            blocks[x, y, z] &= ~(1 << 1);
                            blocks[x, y, z] &= ~(1 << 2);
                            blocks[x, y, z] &= ~(1 << 3);
                            blocks[x, y, z] &= ~(1 << 4);
                            blocks[x, y, z] &= ~(1 << 7);
                        }
                    }
                }
            }

            for(int y = container.fromY; y < container.toY; y++) {
                for(int x = container.fromX; x < container.toX; x++) {
                    for(int z = container.fromZ; z < container.toZ; z++) {
                        if((blocks[x, y, z] >> 8) == 0) {
                            continue; // Skip empty blocks
                        }

                        // Check if hidden
                        int left = 0, right = 0, above = 0, front = 0, back = 0, below = 0;
                        if(z > 0) {
                            if(blocks[x, y, z - 1] != 0) {
                                back = 1;
                                blocks[x, y, z] |= 0x10;
                            }
                        }
                        if(x > 0) {
                            if(blocks[x - 1, y, z] != 0) {
                                left = 1;
                                blocks[x, y, z] |= 0x8;
                            }
                        }

                        if(y > container.toY) {
                            if(blocks[x, y - 1, z] != 0) {
                                below = 1;
                                //blocks[x, y - 1, z] = blocks[x, y, z] | 0x80;
                                blocks[x, y, z] |= 0x80;
                            }
                        }
                        if(x < container.toX - 1) {
                            if(blocks[x + 1, y, z] != 0) {
                                right = 1;
                                blocks[x, y, z] |= 0x4;
                            }
                        }

                        if(y < container.toY - 1) {
                            if(blocks[x, y + 1, z] != 0) {
                                above = 1;
                                blocks[x, y, z] |= 0x2;
                            }
                        }

                        if(z < container.toZ - 1) {
                            if(blocks[x, y, z + 1] != 0) {
                                front = 1;
                                blocks[x, y, z] |= 0x1;
                            }
                        }

                        if(front == 1 && left == 1 && right == 1 && above == 1 && back == 1 && below == 1) {
                            // If we are building a standalone mesh, remove invisible
                            blocks[x, y, z] = 0;
                            continue; // Block is hidden
                        }

                        // Draw block
                        if(below == 0) {
                            if((blocks[x, y, z] & 0x80) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < container.toX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x80) == 0 && SameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < container.toZ; zi++) {
                                        if((blocks[xi, y, zi] & 0x80) == 0 && SameColor(blocks[xi, y, zi], blocks[x, y, z])) {
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
                                        blocks[xi, y, zi] |= 0x80;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }

                        if(above == 0) {
                            // Get above (0010)
                            if((blocks[x, y, z] & 0x2) == 0) {
                                int maxX = 0;
                                int maxZ = 0;

                                for(int xi = x; xi < container.toX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x2) == 0 && SameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpZ = 0;
                                    for(int zi = z; zi < container.toZ; zi++) {
                                        if((blocks[xi, y, zi] & 0x2) == 0 && SameColor(blocks[xi, y, zi], blocks[x, y, z])) {
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
                                        blocks[xi, y, zi] = blocks[xi, y, zi] | 0x2;
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }
                        if(back == 0) {
                            // back  10000
                            if((blocks[x, y, z] & 0x10) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < container.toX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x10) == 0 && SameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < container.toY; yi++) {
                                        if((blocks[xi, yi, z] & 0x10) == 0 && SameColor(blocks[xi, yi, z], blocks[x, y, z])) {
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }
                        if(front == 0) {
                            // front 0001
                            if((blocks[x, y, z] & 0x1) == 0) {
                                int maxX = 0;
                                int maxY = 0;

                                for(int xi = x; xi < container.toX; xi++) {
                                    // Check not drawn + same color
                                    if((blocks[xi, y, z] & 0x1) == 0 && SameColor(blocks[xi, y, z], blocks[x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < container.toY; yi++) {
                                        if((blocks[xi, yi, z] & 0x1) == 0 && SameColor(blocks[xi, yi, z], blocks[x, y, z])) {
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

                                // Add triangle indeces
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
                        if(left == 0) {
                            if((blocks[x, y, z] & 0x8) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < container.toZ; zi++) {
                                    // Check not drawn + same color
                                    if((blocks[x, y, zi] & 0x8) == 0 && SameColor(blocks[x, y, zi], blocks[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < container.toY; yi++) {
                                        if((blocks[x, yi, zi] & 0x8) == 0 && SameColor(blocks[x, yi, zi], blocks[x, y, z])) {
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
                                    colors.Add(new Color32((byte)((blocks[x, y, z] >> 24) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 16) & 0xFF),
                                                           (byte)((blocks[x, y, z] >> 8) & 0xFF),
                                                           (byte)255
                                               ));
                                }
                            }
                        }
                        if(right == 0) {
                            if((blocks[x, y, z] & 0x4) == 0) {
                                int maxZ = 0;
                                int maxY = 0;

                                for(int zi = z; zi < container.toZ; zi++) {
                                    // Check not drawn + same color
                                    if((blocks[x, y, zi] & 0x4) == 0 && SameColor(blocks[x, y, zi], blocks[x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    int tmpY = 0;
                                    for(int yi = y; yi < container.toY; yi++) {
                                        if((blocks[x, yi, zi] & 0x4) == 0 && SameColor(blocks[x, yi, zi], blocks[x, y, z])) {
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

            
            
            var mesh = new Mesh {
                indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                colors32 = colors.ToArray(),
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
