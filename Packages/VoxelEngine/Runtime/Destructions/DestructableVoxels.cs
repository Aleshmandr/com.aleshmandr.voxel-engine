﻿using System;
using Unity.Collections;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public class DestructableVoxels : MonoBehaviour
    {
        public event Action<DestructableVoxels> IntegrityChanged;
        [SerializeField] private VoxelsContainer voxelsContainer;
        [SerializeField] private bool makePhysicalOnCollapse;
        [SerializeField] private float collapsePercentsThresh = 50f;
        private int destructionVoxelsCountThresh;
        private new Rigidbody rigidbody;
        private int voxelsCount = -1;
        private int initialVoxelsCount;

        public VoxelsContainer VoxelsContainer => voxelsContainer;
        
        public bool IsCollapsed { get; private set; }

        public int VoxelsCount
        {
            get {
                if(voxelsCount < 0) {
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
            initialVoxelsCount = VoxelsCount;
            destructionVoxelsCountThresh = (int)(collapsePercentsThresh * initialVoxelsCount / 100);
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
                                        Position = new Vector3(x, y, z),
                                        Color = Utilities.VoxelColor(voxelsContainer.Data[x, y, z])
                                    });
                                    voxelsContainer.Data[x, y, z] = 0;
                                    VoxelsCount--;
                                }
                            }
                        }
                    }
                }
            }
            voxelsContainer.RebuildMesh();
            voxelsContainer.UpdateCollider();
            HandleVoxelsRemove();
        }
        
        private void HandleVoxelsRemove() {
            if(CheckIfNeedCollapse()) {
                Collapse();
                return;
            }
            IntegrityChanged?.Invoke(this);
        }

        private bool CheckIfNeedCollapse() {
            int destroyedVoxelsCount = initialVoxelsCount - VoxelsCount;
            return destroyedVoxelsCount >= destructionVoxelsCountThresh;
        }
        
        private void MakePhysical() {
            if(rigidbody == null) {
                if(!TryGetComponent(out rigidbody)) {
                    rigidbody = gameObject.AddComponent<Rigidbody>();
                }
            }

            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if(meshCollider != null) {
                meshCollider.convex = true;
            }

            rigidbody.mass = VoxelsCount * Constants.VoxelWeight;
            rigidbody.WakeUp();
        }
        
        private void Reset() {
            if(voxelsContainer == null) {
                voxelsContainer = GetComponent<VoxelsContainer>();
            }
        }
    }
}