#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using Unity.Jobs;
using VoxelEngine.Destructions.Jobs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public void Start() {
            lifetimeCts = new CancellationTokenSource();
            connectionsCheckQueue = new List<DestructableVoxels>();
            integrityCheckQueue = new List<DestructableVoxels>();
            neighboursScheduler = new CheckClustersConnectionJobsScheduler();
            processedClusters = new List<DestructableVoxels>();
            integrityJobsScheduler = new VoxelsIntegrityJobsScheduler();
            foreach(var connectionData in connections) {
                connectionData.Root.IntegrityChanged += HandleClusterDamage;
            }
        }
        
        private void OnDestroy() {
            lifetimeCts?.Cancel();
            foreach(var connectionData in connections) {
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
                if (connections[i].IsFixed)
                {
                    connections[i].IsFixed = false;
                    connections[i].Root.Collapse();
                }
            }
        }

        private void HandleClusterDamage(DestructableVoxels cluster) {
            if(cluster.IsCollapsed) {
                var connectionsData = GetClusterConnections(cluster);
                CheckStructureAsync(connectionsData, lifetimeCts.Token);
                return;
            }
            
            if(updateConnectionsInRuntime) {
                if(!connectionsCheckQueue.Contains(cluster)) {
                    UpdateConnections(cluster, lifetimeCts.Token);
                }
            }

            if(updateIntegrityInRuntime) {
                if(!integrityCheckQueue.Contains(cluster)) {
                    UpdateIntegrity(cluster, lifetimeCts.Token);
                }
            }
        }

        private async void UpdateIntegrity(DestructableVoxels cluster, CancellationToken cancellationToken) {
            integrityCheckQueue.Add(cluster);
            await Task.Delay(TimeSpan.FromSeconds(connectionsUpdateDelay), cancellationToken);
            integrityCheckQueue.Remove(cluster);
            
            var isIntegral = await integrityJobsScheduler.Run(cluster.VoxelsContainer.Data, cluster.VoxelsCount);
            if(cancellationToken.IsCancellationRequested) {
                return;
            }
            
            if(!isIntegral) {
                cluster.Collapse();
            }
        }

        private async void UpdateConnections(DestructableVoxels cluster, CancellationToken cancellationToken) {
            connectionsCheckQueue.Add(cluster);
            await Task.Delay(TimeSpan.FromSeconds(connectionsUpdateDelay), cancellationToken);

            await UpdateConnectionsAsync(cluster, cancellationToken);
            
            if(cancellationToken.IsCancellationRequested) {
                return;
            }

            connectionsCheckQueue.Remove(cluster);
        }

        private async Task UpdateConnectionsAsync(DestructableVoxels cluster, CancellationToken cancellationToken) {
            var connectionsData = GetClusterConnections(cluster);
            for(int i = connectionsData.Connections.Count-1; i >=0; i--) {
                if(cluster.IsCollapsed) {
                    break;
                }
                if(connectionsData.Connections[i].IsCollapsed) {
                    continue;
                }
                
                var result = await neighboursScheduler.Run(cluster, connectionsData.Connections[i], false);
                if(cancellationToken.IsCancellationRequested) {
                    return;
                }
                if(result) {
                   continue;
                }
                var notConnectedCluster = connectionsData.Connections[i];
                var notConnectedClusterConnectionsData = GetClusterConnections(notConnectedCluster);
                notConnectedClusterConnectionsData.Connections.Remove(cluster);
                connectionsData.Connections.RemoveAt(i);
            }
            
            CheckStructureAsync(connectionsData, cancellationToken);
        }

        private async void CheckStructureAsync(ClustersConnectionData connectionsData, CancellationToken cancellationToken) {
            if(connectionsData == null) {
                return;
            }
            
            foreach(var neighbour in connectionsData.Connections) {
                if(neighbour == null || neighbour.IsCollapsed) {
                    continue;
                }

                processedClusters.Clear();
                processedClusters.Add(neighbour);
                
                if(!CheckIfClusterConnected(neighbour, processedClusters)) {
                    await Task.Delay(ClusterDestructionDelayMilliseconds, cancellationToken);
                    if(cancellationToken.IsCancellationRequested) {
                        return;
                    }
                    neighbour.Collapse();
                }
            }
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

        [ContextMenu("Bake Connections (Low Precision)")]
        public void BakeConnections() {
            EditorUtility.ClearProgressBar();
            var clusters = GetComponentsInChildren<DestructableVoxels>();
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

        [ContextMenu("Bake Connections (High Precision)")]
        public void BakeConnectionsWithJobs() {
            EditorUtility.ClearProgressBar();
            var clusters = GetComponentsInChildren<DestructableVoxels>();
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
                        if(EditorUtility.DisplayCancelableProgressBar("Bake Connections ((High Precision))", $"Baking {currentClusterIndex}/{totalClustersProgressCount}", progress)) {
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
#endif
    }
}
