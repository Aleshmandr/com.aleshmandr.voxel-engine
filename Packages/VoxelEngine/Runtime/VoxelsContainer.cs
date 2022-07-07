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
        private Mesh dynamicMesh;

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

        public void RebuildMesh() {
            dynamicMesh = Utilities.GenerateMesh(Data, dynamicMesh);
            MeshFilter.mesh = dynamicMesh;
        }

        public void UpdateCollider() {
            if(meshCollider == null && !TryGetComponent(out meshCollider)) {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = MeshFilter.sharedMesh;
        }

        private void LoadAsset() {
            Data.Dispose();
            Data = NativeArray3dSerializer.Deserialize<int>(Asset.bytes);
            RebuildMesh();
            UpdateCollider();
#if UNITY_EDITOR
            if(!Application.isPlaying) {
                Data.Dispose();
            }
#endif
        }
    }
}
