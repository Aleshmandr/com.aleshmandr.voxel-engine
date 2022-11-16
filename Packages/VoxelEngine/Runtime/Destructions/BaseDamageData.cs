using UnityEngine;

namespace VoxelEngine.Destructions
{
    public struct BaseDamageData : IDamageData
    {
        public Vector3 WorldPoint { get; }
        public float Radius { get; }

        public BaseDamageData(Vector3 worldPoint, float radius) {
            WorldPoint = worldPoint;
            Radius = radius;
        }
    }
}
