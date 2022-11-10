using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public class VoxelsJoint : MonoBehaviour
    {
        public event Action FixationBroken;
        public event Action FixationUpdated;

        private const int FixationCollidersCount = 4;
        private static readonly TimeSpan FixationUpdateTimeSpan = TimeSpan.FromSeconds(0.5f);

        [SerializeField] private float fixationRadius = 1f;
        [SerializeField] private Vector3 center;
        [SerializeField] private bool disableOnFixationBreak;
        private Collider[] overlapColliders;
        private List<DestructableVoxels> fixations;
        private bool isDirty;
        private CancellationTokenSource lifetimeCts;

        public bool IsFixed { get; private set; }

        private void Awake() {
            overlapColliders = new Collider[FixationCollidersCount];
            fixations = new List<DestructableVoxels>();
        }

        private void OnEnable() {
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();
            UpdateFixationsAsync(lifetimeCts.Token).Forget();
        }

        private void OnDisable() {
            lifetimeCts?.Cancel();
        }

        [ContextMenu("Mark Dirty")]
        public void MarkDirty() {
            isDirty = true;
        }

        private async UniTaskVoid UpdateFixationsAsync(CancellationToken cancellationToken) {
            while(!cancellationToken.IsCancellationRequested) {
                UpdateFixations();
                await UniTask.Delay(FixationUpdateTimeSpan, cancellationToken: cancellationToken);
                if(cancellationToken.IsCancellationRequested) {
                    return;
                }

                bool isFixed = fixations.Count > 0;
                bool fixationHasBroken = IsFixed && !isFixed;
                IsFixed = isFixed;
                FixationUpdated?.Invoke();
                if(fixationHasBroken) {
                    if(disableOnFixationBreak) {
                        gameObject.SetActive(false);
                    }
                    FixationBroken?.Invoke();
                    break;
                }

                if(!IsFixed) {
                    continue;
                }

                while(!isDirty && !cancellationToken.IsCancellationRequested) {
                    await UniTask.Delay(FixationUpdateTimeSpan, cancellationToken: cancellationToken);
                }
            }
        }

        private void UpdateFixations() {
            for(int i = 0; i < fixations.Count; i++) {
                fixations[i].IntegrityChanged -= HandleFixationIntegrityChange;
            }
            fixations.Clear();
            var overlaps = Physics.OverlapSphereNonAlloc(transform.TransformPoint(center), fixationRadius, overlapColliders);
            if(overlaps > 0) {
                for(int i = 0; i < overlaps; i++) {
                    if(overlapColliders[i].TryGetComponent(out DestructableVoxels destructableVoxels) && !destructableVoxels.IsCollapsed) {
                        fixations.Add(destructableVoxels);
                    }
                }

                for(int i = 0; i < fixations.Count; i++) {
                    fixations[i].IntegrityChanged += HandleFixationIntegrityChange;
                }
            }
            isDirty = false;
        }

        private void HandleFixationIntegrityChange(DestructableVoxels destructableVoxels) {
            MarkDirty();
        }

#if UNITY_EDITOR

        private static readonly Color FixationColor = new Color(0f, 1f, 0f, 0.4f);

        public Vector3 CenterEditor { get => center; set => center = value; }
        
        private void OnDrawGizmosSelected() {
            Gizmos.color = FixationColor;
            Gizmos.DrawSphere(transform.TransformPoint(center), fixationRadius);
        }
#endif
    }
}
