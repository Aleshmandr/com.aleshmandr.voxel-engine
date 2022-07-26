using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public class RawVoxelsData
    {
        public Vector3Int Pivot;
        public readonly List<RawVoxelData> Voxels;

        public Vector3Int Size {
            get {
                int maxX = 0;
                int maxZ = 0;
                int maxY = 0;
                for(int i = 0; i < Voxels.Count; i++) {
                    if(Voxels[i].X > maxX) {
                        maxX = Voxels[i].X;
                    }
                    if(Voxels[i].Y > maxY) {
                        maxY = Voxels[i].Y;
                    }
                    if(Voxels[i].Z > maxZ) {
                        maxZ = Voxels[i].Z;
                    }
                }
                return new Vector3Int(maxX + 1, maxY + 1, maxZ + 1);
            }
        }

        public RawVoxelsData() {
            Voxels = new List<RawVoxelData>();
        }

        public RawVoxelsData(RawVoxelData[] voxels) {
            Voxels = new List<RawVoxelData>(voxels);
        }
    }
}
