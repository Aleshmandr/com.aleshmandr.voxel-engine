using UnityEngine;

namespace VoxelEngine.Destructions
{
    public struct ForceDamageData : IForceDamageData
    {
        public Vector3 WorldPoint { get; }
        public float Radius { get; }
        public Vector3 Force { get; }

        public ForceDamageData(Vector3 worldPoint, Vector3 force, float radius) {
            WorldPoint = worldPoint;
            Force = force;
            Radius = radius;
        }
    }
}
