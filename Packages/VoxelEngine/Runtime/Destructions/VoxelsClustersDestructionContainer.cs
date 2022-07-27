#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.Jobs;
using VoxelEngine.Destructions.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public class VoxelsClustersDestructionContainer : MonoBehaviour
    {
        [SerializeField] private ClustersConnectionData[] connections;
        private const int ClusterDestructionDelayMilliseconds = 200;

        public void Start() {
            foreach(var connectionData in connections) {
                connectionData.Root.IntegrityChanged += HandleClusterDestruction;
            }
        }

        private ClustersConnectionData GetClusterConnections(DestructableVoxels cluster) {
            for(int i = 0; i < connections.Length; i++) {
                if(connections[i].Root == cluster) {
                    return connections[i];
                }
            }
            return null;
        }

        private void HandleClusterDestruction(DestructableVoxels cluster) {
            if(!cluster.IsCollapsed) {
                return;
            }
            var connectionsData = GetClusterConnections(cluster);
            CheckStructureAsync(connectionsData);
        }

        private async void CheckStructureAsync(ClustersConnectionData connectionsData) {
            if(connectionsData == null) {
                return;
            }
            foreach(var neighbour in connectionsData.Connections) {
                if(neighbour.IsCollapsed) {
                    continue;
                }

                var processedClusters = new List<DestructableVoxels> {
                    neighbour
                };
                if(!CheckIfClusterConnected(neighbour, processedClusters)) {
                    await Task.Delay(ClusterDestructionDelayMilliseconds);
                    neighbour.Collapse();
                }
            }
        }

        private bool CheckIfClusterConnected(DestructableVoxels cluster, List<DestructableVoxels> processedClusters) {
            var connectionsData = GetClusterConnections(cluster);
            if(connectionsData == null) {
                return false;
            }

            foreach(var neighbour in connectionsData.Connections) {
                if(neighbour.IsCollapsed || processedClusters.Contains(neighbour)) {
                    continue;
                }

                if(connectionsData.IsFixed) {
                    return true;
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
            var checkNeighboursJob = new CheckVoxelsChunksNeighboursJob {
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

            var dx = Mathf.Abs(clustersDelta.x) - (cluster.VoxelsContainer.Data.SizeX + otherCluster.VoxelsContainer.Data.SizeX) * 0.5f;
            var dy = Mathf.Abs(clustersDelta.y) - (cluster.VoxelsContainer.Data.SizeY + otherCluster.VoxelsContainer.Data.SizeY) * 0.5f;
            var dz = Mathf.Abs(clustersDelta.z) - (cluster.VoxelsContainer.Data.SizeZ + otherCluster.VoxelsContainer.Data.SizeZ) * 0.5f;

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
