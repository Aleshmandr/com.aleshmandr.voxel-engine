using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    [RequireComponent(typeof(VoxelsClustersDestructionContainer))] [ExecuteAlways]
    public class VoxelsContainerJoint : MonoBehaviour
    {
        private const int CheckCollidersCount = 10;
        private static readonly TimeSpan CheckConnectionDelta = TimeSpan.FromSeconds(0.5f);

        [SerializeField] private JointData[] joints = new JointData[1];

        [SerializeField] private bool parentOnlyMode = true;
        [NonSerialized] private VoxelsClustersDestructionContainer container;
        [NonSerialized] private Collider[] colliders;
        [NonSerialized] private DestructableVoxelsRoot root;
        private List<DestructableVoxels> connectedClusters;
        private List<DestructableVoxels> selfConnectedClusters;
        private CancellationTokenSource jointCts;
        private bool isConnectionDirty;
        private bool isConnectionCheckTaskRun;

        public bool IsInitialized { get; private set; }

        private void Start() {
            jointCts = new CancellationTokenSource();
            container = GetComponent<VoxelsClustersDestructionContainer>();
            connectedClusters = new List<DestructableVoxels>();
            selfConnectedClusters = new List<DestructableVoxels>();
            root = this.GetComponentInParent<DestructableVoxelsRoot>();
            InitFixationRoutineAsync(jointCts.Token).Forget();
        }

        private void OnDestroy() {
            jointCts?.Cancel();
            UnsubscribeFromConnectedClusters();
        }

        private void UnsubscribeFromConnectedClusters() {
            if(connectedClusters != null) {
                for(int i = 0; i < connectedClusters.Count; i++) {
                    connectedClusters[i].IntegrityChanged -= HandleConnectionIntegrityChange;
                }
            }
            if(selfConnectedClusters != null) {
                for(int i = 0; i < selfConnectedClusters.Count; i++) {
                    selfConnectedClusters[i].IntegrityChanged -= HandleSelfConnectionIntegrityChange;
                }
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

        private void BreakJoint() {
            UnsubscribeFromConnectedClusters();
            container.BreakFixedConnections();
            jointCts?.Cancel();
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
                        selfConnectedClusters.Add(destructableVoxels);
                        destructableVoxels.IntegrityChanged += HandleSelfConnectionIntegrityChange;
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
                        selfConnectedClusters.Add(destructableVoxels);
                        destructableVoxels.IntegrityChanged += HandleSelfConnectionIntegrityChange;
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

        private async UniTaskVoid InitFixationRoutineAsync(CancellationToken cancellationToken) {
            if(root != null) {
                while(!root.IsInitialized) {
                    await UniTask.Yield();
                    if(cancellationToken.IsCancellationRequested) {
                        return;
                    }
                }
            }
            FixJoint();
            IsInitialized = true;
        }

        private void HandleConnectionIntegrityChange(DestructableVoxels connectedCluster) {
            if(connectedCluster.IsCollapsed) {
                connectedCluster.IntegrityChanged -= HandleConnectionIntegrityChange;
                connectedClusters.Remove(connectedCluster);
            }
            if(connectedClusters.Count == 0) {
                BreakJoint();
            } else {
                isConnectionDirty = true;
                if(VoxelEngineConfig.RunJointsCheckTask && !isConnectionCheckTaskRun) {
                    isConnectionCheckTaskRun = true;
                    CheckConnectionAsync(jointCts.Token).Forget();
                }
            }
        }

        private void HandleSelfConnectionIntegrityChange(DestructableVoxels connectedCluster) {
            if(connectedCluster.IsCollapsed) {
                connectedCluster.IntegrityChanged -= HandleConnectionIntegrityChange;
                selfConnectedClusters.Remove(connectedCluster);
            }
            if(selfConnectedClusters.Count == 0) {
                BreakJoint();
            } else {
                isConnectionDirty = true;
                if(VoxelEngineConfig.RunJointsCheckTask && !isConnectionCheckTaskRun) {
                    isConnectionCheckTaskRun = true;
                    CheckConnectionAsync(jointCts.Token).Forget();
                }
            }
        }

        private async UniTaskVoid CheckConnectionAsync(CancellationToken cancellationToken) {
            var parentContainer = parentOnlyMode ? FindParentContainer() : null;

            while(!cancellationToken.IsCancellationRequested) {
                if(isConnectionDirty) {
                    isConnectionDirty = false;
                    var hasConnectedOverlaps = false;
                    var hasSelfOverlaps = false;

                    for(int i = 0; i < joints.Length; i++) {
                        var pos = transform.TransformPoint(joints[i].Center);
                        var rad = joints[i].Radius * transform.lossyScale.x;
                        var overlaps = Physics.OverlapSphereNonAlloc(pos, rad, colliders, Physics.AllLayers, QueryTriggerInteraction.Ignore);

                        for(int j = 0; j < overlaps; j++) {
                            var destructableVoxels = colliders[j].GetComponent<DestructableVoxels>();
                            if(destructableVoxels == null || destructableVoxels.IsCollapsed) {
                                continue;
                            }
                            
                            var connectionData = container.GetClusterConnections(destructableVoxels);
                            if(connectionData == null) {
                                if(parentContainer == null || parentContainer.ContainsCluster(destructableVoxels)) {
                                    hasConnectedOverlaps = true;
                                }
                            } else {
                                hasSelfOverlaps = true;
                            }
                        }

                        if(hasConnectedOverlaps && hasSelfOverlaps) {
                            break;
                        }

                        await UniTask.Yield();
                    }

                    if(cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    var hasBothOverlaps = hasConnectedOverlaps && hasSelfOverlaps;
                    if(!hasBothOverlaps) {
                        BreakJoint();
                        return;
                    }
                }

                await UniTask.Delay(CheckConnectionDelta, cancellationToken: cancellationToken);
            }
        }

#if UNITY_EDITOR
        private static readonly Color GizmoColor = new Color(0f, 1f, 0f, 0.5f);

        public JointData[] GetJointsEditor() {
            return joints;
        }

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
  #endif
    }
}
