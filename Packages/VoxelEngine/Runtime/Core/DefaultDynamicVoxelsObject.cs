﻿using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VoxelEngine.Destructions;
using VoxelEngine.Jobs;

namespace VoxelEngine
{
    public class DefaultDynamicVoxelsObject : MonoBehaviour, IVoxelsFractureObject
    {
        private NativeArray3d<int> data;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Rigidbody rigidBody;
        private BoxCollider boxCollider;
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

        private MeshFilter MeshFilter
        { get {
            if(meshFilter == null) {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            return meshFilter;
        } }

        private BoxCollider Collider
        { get {
            if(boxCollider == null) {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }
            return boxCollider;
        } }
        
        public Rigidbody Rigidbody
        { get {
            if(rigidBody == null) {
                rigidBody = gameObject.AddComponent<Rigidbody>();
            }
            return rigidBody;
        } }

        private void Start() {
            Destroy(this.gameObject, 10f);
        }

        private void OnDestroy() {
            data.Dispose();
            if(dynamicMesh != null) {
                Destroy(dynamicMesh);
            }
            isDestroyed = true;
        }
        
        public void Init(NativeArray3d<int> voxelsData) {
            
            InitAsync(voxelsData).Forget();
        }

        public async UniTask InitAsync(NativeArray3d<int> voxelsData) {
            if(data.IsCreated) {
                data.Dispose();
            }
            data = voxelsData;
            if(meshGenerationJobsScheduler == null) {
                if(VoxelEngineConfig.UseOptimizedMeshGenerationAtRuntime) {
                    meshGenerationJobsScheduler = new OptimizedMeshGenerationJobsScheduler();
                } else {
                    meshGenerationJobsScheduler = new MeshGenerationJobsScheduler();
                }
            }

            dynamicMesh = await meshGenerationJobsScheduler.Run(data, CancellationToken.None, dynamicMesh);
            if(isDestroyed) {
                return;
            }
            MeshFilter.mesh = dynamicMesh;
            Collider.size = MeshFilter.sharedMesh.bounds.size;
            Rigidbody.mass = 1f;
        }
    }
}