using Cysharp.Threading.Tasks;
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
    [ExecuteInEditMode] [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))]
    public class VoxelsContainer : MonoBehaviour
    {
        public TextAsset Asset;
        public NativeArray3d<int> Data;
        [SerializeField] private bool loadOnStart;
        [SerializeField] private bool updateMeshFilterOnStart;
        [SerializeField] private bool useBakeJob;
        [SerializeField] private bool isColliderDisabled;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh dynamicMesh;
        private IMeshGenerationJobScheduler meshGenerationJobsScheduler;
        private bool isDestroyed;
        private CancellationTokenSource colliderUpdateCts;
        private CancellationTokenSource lifeTimeCts;
        private const float ColliderUpdateCooldown = 0.2f; //TODO: Move to global config
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

        public MeshCollider MeshCollider => meshCollider;

        public bool IsInitialized { get; private set; }

        private async void Start() {
#if UNITY_EDITOR
            if(Application.isPlaying) {
                if(IsInitialized) {
                    return;
                }
            } else {
                OnEditorStart();
                return;
            }
#endif
            lifeTimeCts = new CancellationTokenSource();
            await Deserialize(Asset.bytes, loadOnStart, updateMeshFilterOnStart);
            IsInitialized = true;
        }

        private void OnDestroy() {
            Data.Dispose();
            if(dynamicMesh != null) {
#if UNITY_EDITOR
                if(Application.isPlaying) {
                    Destroy(dynamicMesh);
                } else {
                    DestroyImmediate(dynamicMesh);
                }
#else
                Destroy(dynamicMesh);
#endif
            }
            isDestroyed = true;
            colliderUpdateCts?.Cancel(false);
            colliderUpdateCts?.Dispose();
            lifeTimeCts?.Cancel(false);
            lifeTimeCts?.Dispose();
        }

        public async UniTask Reload() {
            await Deserialize(Asset.bytes, true, true);
        }

        public void SetMeshColliderActive(bool active) {
            isColliderDisabled = !active;
            if(meshCollider != null) {
                meshCollider.enabled = active;
                if(isColliderDisabled) {
                    meshCollider.sharedMesh = null;
                } else {
                    UpdateCollider(true).Forget();
                }
            }
        }

        public async UniTask RebuildMesh(bool updateMeshFilter, bool forceUpdateCollider = false) {
            if(meshGenerationJobsScheduler == null) {
                if(VoxelEngineConfig.UseOptimizedMeshGenerationAtRuntime) {
                    meshGenerationJobsScheduler = new OptimizedMeshGenerationJobsScheduler();
                } else {
                    meshGenerationJobsScheduler = new MeshGenerationJobsScheduler();
                }
            }

            dynamicMesh = await meshGenerationJobsScheduler.Run(Data, lifeTimeCts.Token, dynamicMesh);
            if(isDestroyed || lifeTimeCts.IsCancellationRequested) {
                return;
            }
            if(updateMeshFilter) {
                MeshFilter.mesh = dynamicMesh;
            }

            if(isColliderDisabled) {
                return;
            }

            if(meshCollider == null && !TryGetComponent(out meshCollider)) {
#if UNITY_EDITOR
                if(Application.isPlaying) {
                    return;
                }
                meshCollider = gameObject.AddComponent<MeshCollider>();
#else
                return;
#endif
            }

            await UpdateCollider(forceUpdateCollider);
        }

        private async UniTask UpdateCollider(bool forceUpdateCollider = false) {
            if(forceUpdateCollider) {
                colliderUpdateCts?.Cancel(false);
                colliderUpdateCts?.Dispose();
                colliderUpdateCts = null;

                if(dynamicMesh.vertexCount > 0) {

                    if(useBakeJob) {
                        var meshIds = new NativeArray<int>(1, Allocator.TempJob);
                        meshIds[0] = dynamicMesh.GetInstanceID();
                        bakeMeshJobHandle = new BakeMeshJob {
                            MeshIds = meshIds, Convex = meshCollider.convex
                        }.Schedule(1, 1, bakeMeshJobHandle);
                        bakeMeshJobHandle.Complete();
                    }

                    meshCollider.sharedMesh = dynamicMesh;
                    meshCollider.enabled = true;
                } else {
                    meshCollider.enabled = false;
                }
            } else if(colliderUpdateCts == null) {
                colliderUpdateCts = new CancellationTokenSource();
                await UpdateColliderAsync(colliderUpdateCts.Token);
            }
        }

        private async UniTask UpdateColliderAsync(CancellationToken cancellationToken) {
            var updateEndTime = Time.unscaledTime + ColliderUpdateCooldown;
            while(Time.unscaledTime < updateEndTime) {
                await UniTask.Yield();
                if(cancellationToken.IsCancellationRequested) {
                    return;
                }
            }
            
            if(isColliderDisabled) {
                return;
            }

            if(MeshFilter.sharedMesh.vertexCount > 0) {
                if(useBakeJob) {
                    var meshIds = new NativeArray<int>(1, Allocator.TempJob);
                    meshIds[0] = MeshFilter.sharedMesh.GetInstanceID();
                    bakeMeshJobHandle = new BakeMeshJob {
                        MeshIds = meshIds, Convex = meshCollider.convex
                    }.Schedule(1, 1, bakeMeshJobHandle);

                    while(!bakeMeshJobHandle.IsCompleted) {
                        await UniTask.Yield();
                    }

                    if(cancellationToken.IsCancellationRequested) {
                        return;
                    }
                }

                if(isColliderDisabled) {
                    return;
                }

                if(meshCollider != null) {
                    meshCollider.sharedMesh = MeshFilter.sharedMesh;
                    meshCollider.enabled = true;
                }
            } else {
                if(meshCollider != null) {
                    meshCollider.enabled = false;
                }
            }

            colliderUpdateCts?.Cancel(false);
            colliderUpdateCts?.Dispose();
            colliderUpdateCts = null;
        }

        public bool IsVoxelInner(int x, int y, int z) {
            return Data.IsCoordsValid(x, y, z)
                && Data.IsCoordsValid(x + 1, y, z) && Data[x + 1, y, z] != 0
                && Data.IsCoordsValid(x - 1, y, z) && Data[x - 1, y, z] != 0
                && Data.IsCoordsValid(x, y + 1, z) && Data[x, y + 1, z] != 0
                && Data.IsCoordsValid(x, y - 1, z) && Data[x, y - 1, z] != 0
                && Data.IsCoordsValid(x, y, z + 1) && Data[x, y, z + 1] != 0
                && Data.IsCoordsValid(x, y, z - 1) && Data[x, y, z - 1] != 0;
        }

        public async UniTask Deserialize(byte[] bytes, bool rebuildMesh, bool updateMeshFilter) {
            if(bytes == null) {
                return;
            }
            Data.Dispose();
            Data = NativeArray3dSerializer.Deserialize<int>(bytes);
            if(rebuildMesh) {
                await RebuildMesh(updateMeshFilter, true);
            }
        }

        public byte[] Serialize() {
            return NativeArray3dSerializer.Serialize(Data, true);
        }

#if UNITY_EDITOR

        public void EditorEnableLoadOnStart() {
            loadOnStart = true;
            updateMeshFilterOnStart = true;
            EditorUtility.SetDirty(this);
        }

        public void EditorRefresh() {
            EditorRefreshAsync().Forget();
        }

        private void OnEditorStart() {
            lifeTimeCts = new CancellationTokenSource();
            //Do not generate mesh in editor if exist to not loose link to the original mesh asset
            if(MeshFilter.sharedMesh != null || !loadOnStart) {
                return;
            }

            EditorRefreshAsync().Forget();
        }

        private async UniTaskVoid EditorRefreshAsync() {
            Data = NativeArray3dSerializer.Deserialize<int>(Asset.bytes);
            await RebuildMesh(true, true);
            Data.Dispose();
        }
#endif
    }
}
