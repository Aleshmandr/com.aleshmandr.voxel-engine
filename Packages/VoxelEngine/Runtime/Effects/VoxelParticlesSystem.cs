using Unity.Collections;
using UnityEngine;

namespace VoxelEngine.Effects
{
    public class VoxelParticlesSystem : MonoBehaviour
    {
        private static readonly Vector3 Offset = new Vector3(-0.5f, -0.5f, -0.5f);
        [SerializeField] private ParticleSystem particles;
        [SerializeField] [Min(1)] private int mod = 1;

        public void Play(NativeList<VoxelData> voxels, Matrix4x4 transformationMatrix) {
            var rotation = transformationMatrix.rotation;
            var scale = transformationMatrix.lossyScale;
            var totalParticles = voxels.Length / mod + 1;
            var nativeParticles = new NativeArray<ParticleSystem.Particle>(totalParticles, Allocator.Temp);

            var mainModule = particles.main;
            mainModule.maxParticles = totalParticles;
            mainModule.startSizeX = scale.x;
            mainModule.startSizeY = scale.y;
            mainModule.startSizeZ = scale.z;
            mainModule.startRotationX = rotation.x;
            mainModule.startRotationY = rotation.y;
            mainModule.startRotationZ = rotation.z;

            particles.Emit(totalParticles);
            particles.GetParticles(nativeParticles);

            var particleIndex = 0;
            for(var i = 0; i < voxels.Length && particleIndex < totalParticles; i++) {
                if(i % mod != 0) {
                    continue;
                }
                var particle = nativeParticles[particleIndex];
                particle.position = transformationMatrix.MultiplyPoint(voxels[i].Position + Offset);
                particle.startColor = voxels[i].Color;
                nativeParticles[particleIndex] = particle;
                particleIndex++;
            }

            particles.SetParticles(nativeParticles, totalParticles);
        }
    }
}
