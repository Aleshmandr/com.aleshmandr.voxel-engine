namespace VoxelEngine.Delaunay
{
    public struct Rectf
    {
        public static readonly Rectf Zero = new Rectf(0, 0, 0, 0);
        public static readonly Rectf One = new Rectf(1, 1, 1, 1);

        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Left => X;

        public float Right => X + Width;

        public float Top => Y;

        public float Bottom => Y + Height;

        public Vector2f TopLeft => new Vector2f(Left, Top);

        public Vector2f BottomRight => new Vector2f(Right, Bottom);

        public Rectf(float x, float y, float width, float height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
