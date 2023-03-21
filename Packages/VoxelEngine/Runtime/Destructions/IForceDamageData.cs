using UnityEngine;

namespace VoxelEngine.Destructions
{
    public interface IForceDamageData : IDamageData
    {
        Vector3 Force { get; }
    }
}
