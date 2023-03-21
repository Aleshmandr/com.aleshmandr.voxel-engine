using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using VoxelEngine.Destructions.Jobs;

namespace VoxelEngine.Destructions
{
    public class VoxelsFractureObject : MonoBehaviour
    {
        [SerializeField] private VoxelsContainer voxelsContainer;
        [SerializeField] [Min(0)] private float toughness = 1f;
        [SerializeField] [Min(1)] private int minFractureSize = 3;
        [SerializeField] [Min(1)] private int maxFractureSize = 10;
        [SerializeField] private bool collapseHangingParts = true;
        private VoxelsFractureJobsScheduler fractureJobsScheduler;
        private CancellationTokenSource lifeTimeCts;

        public VoxelsContainer VoxelsContainer => voxelsContainer;

        private void Awake() {
            lifeTimeCts = new CancellationTokenSource();
        }

        public async UniTaskVoid Hit(Vector3 pos, float radius, Vector3 force) {
            
#if UNITY_EDITOR
            Debug.DrawRay(pos, force, Color.green, 1f);
            DrawDebugSphere(pos, radius, new Color(0f, 1f, 0f, 0.3f), 1f);
  #endif

            var fractureData = await RunDamageJob(new ForceDamageData(pos, force, radius), Allocator.Persistent, lifeTimeCts.Token);
            fractureData.Dispose();
        }

        private async UniTask<FractureData> RunDamageJob<T>(T damageData, Allocator allocator, CancellationToken cancellationToken) where T : IForceDamageData {
            int intRad = Mathf.CeilToInt(damageData.Radius / (toughness * voxelsContainer.transform.lossyScale.x));
            var localPoint = voxelsContainer.transform.InverseTransformPoint(damageData.WorldPoint);
            var localPointInt = new Vector3Int((int)localPoint.x, (int)localPoint.y, (int)localPoint.z);

            fractureJobsScheduler ??= new VoxelsFractureJobsScheduler();
            FractureData fractureData = await fractureJobsScheduler.Run(voxelsContainer.Data, intRad, minFractureSize, maxFractureSize, collapseHangingParts, localPointInt, allocator);
            if(cancellationToken.IsCancellationRequested) {
                return FractureData.Empty;
            }

            int totalSize = 0;
            for(int f = 0; f < fractureData.ClustersLengths.Length; f++) {

                int currentSize = fractureData.ClustersLengths[f];

                int maxX = 0;
                int maxZ = 0;
                int maxY = 0;

                int minX = int.MaxValue;
                int minZ = int.MaxValue;
                int minY = int.MaxValue;

                for(int i = totalSize; i < totalSize + currentSize; i++) {
                    var pos = fractureData.Voxels[i].Position;
                    if(pos.x > maxX) {
                        maxX = pos.x;
                    }
                    if(pos.x < minX) {
                        minX = pos.x;
                    }

                    if(pos.y > maxY) {
                        maxY = pos.y;
                    }
                    if(pos.y < minY) {
                        minY = pos.y;
                    }

                    if(pos.z > maxZ) {
                        maxZ = pos.z;
                    }
                    if(pos.z < minZ) {
                        minZ = pos.z;
                    }
                }

                int sizeX = maxX - minX + 1;
                int sizeY = maxY - minY + 1;
                int sizeZ = maxZ - minZ + 1;

                var data = new NativeArray3d<int>(sizeX, sizeY, sizeZ);
                for(int i = totalSize; i < totalSize + currentSize; i++) {
                    var pos = fractureData.Voxels[i].Position;
                    data[pos.x - minX, pos.y - minY, pos.z - minZ] = fractureData.Voxels[i].Color;
                }
                totalSize += currentSize;

                Vector3 worldPos = transform.TransformPoint(minX, minY, minZ);
                VoxelsFractureEngine.FractureFactory.Create(this, data, worldPos, damageData);
            }

            voxelsContainer.RebuildMesh(true).Forget();

            return fractureData;
        }

        private void OnDestroy() {
            lifeTimeCts?.Cancel(false);
            lifeTimeCts?.Dispose();
        }

#if UNITY_EDITOR

        private List<DebugSphereData> debugSpheres;

        private class DebugSphereData
        {
            public float Radius;
            public Vector3 Position;
            public float RemainingTime;
            public Color Color;
        }

        private void DrawDebugSphere(Vector3 pos, float radius, Color color, float duration) {
            debugSpheres ??= new List<DebugSphereData>();
            debugSpheres.Add(new DebugSphereData {
                Color = color, Radius = radius, Position = pos, RemainingTime = duration
            });
        }

        private void OnDrawGizmos() {
            if(debugSpheres == null || debugSpheres.Count == 0) {
                return;
            }

            for(int i = debugSpheres.Count - 1; i >= 0; i--) {
                Gizmos.color = debugSpheres[i].Color;
                Gizmos.DrawSphere(debugSpheres[i].Position, debugSpheres[i].Radius);
                debugSpheres[i].RemainingTime -= Time.smoothDeltaTime;
                if(debugSpheres[i].RemainingTime < 0f) {
                    debugSpheres.RemoveAt(i);
                }
            }
        }

        private void Reset() {
            if(voxelsContainer == null) {
                voxelsContainer = GetComponent<VoxelsContainer>();
            }
        }

        private void OnValidate() {
            if(maxFractureSize < minFractureSize) {
                maxFractureSize = minFractureSize;
            }
        }
#endif
    }
}
