namespace VoxelEngine.Delaunay
{
    public class EdgeList
    {
        private float deltaX;
        private float xmin;

        private int hashSize;
        private Halfedge[] hash;
        private Halfedge leftEnd;
        private Halfedge rightEnd;
        
        public Halfedge LeftEnd => leftEnd;
        public Halfedge RightEnd => rightEnd;

        public void Dispose() {
            Halfedge halfedge = leftEnd;
            Halfedge prevHe;
            while(halfedge != rightEnd) {
                prevHe = halfedge;
                halfedge = halfedge.EdgeListRightNeighbor;
                prevHe.Dispose();
            }
            leftEnd = null;
            rightEnd.Dispose();
            rightEnd = null;

            hash = null;
        }

        public EdgeList(float xmin, float deltaX, int sqrtSitesNb) {
            this.xmin = xmin;
            this.deltaX = deltaX;
            hashSize = 2 * sqrtSitesNb;

            hash = new Halfedge[hashSize];

            // Two dummy Halfedges:
            leftEnd = Halfedge.CreateDummy();
            rightEnd = Halfedge.CreateDummy();
            leftEnd.EdgeListLeftNeighbor = null;
            leftEnd.EdgeListRightNeighbor = rightEnd;
            rightEnd.EdgeListLeftNeighbor = leftEnd;
            rightEnd.EdgeListRightNeighbor = null;
            hash[0] = leftEnd;
            hash[hashSize - 1] = rightEnd;
        }

        /*
         * Insert newHalfedge to the right of lb
         * @param lb
         * @param newHalfedge
         */
        public void Insert(Halfedge lb, Halfedge newHalfedge) {
            newHalfedge.EdgeListLeftNeighbor = lb;
            newHalfedge.EdgeListRightNeighbor = lb.EdgeListRightNeighbor;
            lb.EdgeListRightNeighbor.EdgeListLeftNeighbor = newHalfedge;
            lb.EdgeListRightNeighbor = newHalfedge;
        }

        /*
         * This function only removes the Halfedge from the left-right list.
         * We cannot dispose it yet because we are still using it.
         * @param halfEdge
         */
        public void Remove(Halfedge halfedge) {
            halfedge.EdgeListLeftNeighbor.EdgeListRightNeighbor = halfedge.EdgeListRightNeighbor;
            halfedge.EdgeListRightNeighbor.EdgeListLeftNeighbor = halfedge.EdgeListLeftNeighbor;
            halfedge.Edge = Edge.DELETED;
            halfedge.EdgeListLeftNeighbor = halfedge.EdgeListRightNeighbor = null;
        }

        /*
         * Find the rightmost Halfedge that is still elft of p
         * @param p
         * @return
         */
        public Halfedge EdgeListLeftNeighbor(Vector2f p) {
            int bucket;
            Halfedge halfedge;

            // Use hash table to get close to desired halfedge
            bucket = (int)((p.x - xmin) / deltaX * hashSize);
            if(bucket < 0) {
                bucket = 0;
            }
            if(bucket >= hashSize) {
                bucket = hashSize - 1;
            }
            halfedge = GetHash(bucket);
            if(halfedge == null) {
                for(int i = 0; true; i++) {
                    if((halfedge = GetHash(bucket - i)) != null) break;
                    if((halfedge = GetHash(bucket + i)) != null) break;
                }
            }

            // Now search linear list of haledges for the correct one
            if(halfedge == leftEnd || (halfedge != rightEnd && halfedge.IsLeftOf(p))) {
                do {
                    halfedge = halfedge.EdgeListRightNeighbor;
                } while(halfedge != rightEnd && halfedge.IsLeftOf(p));
                halfedge = halfedge.EdgeListLeftNeighbor;

            } else {
                do {
                    halfedge = halfedge.EdgeListLeftNeighbor;
                } while(halfedge != leftEnd && !halfedge.IsLeftOf(p));
            }

            // Update hash table and reference counts
            if(bucket > 0 && bucket < hashSize - 1) {
                hash[bucket] = halfedge;
            }
            return halfedge;
        }

        // Get entry from the has table, pruning any deleted nodes
        private Halfedge GetHash(int b) {
            if(b < 0 || b >= hashSize) {
                return null;
            }

            var halfedge = hash[b];
            if(halfedge != null && halfedge.Edge == Edge.DELETED) {
                // Hash table points to deleted halfedge. Patch as necessary
                hash[b] = null;

                // Still can't dispose halfedge yet!
                return null;
            }
            return halfedge;
        }
    }
}
