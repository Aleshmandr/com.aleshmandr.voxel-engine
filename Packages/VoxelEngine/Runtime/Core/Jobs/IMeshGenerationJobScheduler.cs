using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    public interface IMeshGenerationJobScheduler
    {
        UniTask<Mesh> Run(NativeArray3d<int> voxels, CancellationToken cancellationToken, Mesh mesh = null);
    }
}
