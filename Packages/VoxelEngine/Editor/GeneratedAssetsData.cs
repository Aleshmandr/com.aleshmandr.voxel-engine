using UnityEngine;

namespace VoxelEngine.Editor
{
    public class GeneratedAssetsData
    {
        public readonly string OriginalAssetName;
        public readonly string AssetName;
        public readonly Mesh MeshAsset;
        public readonly TextAsset DataAsset;
        
        public GeneratedAssetsData(string originalAssetName, string assetName, Mesh meshAsset, TextAsset dataAsset) {
            OriginalAssetName = originalAssetName;
            AssetName = assetName;
            MeshAsset = meshAsset;
            DataAsset = dataAsset;
        }
    }
}
