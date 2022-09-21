using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    [RequireComponent(typeof(VoxelsClustersDestructionContainer))]
    public class VoxelsContainerJoint : MonoBehaviour
    {
        private const int CheckCollidersCount = 6;
        
        [SerializeField] private float radius;
        [SerializeField] private Vector3 center;
        private VoxelsClustersDestructionContainer container;
        private Collider[] colliders;
        private List<DestructableVoxels> connectedClusters;
        private List<DestructableVoxels> fixedClusters;
        private DestructableVoxelsRoot root;

        private void Start() {
            colliders = new Collider[CheckCollidersCount];
            container = GetComponent<VoxelsClustersDestructionContainer>();
            connectedClusters = new List<DestructableVoxels>();
            fixedClusters = new List<DestructableVoxels>();
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
                connectedClusters[i].IntegrityChanged -= HandleConnectionIntegrityChnage;
            }
        }

        private IEnumerator InitFixationRoutine() {
            if(root != null) {
                while(!root.IsInitialized) {
                    yield return null;
                }
            }
            var overlaps = Physics.OverlapSphereNonAlloc(transform.TransformPoint(center), radius, colliders);
            for(int i = 0; i < overlaps; i++) {
                var destructableVoxels = colliders[i].GetComponent<DestructableVoxels>();
                if(destructableVoxels != null) {
                    var connectionData = container.GetClusterConnections(destructableVoxels);
                    if(connectionData == null) {
                        connectedClusters.Add(destructableVoxels);
                        destructableVoxels.IntegrityChanged += HandleConnectionIntegrityChnage;
                    } else {
                        connectionData.IsFixed = true;
                        fixedClusters.Add(destructableVoxels);
                    }
                }
            }
        }

        private void HandleConnectionIntegrityChnage(DestructableVoxels connectedCluster) {
            if(connectedCluster.IsCollapsed) {
                connectedClusters.Remove(connectedCluster);
            }
            if(connectedClusters.Count == 0) {
                UnsubscribeFromConnectedClusters();
                for(int i = 0; i < fixedClusters.Count; i++) {
                    fixedClusters[i].Collapse();
                }
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
