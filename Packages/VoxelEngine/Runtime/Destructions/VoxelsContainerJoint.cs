using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    [RequireComponent(typeof(VoxelsClustersDestructionContainer))][ExecuteAlways]
    public class VoxelsContainerJoint : MonoBehaviour
    {
        private const int CheckCollidersCount = 6;
        
        [SerializeField] private float radius;
        [SerializeField] private Vector3 center;
        [NonSerialized] private VoxelsClustersDestructionContainer container;
        [NonSerialized] private Collider[] colliders;
        [NonSerialized] private DestructableVoxelsRoot root;
        private List<DestructableVoxels> connectedClusters;

        private void Start() {
            container = GetComponent<VoxelsClustersDestructionContainer>();
            connectedClusters = new List<DestructableVoxels>();
            root = this.GetComponentInParent<DestructableVoxelsRoot>();
            StartCoroutine(InitFixationRoutine());
        }

        private void OnDestroy() {
            UnsubscribeFromConnectedClusters();
        }

        private void UnsubscribeFromConnectedClusters() {
            if(connectedClusters == null) {
                return;
            }
            for(int i = 0; i < connectedClusters.Count; i++) {
                connectedClusters[i].IntegrityChanged -= HandleConnectionIntegrityChange;
            }
        }

        public void FixJoint() {
            if(colliders == null || colliders.Length == 0) {
                colliders = new Collider[CheckCollidersCount];
            }
            
            var overlaps = 0;
            var pos = transform.TransformPoint(center);
            
            overlaps = gameObject.scene.GetPhysicsScene().OverlapSphere(pos, radius, colliders, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            
            for(int i = 0; i < overlaps; i++) {
                var destructableVoxels = colliders[i].GetComponent<DestructableVoxels>();
                if(destructableVoxels != null) {
                    var connectionData = container.GetClusterConnections(destructableVoxels);
                    if(connectionData == null) {
                        connectedClusters.Add(destructableVoxels);
                        destructableVoxels.IntegrityChanged += HandleConnectionIntegrityChange;
                    } else {
                        connectionData.IsFixed = true;
                    }
                }
            }
        }

        private IEnumerator InitFixationRoutine() {
            if(root != null) {
                while(!root.IsInitialized) {
                    yield return null;
                }
            }
            FixJoint();
        }

        private void HandleConnectionIntegrityChange(DestructableVoxels connectedCluster) {
            if(connectedCluster.IsCollapsed) {
                connectedClusters.Remove(connectedCluster);
            }
            if(connectedClusters.Count == 0) {
                UnsubscribeFromConnectedClusters();
                container.BreakFixedConnections();
            }
        }

#if UNITY_EDITOR
        private static readonly Color GizmoColor = new Color(0f, 1f, 0f, 0.5f);

        private void OnDrawGizmos() {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = GizmoColor;
            Gizmos.DrawSphere(center, radius);
        }
  #endif
    }
}
