using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VoxelEngine.Jobs;

namespace VoxelEngine
{
    public class DynamicVoxelsObject : MonoBehaviour
    {
        public NativeArray3d<int> Data;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh dynamicMesh;
        private IMeshGenerationJobScheduler meshGenerationJobsScheduler;
        private bool isDestroyed;

        public MeshRenderer MeshRenderer
        { get {
            if(meshRenderer == null) {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            return meshRenderer;
        } }

        public MeshFilter MeshFilter
        { get {
            if(meshFilter == null) {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            return meshFilter;
        } }

        private void Start() {
            Destroy(this.gameObject, 10f);
        }

        private void OnDestroy() {
            Data.Dispose();
            if(dynamicMesh != null) {
                Destroy(dynamicMesh);
            }
            isDestroyed = true;
        }

        public async UniTask RebuildMesh() {
            if(meshGenerationJobsScheduler == null) {
                if(VoxelEngineConfig.UseOptimizedMeshGenerationAtRuntime) {
                    meshGenerationJobsScheduler = new OptimizedMeshGenerationJobsScheduler();
                } else {
                    meshGenerationJobsScheduler = new MeshGenerationJobsScheduler();
                }
            }

            dynamicMesh = await meshGenerationJobsScheduler.Run(Data, dynamicMesh);
            if(isDestroyed) {
                return;
            }
            MeshFilter.mesh = dynamicMesh;

            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = MeshFilter.sharedMesh.bounds.size;
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = bc.size.x * bc.size.y * bc.size.z;
            
            rb.AddExplosionForce(5000, transform.position, 10, 0f, ForceMode.Force);
            rb.AddTorque(Random.onUnitSphere * 2000, ForceMode.Force);
        }
    }
}
