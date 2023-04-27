
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public static class VoxelsFractureEngine
    {
        public static IFractureFactory FractureFactory = new DefaultFractureFactory();
    }

    public class DefaultFractureFactory : IFractureFactory
    {
        public IVoxelsFractureObject Create(VoxelsFractureObject fractureObject, NativeArray3d<int> data, int voxelsCount, Vector3 worldPos, IForceDamageData damageData) {
            Transform parentTransform = fractureObject.transform;
            GameObject cluster = new GameObject {
                transform = {
                    parent = parentTransform.parent
                }
            };

            var dynamicVoxelsObject = cluster.AddComponent<DefaultDynamicVoxelsObject>();
            InitAsync(dynamicVoxelsObject, data, voxelsCount, damageData).Forget();
            dynamicVoxelsObject.transform.position = worldPos;
            dynamicVoxelsObject.transform.rotation = parentTransform.rotation;
            dynamicVoxelsObject.transform.localScale = parentTransform.localScale;
            dynamicVoxelsObject.gameObject.layer = fractureObject.gameObject.layer;
            dynamicVoxelsObject.MeshRenderer.sharedMaterial = fractureObject.VoxelsContainer.MeshRenderer.sharedMaterial;

            return dynamicVoxelsObject;
        }

        private async UniTaskVoid InitAsync(DefaultDynamicVoxelsObject dynamicVoxelsObject, NativeArray3d<int> data, int voxelsCount, IForceDamageData damageData) {
            await dynamicVoxelsObject.InitAsync(data);
            dynamicVoxelsObject.Rigidbody.AddExplosionForce(damageData.Force.magnitude, damageData.WorldPoint, damageData.Radius);
            dynamicVoxelsObject.Rigidbody.mass = voxelsCount;
        }
    }

    public interface IVoxelsFractureObject
    {
        public void Init(NativeArray3d<int> voxelsData);
    }

    public interface IFractureFactory
    {
        public IVoxelsFractureObject Create(VoxelsFractureObject fractureObject, NativeArray3d<int> data, int voxelsCount, Vector3 worldPos, IForceDamageData damageData);
    }
}
