namespace VoxelEngine.Delaunay
{
    public class Circle
    {
        public readonly Vector2f Center;
        public readonly float Radius;

        public Circle(float centerX, float centerY, float radius) {
            Center = new Vector2f(centerX, centerY);
            Radius = radius;
        }

        public override string ToString() {
            return "Circle (center: " + Center + "; radius: " + Radius + ")";
        }
    }
}
