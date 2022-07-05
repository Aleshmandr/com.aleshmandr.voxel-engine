namespace VoxelEngine.Delaunay
{
    public enum OrientationType : byte
    {
        None,
        Left,
        Right
    }

    public static class OrientationTypeExtensions
    {
        public static OrientationType Opposite(this OrientationType orientation) {
            return orientation == OrientationType.Left ? OrientationType.Right : OrientationType.Left;
        }
    }
}
