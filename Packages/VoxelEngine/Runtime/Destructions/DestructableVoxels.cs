using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using VoxelEngine.Destructions.Jobs;
using Random = UnityEngine.Random;

namespace VoxelEngine.Destructions
{
    public class DestructableVoxels : MonoBehaviour
    {
        public event Action<DestructableVoxels> IntegrityChanged;
        [SerializeField] private VoxelsContainer voxelsContainer;
        [SerializeField] private bool makePhysicalOnCollapse;
        [SerializeField] private float collapsePercentsThresh = 50f;
        [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
        [SerializeField] private DestructionColliderType destructionCollider = DestructionColliderType.Box;
        private int destructionVoxelsCountThresh;
        private new Rigidbody rigidbody;
        private int voxelsCount = -1;
        private VoxelsDamageJobsScheduler damageJobsScheduler;
        private static readonly Vector2 CollapseTorqueMinMax = new Vector2(10, 200);

        public VoxelsContainer VoxelsContainer => voxelsContainer;

        public bool IsCollapsed { get; private set; }

        public int InitialVoxelsCount { get; private set; }

        public bool IsInitialized { get; private set; }
        
        public int VoxelsCount {
            get {
                if(voxelsCount < 0) {
                    voxelsCount = 0;
                    for(int i = 0; i < voxelsContainer.Data.NativeArray.Length; i++) {
                        if(voxelsContainer.Data.NativeArray[i] == 0) {
                            continue;
                        }
                        voxelsCount++;
                    }
                }
                return voxelsCount;
            }
            private set {
                voxelsCount = value;
            }
        }

        private void Start() {
            InitialVoxelsCount = VoxelsCount;
            destructionVoxelsCountThresh = (int)(collapsePercentsThresh * InitialVoxelsCount / 100);
            IsInitialized = true;
        }

        [ContextMenu("Collapse")]
        public void Collapse() {
            if(IsCollapsed) {
                return;
            }
            if(makePhysicalOnCollapse) {
                MakePhysical();
            }
            IsCollapsed = true;
            IntegrityChanged?.Invoke(this);
        }

        public async Task<NativeList<VoxelData>> RunDamageJob(Vector3 worldPoint, float radius, Allocator allocator) {
            int intRad = Mathf.CeilToInt(radius / voxelsContainer.transform.lossyScale.x);
            var localPoint = voxelsContainer.transform.InverseTransformPoint(worldPoint);
            var localPointInt = new Vector3Int((int)localPoint.x, (int)localPoint.y, (int)localPoint.z);

            damageJobsScheduler ??= new VoxelsDamageJobsScheduler();
            var damageVoxels = await damageJobsScheduler.Run(voxelsContainer.Data, intRad, localPointInt, allocator);
            VoxelsCount -= damageVoxels.Length;

            voxelsContainer.RebuildMesh(true);
            HandleVoxelsRemove();

            return damageVoxels;
        }

        public void Damage(Vector3 worldPoint, float radius, ref NativeList<VoxelData> damagedVoxels) {
            int intRad = Mathf.CeilToInt(radius / voxelsContainer.transform.lossyScale.x);
            var localPoint = voxelsContainer.transform.InverseTransformPoint(worldPoint);
            var localPointInt = new Vector3Int((int)localPoint.x, (int)localPoint.y, (int)localPoint.z);
            for(int i = -intRad; i <= intRad; i++) {
                for(int j = -intRad; j <= intRad; j++) {
                    for(int k = -intRad; k <= intRad; k++) {
                        if(i * i + j * j + k * k <= intRad * intRad) {
                            int x = i + localPointInt.x;
                            int y = j + localPointInt.y;
                            int z = k + localPointInt.z;
                            if(x >= 0 && x < voxelsContainer.Data.SizeX && y >= 0 && voxelsContainer.Data.SizeY > y && z >= 0 && voxelsContainer.Data.SizeZ > z) {
                                if(voxelsContainer.Data[x, y, z] != 0) {
                                    damagedVoxels.Add(new VoxelData {
                                        Position = new Vector3(x, y, z), Color = Utilities.VoxelColor(voxelsContainer.Data[x, y, z])
                                    });
                                    voxelsContainer.Data[x, y, z] = 0;
                                    VoxelsCount--;
                                }
                            }
                        }
                    }
                }
            }
            voxelsContainer.RebuildMesh(true);
            HandleVoxelsRemove();
        }

        public async void Recover() {
            await voxelsContainer.Reload();
            MarkDirty();
        }

        public void MarkCollapsed() {
            IsCollapsed = true;
        }

        public void MarkDirty() {
            VoxelsCount = -1;
            IntegrityChanged?.Invoke(this);
        }

        private void HandleVoxelsRemove() {
            if(IsCollapsed) {
                return;
            }

            if(CheckIfNeedCollapse()) {
                Collapse();
                return;
            }

            IntegrityChanged?.Invoke(this);
        }

        private bool CheckIfNeedCollapse() {
            int destroyedVoxelsCount = InitialVoxelsCount - VoxelsCount;
            return destroyedVoxelsCount >= destructionVoxelsCountThresh;
        }

        private void MakePhysical() {
            UnRoot();
            GenerateDestructionCollider();
          
            if(rigidbody == null) {
                if(!TryGetComponent(out rigidbody)) {
                    rigidbody = gameObject.AddComponent<Rigidbody>();
                    rigidbody.interpolation = interpolation;
                    rigidbody.solverIterations = Constants.DestructableSolverIterations;
                    var torqueForce = Random.Range(CollapseTorqueMinMax.x, CollapseTorqueMinMax.y);
                    rigidbody.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Acceleration);
                }
            }

            rigidbody.mass = VoxelsCount * Constants.VoxelWeight;
            rigidbody.WakeUp();
        }

        private void UnRoot() {
            var root = transform.GetComponentInParent<DestructableVoxelsRoot>();
            if(root != null) {
                transform.SetParent(root.transform.parent);
            }
        }

        private void GenerateDestructionCollider() {
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            switch(destructionCollider) {
                case DestructionColliderType.Box:
                    if(meshCollider != null) {
                        Destroy(meshCollider);
                    }
                    var boxCollider = gameObject.AddComponent<BoxCollider>();
                    var mesh = voxelsContainer.MeshFilter.sharedMesh;
                    boxCollider.center = mesh.bounds.center;
                    boxCollider.size = mesh.bounds.size;
                    break;
                default:
                    if(meshCollider != null) {
                        meshCollider.convex = true;
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        private void Reset() {
            if(voxelsContainer == null) {
                voxelsContainer = GetComponent<VoxelsContainer>();
            }
        }
#endif
    }
}
