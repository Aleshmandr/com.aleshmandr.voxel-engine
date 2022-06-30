using UnityEngine;

namespace VoxelEngine
{
    [ExecuteAlways][RequireComponent(typeof(MeshFilter))][RequireComponent(typeof(MeshRenderer))]
    public class VoxelsContainer : MonoBehaviour
    {
        [SerializeField] private TextAsset asset;
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
            Initialize();
        }

        [ContextMenu("Initialize")]
        private void Initialize() {
            data = Utilities.UnzipObject<VoxelsData>(asset.bytes);
            MeshFilter.mesh = Utilities.GenerateMesh(data);
        }
    }
}
