namespace VoxelEngine.Delaunay {

	// Also know as heap
	public class HalfedgePriorityQueue {

		private Halfedge[] hash;
		private int count;
		private int minBucked;
		private int hashSize;

		private float ymin;
		private float deltaY;

		public HalfedgePriorityQueue(float ymin, float deltaY, int sqrtSitesNb) {
			this.ymin = ymin;
			this.deltaY = deltaY;
			hashSize = 4 * sqrtSitesNb;
			Init();
		}

		public void Dispose() {
			// Get rid of dummies
			for (int i = 0; i < hashSize; i++) {
				hash[i].Dispose();
			}
			hash = null;
		}

		public void Init() {
			count = 0;
			minBucked = 0;
			hash = new Halfedge[hashSize];
			// Dummy Halfedge at the top of each hash
			for (int i = 0; i < hashSize; i++) {
				hash[i] = Halfedge.CreateDummy();
				hash[i].NextInPriorityQueue = null;
			}
		}

		public void Insert(Halfedge halfedge) {
			Halfedge previous, next;

			int insertionBucket = Bucket(halfedge);
			if (insertionBucket < minBucked) {
				minBucked = insertionBucket;
			}
			previous = hash[insertionBucket];
			while ((next = previous.NextInPriorityQueue) != null &&
			       (halfedge.ystar > next.ystar || (halfedge.ystar == next.ystar && halfedge.Vertex.x > next.Vertex.x))) {
				previous = next;
			}
			halfedge.NextInPriorityQueue = previous.NextInPriorityQueue;
			previous.NextInPriorityQueue = halfedge;
			count++;
		}

		public void Remove(Halfedge halfedge) {
			Halfedge previous;
			int removalBucket = Bucket(halfedge);

			if (halfedge.Vertex != null) {
				previous = hash[removalBucket];
				while (previous.NextInPriorityQueue != halfedge) {
					previous = previous.NextInPriorityQueue;
				}
				previous.NextInPriorityQueue = halfedge.NextInPriorityQueue;
				count--;
				halfedge.Vertex = null;
				halfedge.NextInPriorityQueue = null;
				halfedge.Dispose();
			}
		}

		private int Bucket(Halfedge halfedge) {
			int theBucket = (int)((halfedge.ystar - ymin)/deltaY * hashSize);
			if (theBucket < 0) theBucket = 0;
			if (theBucket >= hashSize) theBucket = hashSize - 1;
			return theBucket;
		}

		private bool IsEmpty(int bucket) {
			return (hash[bucket].NextInPriorityQueue == null);
		}

		/*
		 * move minBucket until it contains an actual Halfedge (not just the dummy at the top);
		 */
		private void AdjustMinBucket() {
			while (minBucked < hashSize - 1 && IsEmpty(minBucked)) {
				minBucked++;
			}
		}

		public bool Empty() {
			return count == 0;
		}

		/*
		 * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
		 */
		public Vector2f Min() {
			AdjustMinBucket();
			Halfedge answer = hash[minBucked].NextInPriorityQueue;
			return new Vector2f(answer.Vertex.x, answer.ystar);
		}

		/*
		 * Remove and return the min Halfedge
		 */
		public Halfedge ExtractMin() {
			Halfedge answer;

			// Get the first real Halfedge in minBucket
			answer = hash[minBucked].NextInPriorityQueue;

			hash[minBucked].NextInPriorityQueue = answer.NextInPriorityQueue;
			count--;
			answer.NextInPriorityQueue = null;

			return answer;
		}
	}
}