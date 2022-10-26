using System;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    [Serializable]
    public struct JointData
    {
        [field: SerializeField] public float Radius { get; private set; }
        [field: SerializeField] public Vector3 Center { get; private set; }

        public JointData(float radius, Vector3 center) {
            Radius = radius;
            Center = center;
        }
    }
}
