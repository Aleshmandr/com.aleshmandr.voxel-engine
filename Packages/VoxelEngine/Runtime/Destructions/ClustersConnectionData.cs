using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    [System.Serializable]
    public class ClustersConnectionData
    {
        [SerializeField] private DestructableVoxels root;
        [SerializeField] private bool isFixed;
        [SerializeField] private List<DestructableVoxels> connections;

        public DestructableVoxels Root => root;
        
        public bool IsFixed
        { 
            get => isFixed;
            set => isFixed = value; 
        }

        public List<DestructableVoxels> Connections => connections;

        public ClustersConnectionData(DestructableVoxels rootCluster) {
            root = rootCluster;
            connections = new List<DestructableVoxels>();
        }
    }
}
