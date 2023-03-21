using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public static class VoxelsFractureEngine
    {
        public static IFractureFactory FractureFactory = new DefaultFracturFactory();
    }

    public class DefaultFracturFactory : IFractureFactory
    {
        public IVoxelsFractureObject Create(VoxelsFractureObject fractureObject, NativeArray3d<int> data, Vector3 worldPos, IForceDamageData damageData) {
            Transform parentTransform = fractureObject.transform;
            GameObject cluster = new GameObject {
                transform = {
                    parent = parentTransform.parent
                }
            };

            var dynamicVoxelsObject = cluster.AddComponent<DefaultDynamicVoxelsObject>();
            TestAsync(dynamicVoxelsObject, data, damageData).Forget();
            dynamicVoxelsObject.transform.position = worldPos;
            dynamicVoxelsObject.transform.rotation = parentTransform.rotation;
            dynamicVoxelsObject.transform.localScale = parentTransform.localScale;
            dynamicVoxelsObject.gameObject.layer = VoxelEngineConfig.FractureUseParentLayer ? fractureObject.gameObject.layer : VoxelEngineConfig.FracturesCustomLayer;
            dynamicVoxelsObject.MeshRenderer.sharedMaterial = fractureObject.VoxelsContainer.MeshRenderer.sharedMaterial;

            return dynamicVoxelsObject;
        }

        private async UniTaskVoid TestAsync(DefaultDynamicVoxelsObject dynamicVoxelsObject, NativeArray3d<int> data, IForceDamageData damageData) {
            await dynamicVoxelsObject.InitAsync(data);
            dynamicVoxelsObject.Rigidbody.AddExplosionForce(damageData.Force.magnitude, damageData.WorldPoint, damageData.Radius);
        }
    }

    public interface IVoxelsFractureObject
    {
        public void Init(NativeArray3d<int> voxelsData);
    }

    public interface IFractureFactory
    {
        public IVoxelsFractureObject Create(VoxelsFractureObject fractureObject, NativeArray3d<int> data, Vector3 worldPos, IForceDamageData damageData);
    }
}
