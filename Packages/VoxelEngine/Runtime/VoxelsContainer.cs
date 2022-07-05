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

        [ContextMenu("LoadAsset")]
        private void LoadAsset() {
            data = Utilities.DeserializeObject<VoxelsData>(Asset.bytes);
            MeshFilter.mesh = Utilities.GenerateMesh(data);
        }
    }
}
