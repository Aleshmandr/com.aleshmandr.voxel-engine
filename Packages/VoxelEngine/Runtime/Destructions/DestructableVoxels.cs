using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using VoxelEngine.Destructions.Jobs;
using Random = UnityEngine.Random;

namespace VoxelEngine.Destructions
{
    public class DestructableVoxels : MonoBehaviour
    {
        public event Action<DestructableVoxels> IntegrityChanged;
        public event Action<DamageEventData<IDamageData>> Damaged;
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

        public bool MakePhysicalOnCollapse
        { get => makePhysicalOnCollapse;
          set => makePhysicalOnCollapse = value; }

        public bool IsCollapsed { get; private set; }

        public int InitialVoxelsCount { get; private set; }

        public float CollapsePercentsThresh
        { get => collapsePercentsThresh;
          set {
              collapsePercentsThresh = value;
              collapsePercentsThresh = Mathf.Clamp(collapsePercentsThresh, 0f, 100f);
              if(IsInitialized) {
                  RecalculateDestructionThresh();
              }
          } }

        public bool IsInitialized { get; private set; }

        public int VoxelsCount
        { get {
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
          } }

        private void Start() {
            InitialVoxelsCount = VoxelsCount;
            RecalculateDestructionThresh();
            IsInitialized = true;
        }

        private void RecalculateDestructionThresh() {
            destructionVoxelsCountThresh = (int)(collapsePercentsThresh * InitialVoxelsCount / 100);
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

        public async UniTask<NativeList<VoxelData>> RunDamageJob<T>(T damageData, Allocator allocator) where T : IDamageData {
            int intRad = Mathf.CeilToInt(damageData.Radius / voxelsContainer.transform.lossyScale.x);
            var localPoint = voxelsContainer.transform.InverseTransformPoint(damageData.WorldPoint);
            var localPointInt = new Vector3Int((int)localPoint.x, (int)localPoint.y, (int)localPoint.z);

            damageJobsScheduler ??= new VoxelsDamageJobsScheduler();
            var damageVoxels = await damageJobsScheduler.Run(voxelsContainer.Data, intRad, localPointInt, allocator);
            VoxelsCount -= damageVoxels.Length;
            await voxelsContainer.RebuildMesh();
            HandleDamage(damageData);

            return damageVoxels;
        }

        public void Damage<T>(T damageData, ref NativeList<VoxelData> damagedVoxels) where T : IDamageData {
            int intRad = Mathf.CeilToInt(damageData.Radius / voxelsContainer.transform.lossyScale.x);
            var localPoint = voxelsContainer.transform.InverseTransformPoint(damageData.WorldPoint);
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
            voxelsContainer.RebuildMesh().Forget();
            HandleDamage(damageData);
        }

        public async void Recover() {
            if(IsCollapsed && makePhysicalOnCollapse) {
                if(rigidbody != null) {
                    rigidbody.isKinematic = true;
                    Destroy(rigidbody);
                }
                if(destructionCollider == DestructionColliderType.Box) {
                    BoxCollider bc = this.GetComponent<BoxCollider>();
                    if(bc != null) {
                        Destroy(bc);
                    }
                    voxelsContainer.SetMeshColliderActive(true);
                }
            }

            await voxelsContainer.Reload();
            IsCollapsed = false;
            MarkDirty();
        }

        public void MarkCollapsed() {
            IsCollapsed = true;
        }

        public void MarkDirty() {
            VoxelsCount = -1;
            IntegrityChanged?.Invoke(this);
        }

        private void HandleDamage<T>(T damageData) where T : IDamageData {
            if(IsCollapsed) {
                return;
            }

            if(CheckIfNeedCollapse()) {
                Damaged?.Invoke(new DamageEventData<IDamageData>(this, damageData));
                Collapse();
                return;
            }

            Damaged?.Invoke(new DamageEventData<IDamageData>(this, damageData));
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
            switch(destructionCollider) {
                case DestructionColliderType.Box:
                    voxelsContainer.SetMeshColliderActive(false);
                    var boxCollider = gameObject.AddComponent<BoxCollider>();
                    var mesh = voxelsContainer.MeshFilter.sharedMesh;
                    boxCollider.center = mesh.bounds.center;
                    boxCollider.size = mesh.bounds.size;
                    break;
                default:
                    if(voxelsContainer.MeshCollider != null) {
                        voxelsContainer.MeshCollider.convex = true;
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
