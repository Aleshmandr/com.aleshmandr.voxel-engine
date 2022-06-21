namespace VoxelEngine.Delaunay
{

	public struct Rectf
	{
		public static readonly Rectf Zero = new Rectf(0, 0, 0, 0);
		public static readonly Rectf One = new Rectf(1, 1, 1, 1);

		public readonly float x;
		public readonly float y;
		public readonly float width;
		public readonly float height;

		public Rectf(float x, float y, float width, float height) {
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public float left => x;

		public float right => x + width;

		public float top => y;

		public float bottom => y + height;

		public Vector2f topLeft => new Vector2f(left, top);

		public Vector2f bottomRight => new Vector2f(right, bottom);
	}
}