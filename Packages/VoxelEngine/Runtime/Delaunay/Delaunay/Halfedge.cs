using System.Collections.Generic;

namespace VoxelEngine.Delaunay {

	public class Halfedge {

		#region Pool
		private static readonly Queue<Halfedge> Pool = new Queue<Halfedge>();

		public static Halfedge Create(Edge edge, OrientationType lr) {
			if (Pool.Count > 0) {
				return Pool.Dequeue().Init(edge,lr);
			}
			return new Halfedge(edge,lr);
		}
		public static Halfedge CreateDummy() {
			return Create(null, OrientationType.None);
		}
		#endregion

		#region Object
		public Halfedge EdgeListLeftNeighbor;
		public Halfedge EdgeListRightNeighbor;
		public Halfedge NextInPriorityQueue;

		public Edge Edge;
		public OrientationType Orientation;
		public Vertex Vertex;

		// The vertex's y-coordinate in the transformed Voronoi space V
		public float ystar;

		public Halfedge(Edge edge, OrientationType orientation) {
			Init(edge, orientation);
		}

		private Halfedge Init(Edge edge, OrientationType orientation) {
			Edge = edge;
			Orientation = orientation;
			NextInPriorityQueue = null;
			Vertex = null;

			return this;
		}

		public override string ToString() {
			return "Halfedge (LeftRight: " + Orientation + "; vertex: " + Vertex + ")";
		}

		public void Dispose() {
			if (EdgeListLeftNeighbor != null || EdgeListRightNeighbor != null) {
				// still in EdgeList
				return;
			}
			if (NextInPriorityQueue != null) {
				// still in PriorityQueue
				return;
			}
			Edge = null;
			Orientation = OrientationType.None;
			Vertex = null;
			Pool.Enqueue(this);
		}

		public void ReallyDispose() {
			EdgeListLeftNeighbor = null;
			EdgeListRightNeighbor = null;
			NextInPriorityQueue = null;
			Edge = null;
			Orientation = OrientationType.None;
			Vertex = null;
			Pool.Enqueue(this);
		}

		public bool IsLeftOf(Vector2f p) {
			Site topSite;
			bool rightOfSite, above, fast;
			float dxp, dyp, dxs, t1, t2, t3, y1;

			topSite = Edge.RightSite;
			rightOfSite = p.x > topSite.x;
			if (rightOfSite && this.Orientation == OrientationType.Left) {
				return true;
			}
			if (!rightOfSite && this.Orientation == OrientationType.Right) {
				return false;
			}

			if (Edge.a == 1) {
				dyp = p.y - topSite.y;
				dxp = p.x - topSite.x;
				fast = false;
				if ((!rightOfSite && Edge.b < 0) || (rightOfSite && Edge.b >= 0)) {
					above = dyp >= Edge.b * dxp;
					fast = above;
				} else {
					above = p.x + p.y * Edge.b > Edge.c;
					if (Edge.b < 0) {
						above = !above;
					} 
					if (!above) {
						fast = true;
					}
				}
				if (!fast) {
					dxs = topSite.x - Edge.LeftSite.x;
					above = Edge.b * (dxp * dxp - dyp * dyp) < dxs * dyp * (1+2 * dxp/dxs + Edge.b * Edge.b);
					if (Edge.b < 0) {
						above = !above;
					}
				}
			} else {
				y1 = Edge.c - Edge.a * p.x;
				t1 = p.y - y1;
				t2 = p.x - topSite.x;
				t3 = y1 - topSite.y;
				above = t1 * t1 > t2 * t2 + t3 * t3;
			}
			return this.Orientation == OrientationType.Left ? above : !above;
		}
		#endregion
	}
}
