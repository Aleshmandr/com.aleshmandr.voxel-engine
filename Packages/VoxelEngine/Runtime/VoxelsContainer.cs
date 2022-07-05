using UnityEngine;

namespace VoxelEngine
{
    [ExecuteAlways] [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))]
    public class VoxelsContainer : MonoBehaviour
    {
        public TextAsset Asset;
        [SerializeField] private bool loadOnStart;
        private VoxelsData data;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        public VoxelsData Data => data;

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

        [ContextMenu("RebuildMesh")]
        public void RebuildMesh() {
            MeshFilter.mesh = Utilities.GenerateMesh(data);
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
            data = Utilities.DeserializeObject<VoxelsData>(Asset.bytes);
            RebuildMesh();
        }
    }
}
