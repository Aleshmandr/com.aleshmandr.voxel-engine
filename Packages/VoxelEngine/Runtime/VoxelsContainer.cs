using UnityEngine;

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

        private MeshFilter MeshFilter {
            get {
                if(meshFilter == null) {
                    meshFilter = GetComponent<MeshFilter>();
                }
                return meshFilter;
            }
        }

        private void Start() {
            if(loadOnStart) {
                LoadAsset();
            }
        }

        private void OnDestroy() {
            Data.Dispose();
        }

        [ContextMenu("RebuildMesh")]
        public void RebuildMesh() {
            MeshFilter.mesh = Utilities.GenerateMesh(Data);
        }

        [ContextMenu("UpdateCollider")]
        public void UpdateCollider() {
            if(meshCollider == null && !TryGetComponent(out meshCollider)) {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = MeshFilter.mesh;
        }
        
        [ContextMenu("LoadAsset")]
        private void LoadAsset() {
            Data.Dispose();
            Data = NativeArray3dSerializer.Deserialize<int>(Asset.bytes);
            RebuildMesh();
        }
    }
}
