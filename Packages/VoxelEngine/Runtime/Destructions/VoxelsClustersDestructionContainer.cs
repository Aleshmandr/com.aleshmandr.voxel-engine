using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
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

        [ContextMenu("Bake Connections")]
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
                        if(EditorUtility.DisplayCancelableProgressBar("Bake Connections", "Baking", progress)) {
                            return;
                        }
                        if(cluster == otherCluster || IsNeighbours(cluster, otherCluster)) {
                            currentClusterIndex++;
                            continue;
                        }

                        if(CheckIfNeighbours(cluster, otherCluster)) {
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

        private bool CheckIfNeighbours(DestructableVoxels cluster, DestructableVoxels otherCluster) {
            var clusterVoxels = cluster.VoxelsContainer;
            var otherClusterVoxels = otherCluster.VoxelsContainer;
            if(otherClusterVoxels == null || clusterVoxels == null) {
                return false;
            }

            var clustersDelta = otherCluster.transform.localPosition - cluster.transform.localPosition;
            if(clustersDelta.x - Epsilon > clusterVoxels.Data.SizeX ||
               clustersDelta.y - Epsilon > clusterVoxels.Data.SizeY ||
               clustersDelta.z - Epsilon > clusterVoxels.Data.SizeZ) {
                return false;
            }

            var clusterLocalPos = cluster.transform.localPosition;
            var otherClusterLocalPos = otherCluster.transform.localPosition;

            for(int i1 = 0; i1 < clusterVoxels.Data.SizeX; i1++) {
                for(int j1 = 0; j1 < clusterVoxels.Data.SizeY; j1++) {
                    for(int k1 = 0; k1 < clusterVoxels.Data.SizeZ; k1++) {
                        if(clusterVoxels.Data[i1, j1, k1] == 0 || clusterVoxels.IsVoxelInner(i1, j1, k1)) {
                            continue;
                        }
                        var voxelPos = new Vector3(i1 + clusterLocalPos.x, j1 + clusterLocalPos.y, k1 + clusterLocalPos.z);

                        for(int i2 = 0; i2 < otherClusterVoxels.Data.SizeX; i2++) {
                            for(int j2 = 0; j2 < otherClusterVoxels.Data.SizeY; j2++) {
                                for(int k2 = 0; k2 < otherClusterVoxels.Data.SizeZ; k2++) {
                                    if(otherClusterVoxels.Data[i2, j2, k2] == 0 || otherClusterVoxels.IsVoxelInner(i2, j2, k2)) {
                                        continue;
                                    }
                                    var otherVoxelPos = new Vector3(i2 + otherClusterLocalPos.x, j2 + otherClusterLocalPos.y, k2 + otherClusterLocalPos.z);
                                    if((otherVoxelPos - voxelPos).sqrMagnitude <= 1f + Epsilon) {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
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
