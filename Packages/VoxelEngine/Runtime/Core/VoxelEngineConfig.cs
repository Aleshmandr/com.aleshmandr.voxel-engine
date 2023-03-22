using UnityEngine;

namespace VoxelEngine
{
    public static class VoxelEngineConfig
    {
        public static readonly bool UseOptimizedMeshGenerationAtRuntime;
        public static readonly bool RunJointsCheckTask;
        private const string ConfigResourcePath = "VoxelEngineConfig";

        static VoxelEngineConfig() {
            var config = Resources.Load<VoxelEngineConfigData>(ConfigResourcePath);
            if(config != null) {
                UseOptimizedMeshGenerationAtRuntime = config.UseOptimizedMeshGenerationAtRuntime;
                RunJointsCheckTask = config.RunJointsCheckTask;
            }
        }
    }
}
