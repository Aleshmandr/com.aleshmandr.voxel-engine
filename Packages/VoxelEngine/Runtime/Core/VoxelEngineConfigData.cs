using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelEngine
{
    [CreateAssetMenu(fileName = "VoxelEngineConfig", menuName = "VoxelEngine/VoxelEngineConfig", order = 0)]
    public class VoxelEngineConfigData : ScriptableObject
    {
        public bool UseOptimizedMeshGenerationAtRuntime;
        public bool RunJointsCheckTask;
        [FormerlySerializedAs("UseParentLayer")] [Header("Fracture")]
        public bool FractureUseParentLayer = true;
        [FormerlySerializedAs("CustomLayer")] [Layer]
        public int FracturesCustomLayer;
    }
}
