using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VoxelEngine.Destructions.Jobs;

namespace VoxelEngine.Destructions
{
    public class VoxelsFractureObject : MonoBehaviour
    {
        [SerializeField] private VoxelsContainer voxelsContainer;
        [SerializeField] private float toughness = 1f;
        [SerializeField] private int minFractureSize = 3;
        [SerializeField] private int maxFractureSize = 10;
        private VoxelsFractureJobsScheduler fractureJobsScheduler;

        public VoxelsContainer VoxelsContainer => voxelsContainer;

        public async UniTaskVoid Hit(Vector3 pos, Vector3 force) {
            Debug.Log($"Hit p:{pos}, f:{force}");
            Debug.DrawRay(pos, force, Color.green, 1f);
            float dmgRadius = force.magnitude / toughness;
            DrawDebugSphere(pos, dmgRadius, new Color(0f, 1f, 0f, 0.3f), 1f);
            var fractureData = await RunDamageJob(new BaseDamageData(pos, dmgRadius), Allocator.Persistent);

            int totalClusters = 0;
            for(int i = 0; i < fractureData.Length; i++) {
                int clusterSize = fractureData[i];
                int clusterStartIndex = i + 1;
                Debug.Log($"Closter:{clusterSize}");
                for(int j = clusterStartIndex; j < clusterStartIndex+clusterSize; j++) {
                    i++;
                }
                totalClusters++;
            }
            
            Debug.Log($"Total clusters:{totalClusters}");

            fractureData.Dispose();
        }
        
        
        public async UniTask<NativeList<int>> RunDamageJob<T>(T damageData, Allocator allocator) where T : IDamageData {
            int intRad = Mathf.CeilToInt(damageData.Radius / voxelsContainer.transform.lossyScale.x);
            var localPoint = voxelsContainer.transform.InverseTransformPoint(damageData.WorldPoint);
            var localPointInt = new Vector3Int((int)localPoint.x, (int)localPoint.y, (int)localPoint.z);

            fractureJobsScheduler ??= new VoxelsFractureJobsScheduler();
            var damageVoxels = await fractureJobsScheduler.Run(voxelsContainer.Data, intRad, minFractureSize, maxFractureSize, localPointInt, allocator);

            voxelsContainer.RebuildMesh(true).Forget();
            return damageVoxels;
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
#endif
    }
}
