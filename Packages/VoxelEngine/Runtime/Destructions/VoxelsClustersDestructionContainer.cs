#if UNITY_EDITOR
using UnityEditor;
#endif

using Cysharp.Threading.Tasks;
using System;
using Unity.Jobs;
using VoxelEngine.Destructions.Jobs;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public class VoxelsClustersDestructionContainer : MonoBehaviour
    {
        [SerializeField] private float connectionsUpdateDelay = 0.5f;
        [SerializeField] private bool updateConnectionsInRuntime = true;
        [SerializeField] private bool updateIntegrityInRuntime = true;
        [SerializeField] private ClustersConnectionData[] connections;
        private List<DestructableVoxels> integrityCheckQueue;
        private List<DestructableVoxels> connectionsCheckQueue;
        private CheckClustersConnectionJobsScheduler neighboursScheduler;
        private VoxelsIntegrityJobsScheduler integrityJobsScheduler;
        private CancellationTokenSource lifetimeCts;
        private List<DestructableVoxels> processedClusters;
        private const int ClusterDestructionDelayMilliseconds = 200;

        public ClustersConnectionData[] Connections => connections;

        public void Start() {
            lifetimeCts = new CancellationTokenSource();
            connectionsCheckQueue = new List<DestructableVoxels>();
            integrityCheckQueue = new List<DestructableVoxels>();
            neighboursScheduler = new CheckClustersConnectionJobsScheduler();
            processedClusters = new List<DestructableVoxels>();
            integrityJobsScheduler = new VoxelsIntegrityJobsScheduler();
            InitAsync(lifetimeCts.Token).Forget();
        }

        private async UniTaskVoid InitAsync(CancellationToken cancellationToken) {
            var joint = GetComponent<VoxelsContainerJoint>();
            if(joint != null) {
                while(!joint.IsInitialized) {
                    await UniTask.Yield();
                    if(cancellationToken.IsCancellationRequested) {
                        return;
                    }
                }
            }

            foreach(var connectionData in connections) {
                if(connectionData.Root == null) {
                    Debug.LogError($"Connection root is null: {gameObject.name}");
                    continue;
                }
                connectionData.Root.IntegrityChanged += HandleClusterDamage;
            }
        }

        private void OnDestroy() {
            lifetimeCts?.Cancel(false);
            lifetimeCts?.Dispose();
            foreach(var connectionData in connections) {
                if(connectionData.Root == null) {
                    continue;
                }
                connectionData.Root.IntegrityChanged -= HandleClusterDamage;
            }
        }

        public ClustersConnectionData GetClusterConnections(DestructableVoxels cluster) {
            for(int i = 0; i < connections.Length; i++) {
                if(connections[i].Root == cluster) {
                    return connections[i];
                }
            }
            return null;
        }

        public void BreakFixedConnections() {
            for(int i = 0; i < connections.Length; i++) {
                if(connections[i].IsFixed) {
                    connections[i].IsFixed = false;
                    connections[i].Root.Collapse();
                }
            }
        }

        public bool ContainsCluster(DestructableVoxels cluster) {
            if(connections == null) {
                return false;
            }
            for(int i = 0; i < connections.Length; i++) {
                if(connections[i].Root == cluster) {
                    return true;
                }
            }
            return false;
        }

        private void HandleClusterDamage(DestructableVoxels cluster) {
            if(cluster.IsCollapsed) {
                var connectionsData = GetClusterConnections(cluster);
                CheckStructure(connectionsData, lifetimeCts.Token);
                return;
            }

            if(updateConnectionsInRuntime) {
                if(!connectionsCheckQueue.Contains(cluster)) {
                    UpdateConnections(cluster, lifetimeCts.Token).Forget();
                }
            }

            if(updateIntegrityInRuntime) {
                if(!integrityCheckQueue.Contains(cluster)) {
                    UpdateIntegrity(cluster, lifetimeCts.Token).Forget();
                }
            }
        }

        private async UniTask UpdateIntegrity(DestructableVoxels cluster, CancellationToken cancellationToken) {
            integrityCheckQueue.Add(cluster);
            await UniTask.Delay(TimeSpan.FromSeconds(connectionsUpdateDelay), cancellationToken: cancellationToken);
            integrityCheckQueue.Remove(cluster);

            var isIntegral = await integrityJobsScheduler.Run(cluster.VoxelsContainer.Data, cluster.VoxelsCount);
            if(cancellationToken.IsCancellationRequested) {
                return;
            }

            if(!isIntegral) {
                cluster.Collapse();
            }
        }

        private async UniTask UpdateConnections(DestructableVoxels cluster, CancellationToken cancellationToken) {
            connectionsCheckQueue.Add(cluster);
            await UniTask.Delay(TimeSpan.FromSeconds(connectionsUpdateDelay), cancellationToken: cancellationToken);

            await UpdateConnectionsAsync(cluster, cancellationToken);

            if(cancellationToken.IsCancellationRequested) {
                return;
            }

            connectionsCheckQueue.Remove(cluster);
        }

        private async UniTask UpdateConnectionsAsync(DestructableVoxels cluster, CancellationToken cancellationToken) {
            var connectionsData = GetClusterConnections(cluster);
            for(int i = connectionsData.Connections.Count - 1; i >= 0; i--) {
                if(cluster.IsCollapsed) {
                    break;
                }

                if(i >= connectionsData.Connections.Count || connectionsData.Connections[i].IsCollapsed) {
                    continue;
                }

                var result = await neighboursScheduler.Run(cluster, connectionsData.Connections[i], false);
                if(cancellationToken.IsCancellationRequested) {
                    return;
                }
                if(result || i >= connectionsData.Connections.Count) {
                    continue;
                }
                var notConnectedCluster = connectionsData.Connections[i];
                var notConnectedClusterConnectionsData = GetClusterConnections(notConnectedCluster);
                notConnectedClusterConnectionsData.Connections.Remove(cluster);
                connectionsData.Connections.RemoveAt(i);
            }

            CheckStructure(connectionsData, cancellationToken);
        }

        private void CheckStructure(ClustersConnectionData connectionsData, CancellationToken cancellationToken) {
            if(connectionsData == null) {
                return;
            }

            for(int i = 0; i < connectionsData.Connections.Count; i++) {
                var neighbour = connectionsData.Connections[i];
                if(neighbour == null || neighbour.IsCollapsed) {
                    continue;
                }

                processedClusters.Clear();
                processedClusters.Add(neighbour);

                if(!CheckIfClusterConnected(neighbour, processedClusters)) {
                    CollapseWithDelayAsync(neighbour, cancellationToken).Forget();
                }
            }
        }

        private async UniTaskVoid CollapseWithDelayAsync(DestructableVoxels neighbour, CancellationToken cancellationToken) {
            await UniTask.Delay(ClusterDestructionDelayMilliseconds, cancellationToken: cancellationToken);
            if(cancellationToken.IsCancellationRequested) {
                return;
            }
            neighbour.Collapse();
        }

        private bool CheckIfClusterConnected(DestructableVoxels cluster, List<DestructableVoxels> processedClusters) {

            var connectionsData = GetClusterConnections(cluster);
            if(connectionsData == null) {
                return false;
            }

            if(connectionsData.IsFixed) {
                return true;
            }

            foreach(var neighbour in connectionsData.Connections) {
                if(neighbour.IsCollapsed || processedClusters.Contains(neighbour)) {
                    continue;
                }

                processedClusters.Add(neighbour);
                if(CheckIfClusterConnected(neighbour, processedClusters)) {
                    return true;
                }
            }
            return false;
        }

        private bool IsNeighbours(DestructableVoxels a, DestructableVoxels b) {
            var connectionsData = GetClusterConnections(a);
            return connectionsData != null && connectionsData.Connections.Contains(b);
        }


#if UNITY_EDITOR

        private const float Epsilon = 0.01f;

        public void BakeConnections() {
            EditorUtility.ClearProgressBar();
            var clusters = GetClustersInChildren();
            var otherClusters = new List<DestructableVoxels>(clusters);
            connections = new ClustersConnectionData[clusters.Length];
            try {
                for(int i = 0; i < clusters.Length; i++) {
                    connections[i] = new ClustersConnectionData(clusters[i]);
                    clusters[i].VoxelsContainer.Data = NativeArray3dSerializer.Deserialize<int>(clusters[i].VoxelsContainer.Asset.bytes);
                }

                int currentClusterIndex = 0;
                int totalClustersProgressCount = clusters.Length * clusters.Length;
                foreach(var cluster in clusters) {
                    foreach(var otherCluster in otherClusters) {
                        var progress = currentClusterIndex / (float)totalClustersProgressCount;
                        if(EditorUtility.DisplayCancelableProgressBar("Bake Connections (Low Precision)", $"Baking {currentClusterIndex}/{totalClustersProgressCount}", progress)) {
                            return;
                        }
                        if(cluster == otherCluster || IsNeighbours(cluster, otherCluster)) {
                            currentClusterIndex++;
                            continue;
                        }

                        if(CheckIfNeighboursFast(cluster, otherCluster)) {
                            Connect(cluster, otherCluster);
                        }
                        currentClusterIndex++;
                    }
                    currentClusterIndex++;
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
                for(int i = 0; i < clusters.Length; i++) {
                    clusters[i].VoxelsContainer.Data.Dispose();
                }
            }
        }

        public void BakeConnectionsWithJobs() {
            EditorUtility.ClearProgressBar();
            var clusters = GetClustersInChildren();
            var otherClusters = new List<DestructableVoxels>(clusters);
            connections = new ClustersConnectionData[clusters.Length];
            try {
                for(int i = 0; i < clusters.Length; i++) {
                    connections[i] = new ClustersConnectionData(clusters[i]);
                    clusters[i].VoxelsContainer.Data = NativeArray3dSerializer.Deserialize<int>(clusters[i].VoxelsContainer.Asset.bytes);
                }

                int currentClusterIndex = 0;
                int totalClustersProgressCount = clusters.Length * clusters.Length;
                foreach(var cluster in clusters) {
                    foreach(var otherCluster in otherClusters) {
                        var progress = currentClusterIndex / (float)totalClustersProgressCount;
                        if(EditorUtility.DisplayCancelableProgressBar("Bake Connections (High Precision)", $"Baking {currentClusterIndex}/{totalClustersProgressCount}", progress)) {
                            return;
                        }
                        if(cluster == otherCluster || IsNeighbours(cluster, otherCluster)) {
                            currentClusterIndex++;
                            continue;
                        }

                        RunCheckNeighboursJob(cluster, otherCluster);

                        currentClusterIndex++;
                    }
                    currentClusterIndex++;
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
                for(int i = 0; i < clusters.Length; i++) {
                    clusters[i].VoxelsContainer.Data.Dispose();
                }
            }
        }

        private void RunCheckNeighboursJob(DestructableVoxels cluster, DestructableVoxels otherCluster) {
            var result = new NativeArray<bool>(1, Allocator.TempJob);
            var checkNeighboursJob = new CheckClustersConnectionJob {
                ChunkOneData = cluster.VoxelsContainer.Data,
                ChunkTwoData = otherCluster.VoxelsContainer.Data,
                ChunkOnePos = cluster.transform.localPosition,
                ChunkTwoPos = otherCluster.transform.localPosition,
                Result = result
            };

            checkNeighboursJob.Schedule().Complete();

            if(result[0]) {
                Connect(cluster, otherCluster);
            }
            result.Dispose();
        }

        private bool CheckIfNeighboursFast(DestructableVoxels cluster, DestructableVoxels otherCluster) {
            var clusterVoxels = cluster.VoxelsContainer;
            var otherClusterVoxels = otherCluster.VoxelsContainer;

            if(otherClusterVoxels == null || clusterVoxels == null) {
                return false;
            }

            var clustersDelta = otherCluster.transform.localPosition - cluster.transform.localPosition;

            var dx = clustersDelta.x > 0f ? clustersDelta.x - cluster.VoxelsContainer.Data.SizeX
                : Mathf.Abs(clustersDelta.x) - otherCluster.VoxelsContainer.Data.SizeX;

            var dy = clustersDelta.y > 0f ? clustersDelta.y - cluster.VoxelsContainer.Data.SizeY
                : Mathf.Abs(clustersDelta.y) - otherCluster.VoxelsContainer.Data.SizeY;

            var dz = clustersDelta.z > 0f ? clustersDelta.z - cluster.VoxelsContainer.Data.SizeZ
                : Mathf.Abs(clustersDelta.z) - otherCluster.VoxelsContainer.Data.SizeZ;

            return dx < Epsilon && dy < Epsilon && dz < Epsilon;
        }

        private void Connect(DestructableVoxels a, DestructableVoxels b) {
            var connectionsA = GetClusterConnections(a);
            var connectionsB = GetClusterConnections(b);
            if(connectionsA == null || connectionsB == null) {
                return;
            }
            connectionsA.Connections.Add(b);
            connectionsB.Connections.Add(a);
            EditorUtility.SetDirty(this);
        }

        private DestructableVoxels[] GetClustersInChildren() {
            var result = new List<DestructableVoxels>();
            CollectClustersRecursive(transform, result);
            return result.ToArray();
        }

        private void CollectClustersRecursive(Transform root, List<DestructableVoxels> resultList) {
            if(root.childCount <= 0) {
                return;
            }
            for(int i = 0; i < root.childCount; i++) {
                var child = root.GetChild(i);
                if(child.TryGetComponent(out VoxelsClustersDestructionContainer _)) {
                    continue;
                }
                if(child.TryGetComponent(out DestructableVoxels voxels)) {
                    resultList.Add(voxels);
                }
                CollectClustersRecursive(child, resultList);
            }
        }
#endif
    }
}
