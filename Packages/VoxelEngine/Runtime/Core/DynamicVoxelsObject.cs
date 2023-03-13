using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelEngine.Jobs;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelEngine
{
    [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))]
    public class DynamicVoxelsObject : MonoBehaviour
    {
        public NativeArray3d<int> Data;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh dynamicMesh;
        private IMeshGenerationJobScheduler meshGenerationJobsScheduler;
        private bool isDestroyed;
        private JobHandle bakeMeshJobHandle;

        public MeshRenderer MeshRenderer
        { get {
            if(meshRenderer == null) {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            return meshRenderer;
        } }

        public MeshFilter MeshFilter
        { get {
            if(meshFilter == null) {
                meshFilter = GetComponent<MeshFilter>();
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
        }
    }
}
