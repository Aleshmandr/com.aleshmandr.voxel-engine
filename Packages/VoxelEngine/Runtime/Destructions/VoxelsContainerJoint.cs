using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelEngine.Destructions
{
    [RequireComponent(typeof(VoxelsClustersDestructionContainer))] [ExecuteAlways]
    public class VoxelsContainerJoint : MonoBehaviour
    {
        [Serializable]
        private struct JointData
        {
            [field: SerializeField] public float Radius { get; private set; }
            [field: SerializeField] public Vector3 Center { get; private set; }

            public JointData(float radius, Vector3 center) {
                Radius = radius;
                Center = center;
            }
        }

        private const int CheckCollidersCount = 10;

        [SerializeField] private JointData[] joints = new JointData[1];

        [SerializeField] private bool parentOnlyMode = true;
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

            for(int i = 0; i < joints.Length; i++) {
                var pos = transform.TransformPoint(joints[i].Center);
                var scaledRadius = joints[i].Radius * transform.lossyScale.x;

                if(parentOnlyMode) {
                    JoinToParentContainer(pos, scaledRadius);
                } else {
                    JoinToAllContainers(pos, scaledRadius);
                }
            }
        }

        private void JoinToParentContainer(Vector3 pos, float rad) {
            var overlaps = gameObject.scene.GetPhysicsScene().OverlapSphere(pos, rad, colliders, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            var parentContainer = FindParentContainer();

            for(int i = 0; i < overlaps; i++) {
                var destructableVoxels = colliders[i].GetComponent<DestructableVoxels>();
                if(destructableVoxels != null) {
                    var connectionData = container.GetClusterConnections(destructableVoxels);
                    if(connectionData == null) {
                        if(parentContainer != null && parentContainer.ContainsCluster(destructableVoxels)) {
                            connectedClusters.Add(destructableVoxels);
                            destructableVoxels.IntegrityChanged += HandleConnectionIntegrityChange;
                        }
                    } else {
                        connectionData.IsFixed = true;
                    }
                }
            }
        }

        private void JoinToAllContainers(Vector3 pos, float rad) {
            var overlaps = gameObject.scene.GetPhysicsScene().OverlapSphere(pos, rad, colliders, Physics.AllLayers, QueryTriggerInteraction.Ignore);

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

        private VoxelsClustersDestructionContainer FindParentContainer() {
            var parent = transform.parent;
            if(parent == null) {
                return null;
            }
            return parent.GetComponentInParent<VoxelsClustersDestructionContainer>();
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
            if(joints == null) {
                return;
            }

            for(int i = 0; i < joints.Length; i++) {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = GizmoColor;
                Gizmos.DrawSphere(joints[i].Center, joints[i].Radius);
            }
        }

        [ContextMenu("FixLegacyJount")]
        public void FixLegacyJount() {
            joints = new[] {
                new JointData(radius, center)
            };
            EditorUtility.SetDirty(this.gameObject);
        }
  #endif
    }
}
