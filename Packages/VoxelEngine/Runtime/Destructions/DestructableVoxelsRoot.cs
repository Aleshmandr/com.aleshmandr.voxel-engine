using System.Collections;
using UnityEngine;

namespace VoxelEngine.Destructions
{
    public class DestructableVoxelsRoot : MonoBehaviour
    {
        private DestructableVoxels[] destructableVoxels;

        public DestructableVoxels[] DestructableVoxels
        { get {
            if(destructableVoxels == null) {
                destructableVoxels = this.GetComponentsInChildren<DestructableVoxels>();
            }
            return destructableVoxels;
        } }

        public bool IsInitialized { get; private set; }

        private IEnumerator Start() {

            while(!IsInitialized) {
                bool isInitialized = true;
                for(int i = 0; i < DestructableVoxels.Length; i++) {
                    if(!DestructableVoxels[i].VoxelsContainer.IsInitialized) {
                        isInitialized = false;
                        break;
                    }
                }
                if(isInitialized) {
                    IsInitialized = true;
                    break;
                }
                yield return null;
            }
        }
    }
}
