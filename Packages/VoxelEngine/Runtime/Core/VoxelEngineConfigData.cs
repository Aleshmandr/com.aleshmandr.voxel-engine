using UnityEngine;

namespace VoxelEngine
{
    [CreateAssetMenu(fileName = "VoxelEngineConfig", menuName = "VoxelEngine/VoxelEngineConfig", order = 0)]
    public class VoxelEngineConfigData : ScriptableObject
    {
        public bool UseOptimizedMeshGenerationAtRuntime;
        public bool RunJointsCheckTask;
    }
}
