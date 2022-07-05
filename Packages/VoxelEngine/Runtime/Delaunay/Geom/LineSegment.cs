using System.Collections.Generic;

namespace VoxelEngine.Delaunay
{
    public class LineSegment
    {
        public Vector2f StartPoint;
        public Vector2f EndPoint;

        public static List<LineSegment> VisibleLineSegments(List<Edge> edges) {
            List<LineSegment> segments = new List<LineSegment>();

            foreach(Edge edge in edges) {
                if(edge.Visible()) {
                    Vector2f p1 = edge.ClippedEnds[OrientationType.Left];
                    Vector2f p2 = edge.ClippedEnds[OrientationType.Right];
                    segments.Add(new LineSegment(p1, p2));
                }
            }

            return segments;
        }

        public static float CompareLengthsMax(LineSegment segment0, LineSegment segment1) {
            float length0 = (segment0.StartPoint - segment0.EndPoint).magnitude;
            float length1 = (segment1.StartPoint - segment1.EndPoint).magnitude;
            if(length0 < length1) {
                return 1;
            }
            if(length0 > length1) {
                return -1;
            }
            return 0;
        }

        public static float CompareLengths(LineSegment edge0, LineSegment edge1) {
            return -CompareLengthsMax(edge0, edge1);
        }

        public LineSegment(Vector2f startPoint, Vector2f endPoint) {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
    }
}
