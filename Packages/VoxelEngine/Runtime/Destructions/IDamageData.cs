using UnityEngine;

namespace VoxelEngine.Destructions
{
    public interface IDamageData
    {
        Vector3 WorldPoint { get; }
        float Radius { get; }
    }
}
