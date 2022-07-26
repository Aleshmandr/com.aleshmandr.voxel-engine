using UnityEngine;

namespace VoxelEngine.Editor
{
    public class GeneratedAssetsData
    {
        public readonly string OriginalAssetName;
        public readonly string AssetName;
        public readonly Mesh MeshAsset;
        public readonly TextAsset DataAsset;
        public readonly Vector3 Offset;
        
        public GeneratedAssetsData(string originalAssetName, string assetName, Mesh meshAsset, TextAsset dataAsset, Vector3 offset) {
            OriginalAssetName = originalAssetName;
            AssetName = assetName;
            MeshAsset = meshAsset;
            DataAsset = dataAsset;
            Offset = offset;
        }
    }
}
