using System;
using System.Collections;

namespace Dpu.Utility
{
    /// <summary>
    /// Implement the union-find algorithm
    /// </summary>
    public class UnionFind
    {
        int[] _parents;
        int[] _ranks;
        /// <summary>
        /// Perform UF on id's 0 => N-1.
        /// </summary>
        public UnionFind(int n)
        {
            _parents = new int[n];
            for(int i = 0; i < n; i++)
            {
                _parents[i] = i;
            }
            _ranks = new int[n];
            for(int i = 0; i < _ranks.Length; i++)
            {
                _ranks[i] = 0;
            }
        }
        /// <summary>
        /// Collapse all the parent chains to a direct pointer.
        /// MMS: I don't think this is really necessary
        /// </summary>
        public void Pack()
        {
            for(int i = 0; i < _parents.Length; i++)
            {
                _parents[i] = Find(i);
            }
        }

        /// <summary>
        /// Return set of equivalence classes
        /// </summary>
        public int[][] Equivalents()
        {
            Hashtable map = new Hashtable();
            for(int i = 0; i < _parents.Length; i++)
            {
                int root = Find(i);
                ArrayList eq = (ArrayList)map[root];
                if(eq == null)
                {
                    eq = new ArrayList();
                    map[root] = eq;
                }
                eq.Add(i);
            }
            int[][] eqs = new int[map.Values.Count][];
            int index = 0;
            foreach(ArrayList eq in map.Values)
            {
                eqs[index] = (int[])eq.ToArray(typeof(int));
                index++;
            }
            return eqs;
        }

        /// <summary>
        /// Find of the union-find algorithm
        /// See Introduction to Algorithms by Cormen, Leiserson and Rivest (pp 448)
        /// </summary>
        public int Find(int x)
        {
            // recursive way (no inline please)
            // if(x != parents[x]) {
            //     parents[x] = uf_find(parents[x], parents);
            // }
            // return parents[x];

            // non recursive way (faster with inline)
            int p = x;
            while(p != _parents[p])
            {
                p = _parents[p]; // follow chain to parent
            }
            while(x != _parents[x])
            {
                int oldX = x;
                x = _parents[x]; // follow chain to parent
                _parents[oldX] = p; // udpate the chain
            }
            return p;
        }

        /// <summary>
        /// Union of the union-find algorithm
        /// See Introduction to Algorithms by Cormen, Leiserson and Rivest (pp 448)
        /// </summary>
        public void Union(int x1, int x2)
        {
            int x = Find(x1);
            int y = Find(x2);

            if(_ranks[x] > _ranks[y])
            {
                _parents[y] = x;
            }
            else
            {
                _parents[x] = y;
                if(_ranks[x] == _ranks[y])
                {
                    _ranks[y] += 1;
                }
            }
        }

		public bool IsConnected(int x1, int x2)
		{
			return Find(x1) == Find(x2);
		}
    }
}
