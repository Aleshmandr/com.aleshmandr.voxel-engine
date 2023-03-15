using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    public interface IMeshGenerationJobScheduler
    {
        UniTask<Mesh> Run(NativeArray3d<int> voxels, Mesh mesh = null);
    }
}
