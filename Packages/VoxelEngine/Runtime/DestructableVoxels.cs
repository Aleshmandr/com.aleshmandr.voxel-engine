using Unity.Collections;
using UnityEngine;

namespace VoxelEngine
{
    public class DestructableVoxels : MonoBehaviour
    {
        [SerializeField] private VoxelsContainer voxelsContainer;

        public void Damage(Vector3 worldPoint, float radius, ref NativeList<VoxelData> damagedVoxels) {
            int intRad = Mathf.CeilToInt(radius / voxelsContainer.transform.lossyScale.x);
            var localPoint = voxelsContainer.transform.InverseTransformPoint(worldPoint);
            var localPointInt = new Vector3Int((int)localPoint.x, (int)localPoint.y, (int)localPoint.z);
            for(int i = -intRad; i <= intRad; i++) {
                for(int j = -intRad; j <= intRad; j++) {
                    for(int k = -intRad; k <= intRad; k++) {
                        if(i * i + j * j + k * k <= intRad * intRad) {
                            int x = i + localPointInt.x;
                            int y = j + localPointInt.y;
                            int z = k + localPointInt.z;
                            if(x >= 0 && x < voxelsContainer.Data.SizeX && y >= 0 && voxelsContainer.Data.SizeY > y && z >= 0 && voxelsContainer.Data.SizeZ > z) {
                                if(voxelsContainer.Data[x, y, z] != 0) {
                                    damagedVoxels.Add(new VoxelData() {
                                        Position = new Vector3(x, y, z),
                                        Color = Utilities.VoxelColor(voxelsContainer.Data[x, y, z])
                                    });
                                    voxelsContainer.Data[x, y, z] = 0;
                                }
                            }
                        }
                    }
                }
            }
            voxelsContainer.RebuildMesh();
            voxelsContainer.UpdateCollider();
        }

        private void Reset() {
            if(voxelsContainer == null) {
                voxelsContainer = GetComponent<VoxelsContainer>();
            }
        }
    }
}
