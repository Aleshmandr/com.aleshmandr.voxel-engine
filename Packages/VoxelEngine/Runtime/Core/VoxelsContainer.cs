﻿using UnityEngine;
using VoxelEngine.Jobs;

namespace VoxelEngine
{
    [ExecuteAlways] [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))]
    public class VoxelsContainer : MonoBehaviour
    {
        public TextAsset Asset;
        public NativeArray3d<int> Data;
        [SerializeField] private bool loadOnStart;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Mesh dynamicMesh;
        private MeshGenerationJobsScheduler meshGenerationJobsScheduler;

        private MeshFilter MeshFilter {
            get {
                if(meshFilter == null) {
                    meshFilter = GetComponent<MeshFilter>();
                }
                return meshFilter;
            }
        }

        private void Start() {
#if UNITY_EDITOR
            if(!Application.isPlaying) {
                OnEditorStart();
                return;
            }
#endif
            Data = NativeArray3dSerializer.Deserialize<int>(Asset.bytes);
            if(loadOnStart) {
                RebuildMesh();
                UpdateCollider();
            }
        }

        private void OnDestroy() {
            Data.Dispose();
        }

        public async void RebuildMesh() {
            meshGenerationJobsScheduler ??= new MeshGenerationJobsScheduler();
            dynamicMesh = await meshGenerationJobsScheduler.Run(Data, dynamicMesh);
            MeshFilter.mesh = dynamicMesh;
        }

        public void UpdateCollider() {
            if(meshCollider == null && !TryGetComponent(out meshCollider)) {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = MeshFilter.sharedMesh;
        }

#if UNITY_EDITOR
        private void OnEditorStart() {
            //Do not generate mesh in editor if exist to not loose link to the original mesh asset
            if(MeshFilter.sharedMesh != null || !loadOnStart) {
                return;
            }

            Data = NativeArray3dSerializer.Deserialize<int>(Asset.bytes);
            RebuildMesh();
            UpdateCollider();
            Data.Dispose();
        }
#endif
    }
}