using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VoxelEngine.Jobs;

namespace VoxelEngine
{
    [ExecuteInEditMode] [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))]
    public class VoxelsContainer : MonoBehaviour
    {
        public TextAsset Asset;
        public NativeArray3d<int> Data;
        [SerializeField] private bool loadOnStart;
        [SerializeField] private bool updateMeshFilterOnStart;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh dynamicMesh;
        private MeshGenerationJobsScheduler meshGenerationJobsScheduler;
        private bool isDestroyed;
        private CancellationTokenSource colliderUpdateCts;
        private const float ColliderUpdateCooldown = 0.1f;//TODO: Move to global config

        public MeshRenderer MeshRenderer
        { get {
            if(meshRenderer == null) {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            return meshRenderer;
        } }
        
        private MeshFilter MeshFilter
        { get {
            if(meshFilter == null) {
                meshFilter = GetComponent<MeshFilter>();
            }
            return meshFilter;
        } }
        
        public bool IsInitialized { get; private set; }

        private async void Start() {
#if UNITY_EDITOR
            if(Application.isPlaying) {
                if(IsInitialized)
                {
                    return;
                }
            } else {
                OnEditorStart();
                return;
            }
#endif
            await Deserialize(Asset.bytes, loadOnStart, updateMeshFilterOnStart);
            IsInitialized = true;
        }

        private void OnDestroy() {
            Data.Dispose();
            isDestroyed = true;
            colliderUpdateCts?.Cancel();
        }

        public async Task Reload() {
            await Deserialize(Asset.bytes, true, true);
        }

        public async Task RebuildMesh(bool updateMeshFilter, bool forceUpdateCollider = false) {
            meshGenerationJobsScheduler ??= new MeshGenerationJobsScheduler();
            dynamicMesh = await meshGenerationJobsScheduler.Run(Data, dynamicMesh);
            if(isDestroyed) {
                return;
            }
            if(updateMeshFilter) {
                MeshFilter.mesh = dynamicMesh;
            }
            if(meshCollider == null && !TryGetComponent(out meshCollider)) {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            
            if(forceUpdateCollider) {
                colliderUpdateCts?.Cancel();
                meshCollider.sharedMesh = dynamicMesh;
            } else if(colliderUpdateCts == null){
                colliderUpdateCts = new CancellationTokenSource();
                await UpdateColliderAsync(colliderUpdateCts.Token);
            }
        }

        private async Task UpdateColliderAsync(CancellationToken cancellationToken) {
            var updateEndTime = Time.unscaledTime + ColliderUpdateCooldown;
            while(Time.unscaledTime < updateEndTime) {
                await Task.Yield();
                if(cancellationToken.IsCancellationRequested) {
                    return;
                }
            }
            meshCollider.sharedMesh = MeshFilter.sharedMesh;
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

        public async Task Deserialize(byte[] bytes, bool rebuildMesh, bool updateMeshFilter) {
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
        private async void OnEditorStart() {
            //Do not generate mesh in editor if exist to not loose link to the original mesh asset
            if(MeshFilter.sharedMesh != null || !loadOnStart) {
                return;
            }

            Data = NativeArray3dSerializer.Deserialize<int>(Asset.bytes);
            await RebuildMesh(true, true);
            Data.Dispose();
        }
#endif
    }
}
