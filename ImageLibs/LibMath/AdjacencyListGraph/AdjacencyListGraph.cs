//------------------------------------------------------------------------------
// <copyright from='2002' to='2004' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // Debug functionalities.
using System.Collections;

//FUTURE-2005/03/02-MingYe -- IGraph interface for different graph classes.
namespace System.Windows.Ink.Analysis.MathLibrary.Graph
{
    using VertexId = System.Int32;

    /// <summary>
    /// A class representing an undirected weighted graph. Uses an adjacency list implementation.
    /// </summary>
    /// <remarks>Designed for building a Minimum Spanning Tree of lines in the initial step
    /// of block grouping. Can be generalized for other needs in the future.</remarks>
    internal class AdjacencyListGraph
    {
        // Use Hashtable instead of Arraylist for constant-time vertex lookup.
        private Hashtable _vertices;

        // For vertex-based graph traversal. Use int instead of bool visted flag to avoid
        // resetting the flags. The flag increases by one after each visit. Works even
        // if the flag overflows.
        private int _lastVisited;

        public Hashtable Vertices
        {
            get { return this._vertices; }
        }


        // All undirected edges represented as neighboring Vertex pairs.
        // Each UndirectedEdge.Vertex1 has a smaller Id than Vertex2
        public UndirectedEdge[] UndirectedEdges
        {
            get 
            {
                IDictionaryEnumerator enumerator = this._vertices.GetEnumerator();
                ArrayList edges = new ArrayList(this.NumVertices * this.NumVertices / 2);

                while (enumerator.MoveNext())
                {
                    Vertex vertex = (Vertex)enumerator.Value;
                    for (int neighborIndex = 0; neighborIndex < vertex.NumNeighbors; neighborIndex++)
                    {
                        EdgeToNeighbor edgeToNeighbor = (EdgeToNeighbor)vertex.Edges[neighborIndex];
                        if (vertex.Id < edgeToNeighbor.Neighbor.Id)
                        {
                            UndirectedEdge edge = new UndirectedEdge(vertex, edgeToNeighbor);
                            edges.Add(edge);
                        }
                    }
                }

                return (UndirectedEdge[])edges.ToArray(typeof(UndirectedEdge));
            }
        }

        public int NumVertices
        {
            get { return this._vertices.Count; }
        }

        public AdjacencyListGraph(int numVertices)
        {
            this._vertices = new Hashtable(numVertices);
        }

        public void AddVertex(Vertex v)
        {
            if (!this.Contains(v))
            {
                this._vertices.Add(v.Id, v);
            }
        }

        public void AddVertex(VertexId id)
        {
            Vertex vertex = new Vertex(id);
            this.AddVertex(vertex);
        }
        public bool Contains(Vertex vertex)
        {
            return this.Contains(vertex.Id);
        }
        public bool Contains(VertexId id)
        {
            return this._vertices.ContainsKey(id);
        }
#if INTERNAL_DPU
        public bool IsVertexVisited(Vertex v)
        {
            return this.Contains(v) && v.Flag == this._lastVisited;
        }
        private bool IsValidVertexPair(Vertex v1, Vertex v2)
        {
            return this.IsValidVertexPair(v1.Id, v2.Id);
        }
#endif // INTERNAL_DPU

        private bool IsValidVertexPair(VertexId id1, VertexId id2)
        {
            Debug.Assert(id1 != id2,
                "v1 and v2 are the same vertex.");

            Debug.Assert(this.Contains(id1) && this.Contains(id2),
                "At least one of v1 and v2 does not exist in the graph.");

            return id1 != id2 && this.Contains(id1) && this.Contains(id2);
        }

        public bool AreNeighbors(Vertex v1, Vertex v2)
        {
            return this.AreNeighbors(v1.Id, v2.Id);
        }
        
        public bool AreNeighbors(VertexId id1, VertexId id2)
        {
            return this.IsValidVertexPair(id1, id2) &&
                ((Vertex)this._vertices[id1]).IsNeighboring((Vertex)this._vertices[id2]);
        }

        // Add a directed edge v1 -> v2
        public void AddDirectedEdge(Vertex v1, Vertex v2, double cost)
        {
            if (!AreNeighbors(v1, v2))
            {
                v1.AddNeighbor(new EdgeToNeighbor(v2, cost));
            }
        }

#if INTERNAL_DPU
        public void AddDirectedEdge(Vertex v1, Vertex v2)
        {
            AddDirectedEdge(v1, v2, 0);
        }
        public void AddDirectedEdge(VertexId id1, VertexId id2, double cost)
        {
            AddDirectedEdge((Vertex)this._vertices[id1], (Vertex)this._vertices[id2], cost);
        }
#endif // INTERNAL_DPU

        public void AddDirectedEdge(VertexId id1, VertexId id2)
        {
            AddDirectedEdge((Vertex)this._vertices[id1], (Vertex)this._vertices[id2], 0);
        }
        
        public static void SetUndirectedEdgeWeights(Vertex v1, Vertex v2, double cost)
        {
            int indexOfNeighbor = v1.IndexOfNeighbor(v2);
            if (indexOfNeighbor != -1)
            {
                ((EdgeToNeighbor)v1.Edges[indexOfNeighbor]).Cost = cost;

                indexOfNeighbor = v2.IndexOfNeighbor(v1);
                ((EdgeToNeighbor)v2.Edges[indexOfNeighbor]).Cost = cost;
            }
        }

        public static void RemoveUndirectedEdge(Vertex v1, Vertex v2)
        {
            v1.RemoveNeighbor(v2);
            v2.RemoveNeighbor(v1);
        }

        // Find a Minimum Spanning Tree of the Graph (there may exist multiple solutions).
        // Return all edges in the MST sorted by their costs.
        // Kruskal’s algorithm: keep adding the cheapest edge to the tree which does not cause
        // cycles in the graph until n-1 edges are chosen where n is the number of vertices
        // in the connected graph.
        // Reference: http://ciips.ee.uwa.edu.au/~morris/Year2/PLDS210/mst.html
        public UndirectedEdge[] FindMinimumSpanningTree()
        {
            // The Kruskal's algorithm creates a partition of the vertex set at each of its
            // iteration. Initially, every vertex is a set by itself. Adding an edge unions
            // the two partitions of the end vertices. We may define a representative
            // for each set, e.g., any vertex within the set. On unioning two sets, we
            // update the representative. If an edge causes a cycle, its end vertices should
            // already be in the same set and have the same representative. Therefore,
            // detecting cycles amounts to comparing the representatives of the vertices.
            
            // Initialize partitions: each vertex being a partition itself.
            // A partition is an ArrayList of vertices. The representative of a partition is
            // the first item in the ArrayList.
            ArrayList[] partitions = new ArrayList[this.NumVertices];
            IDictionaryEnumerator enumerator = this.Vertices.GetEnumerator();
            int i =0;
            while (enumerator.MoveNext())
            {
                Vertex v = (Vertex)enumerator.Value;
                // Create a partition for each vertex
                partitions[i] = new ArrayList(this.NumVertices);
                partitions[i].Add(v);
                // Let each vertex point to its partition
                v.Partition = partitions[i];
                i++;
            }

            // Get all undirected edges in the graph
            UndirectedEdge[] edges = this.UndirectedEdges;

            // Sort the edges by their costs
            Array.Sort(edges);

            // Determine the number of edges nE in the MST as a function of the number
            // of disjoint sub-graphs nG in the current graph of size nV:
            // nE = nV - nG.
            int numEdgesToAdd = this.NumVertices - this.GetNumConnectedComponents();

            ArrayList treeEdges = new ArrayList(edges.Length);

            for (int nextCheapestEdgeIndex = 0;
                numEdgesToAdd > 0;
                --numEdgesToAdd, ++nextCheapestEdgeIndex)
            {
                // Find the next cheapest edge
                UndirectedEdge nextCheapestEdge = edges[nextCheapestEdgeIndex];
                while (nextCheapestEdge.Vertex1.Partition ==
                    nextCheapestEdge.Vertex2.Partition)
                {
                    ++nextCheapestEdgeIndex;
                    nextCheapestEdge = edges[nextCheapestEdgeIndex];
                }

                // Add this edge to the treeEdges list
                treeEdges.Add(nextCheapestEdge);

                // Union the partitions of the two vertices:
                    
                // Append vertex2's partition to vertex1's partition.
                nextCheapestEdge.Vertex1.Partition.AddRange(nextCheapestEdge.Vertex2.Partition);
                // Make vertices in the previous Vertex2 set point to the new partition
                for (i = 0; i < nextCheapestEdge.Vertex2.Partition.Count; i++)
                {
                    ((Vertex)nextCheapestEdge.Vertex2.Partition[i]).Partition =
                        nextCheapestEdge.Vertex1.Partition;
                }
                // nextCheapestEdge.Vertex2.Partition is not used from this point on.

                Debug.Assert(nextCheapestEdgeIndex <= edges.Length,
                    "The MST edges should be a subset of graph edges.");
            }

            return (MathLibrary.Graph.UndirectedEdge[])
                treeEdges.ToArray(typeof(MathLibrary.Graph.UndirectedEdge));
        }

        private void IncrLastVisited()
        {
            ++this._lastVisited;
        }
        
        public int GetNumConnectedComponents()
        {
            ArrayList connectedComponents = this.GetConnectedComponents();
            return connectedComponents.Count;
        }

        /// <summary>
        /// Returns all connected components in the graph.
        /// </summary>
        /// <returns>An ArrayList of Vertex[], each containing the vertices in a connected component.</returns>
        public ArrayList GetConnectedComponents()
        {
            // Reset the last-visited flag
            this.IncrLastVisited();

            ArrayList connectedComponents = new ArrayList(this.NumVertices);
            
            for (;;)
            {
                object nextUnvisitedVertex = GetNextUnvisitedVertex();
                if (nextUnvisitedVertex == null)
                {
                    // All vertices have been visited: stop
                    break;
                }

                // There exists at least one vertex outside all existing connected components.
                // Get its connected component.
                Vertex[] traversedVertices = TraverseVerticesFrom((Vertex)nextUnvisitedVertex);
                
                connectedComponents.Add(traversedVertices);
            }
            // Now all vertices are visited (Flag = true)

            return connectedComponents;
        }

        private object GetNextUnvisitedVertex()
        {
            IDictionaryEnumerator enumerator = this.Vertices.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Vertex v = (Vertex)enumerator.Value;
                if (v.Flag != this._lastVisited)
                {
                    return v;
                }
            }
            return null;
        }


        // Get the connected component that the input vertex belongs to and return all
        // vertices in this component including the input vertex.
        private Vertex[] TraverseVerticesFrom(Vertex vertex)
        {
            Debug.Assert(this.Contains(vertex) && vertex.Flag != this._lastVisited,
                "The starting point of the traversal must be a valid unvisted vertex.");

            ArrayList traversedVertices = new ArrayList(this.NumVertices);

            ArrayList verticesToProcess = new ArrayList(this.NumVertices);
            verticesToProcess.Add(vertex);

            while (verticesToProcess.Count > 0)
            {
                // Process from the end instead of the beginning of the array list to avoid
                // the shifting when removing an item
                int indexToProcess = verticesToProcess.Count - 1;

                // Check the next vertex in the ToProcess list
                Vertex curVertex = (Vertex)verticesToProcess[indexToProcess];

                // Mark it as visited
                curVertex.Flag = this._lastVisited;

                // Save it to the traversedVertices list
                traversedVertices.Add(curVertex);

                // Remove it from the ToProcess list
                verticesToProcess.RemoveAt(indexToProcess);

                // Check all its neighbors:
                for (int i = 0; i < curVertex.NumNeighbors; i++)
                {
                    Vertex neighbor = ((EdgeToNeighbor)curVertex.Edges[i]).Neighbor;
                    if (neighbor.Flag != this._lastVisited)
                    {
                        // Found an unvisited neighbor. Mark as visited
                        // and add it to the ToProcess list.
                        neighbor.Flag = this._lastVisited;
                        verticesToProcess.Add(neighbor);
                    }
                }
            }

            return (Vertex[])traversedVertices.ToArray(typeof(Vertex));
        }

        public void RemoveHighCostEdges(double costThreshold)
        {
            UndirectedEdge[] allEdges = this.UndirectedEdges;

            for (int i = 0; i < allEdges.Length; ++i)
            {
                if (allEdges[i].Cost >= costThreshold)
                {
                    RemoveUndirectedEdge(allEdges[i].Vertex1, allEdges[i].Vertex2);
                }
            }
        }

        public void KeepInputEdgesOnly(UndirectedEdge[] edgesToKeep)
        {
            // Increase the last visited value of the graph. During traversal, set
            // visited edges' flags to this value.
            this.IncrLastVisited();

            // Set edgesToKeep's flags to graph's last visited value.
            for (int i = 0; i < edgesToKeep.Length; ++i)
            {
                this.MarkUndirectedEdgeAsVisited(
                    edgesToKeep[i].Vertex1,
                    edgesToKeep[i].Vertex2);
            }

            MathLibrary.Graph.UndirectedEdge[] allEdges = this.UndirectedEdges;
            // Remove from lineNeighborhoodGraph all edges that are not visited.
            for (int i = 0; i < allEdges.Length; ++i)
            {
                if (!this.IsUndirectedEdgeVisited(allEdges[i]))
                {
                    RemoveUndirectedEdge(allEdges[i].Vertex1, allEdges[i].Vertex2);
                }
            }
        }

        private void MarkUndirectedEdgeAsVisited(Vertex v1, Vertex v2)
        {
            int indexOfNeighbor = v1.IndexOfNeighbor(v2);
            if (indexOfNeighbor != -1)
            {
                ((EdgeToNeighbor)v1.Edges[indexOfNeighbor]).Flag = this._lastVisited;

                indexOfNeighbor = v2.IndexOfNeighbor(v1);
                ((EdgeToNeighbor)v2.Edges[indexOfNeighbor]).Flag = this._lastVisited;
            }
        }

        private bool IsUndirectedEdgeVisited(UndirectedEdge e)
        {
            Vertex v1 = e.Vertex1;
            Vertex v2 = e.Vertex2;
            int indexOfNeighbor = v1.IndexOfNeighbor(v2);
            if (indexOfNeighbor != -1)
            {
                if (((EdgeToNeighbor)v1.Edges[indexOfNeighbor]).Flag != this._lastVisited)
                {
                    return false;
                }

                indexOfNeighbor = v2.IndexOfNeighbor(v1);
                if (((EdgeToNeighbor)v2.Edges[indexOfNeighbor]).Flag != this._lastVisited)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// A class representing a directed weighted edge in a graph.
    /// </summary>
    internal class EdgeToNeighbor : IComparable
    {
        private Vertex _neighbor;
        private double _cost;
        private object _data; // for storing generic edge data
        // To support various operations such as edge traversal
        private int _flag;

        // An invalid edge with an infinite cost.
        public static readonly EdgeToNeighbor Null = new EdgeToNeighbor(null, Double.PositiveInfinity);

        public Vertex Neighbor
        {
            get { return this._neighbor; }
        }

        public double Cost
        {
            get { return this._cost; }
            set { this._cost = value; }
        }
        public object Data
        {
            get { return this._data; }
            set { this._data = value; }
        }

        public int Flag
        {
            get { return this._flag; }
            set { this._flag = value; }
        }

        public EdgeToNeighbor(Vertex neighbor, double cost) :
            this(neighbor, cost, null)
        {
        }

        public EdgeToNeighbor(Vertex neighbor, double cost, object data)
        {
            this._neighbor = neighbor;
            this._cost = cost;
            this._data = data;
        }
        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            EdgeToNeighbor other = obj as EdgeToNeighbor;
            Debug.Assert(other != null, "Comparing incompatible object");
            return this.Cost.CompareTo(other.Cost);
        }

        #endregion
    }

    internal class UndirectedEdge : IComparable
    {
        private Vertex _vertex;
        private EdgeToNeighbor _edgeToNeighbor;

        public Vertex Vertex1
        {
            get { return this._vertex; }
        }
        public Vertex Vertex2
        {
            get { return this._edgeToNeighbor.Neighbor; }
        }
        public double Cost
        {
            get { return this._edgeToNeighbor.Cost; }
        }
        public object Data
        {
            get { return this._edgeToNeighbor.Data; }
            set { this._edgeToNeighbor.Data = value; }
        }

        public UndirectedEdge(Vertex vertex, EdgeToNeighbor edge) :
            this(vertex, edge, null)
        {
        }
            
        public UndirectedEdge(Vertex vertex, EdgeToNeighbor edge, object data)
        {
            Debug.Assert(vertex.Id != edge.Neighbor.Id,
                "An edge should connect two distinct vertices");

            if (vertex.Id < edge.Neighbor.Id)
            {
                this._vertex = vertex;
                this._edgeToNeighbor = edge;
            }
            else
            {
                Debug.Assert(true,
                    "Should never reach here in all current usage.");

                this._vertex = edge.Neighbor;
                this._edgeToNeighbor = new EdgeToNeighbor(vertex, edge.Cost, data);
            }
        }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            UndirectedEdge other = obj as UndirectedEdge;
            Debug.Assert(other != null, "Comparing incompatible object");
            return this.Cost.CompareTo(other.Cost);
        }

        #endregion
    }

    /// <summary>
    /// A class representing the vertex in a graph.
    /// </summary>
    internal class Vertex
    {
        private VertexId _id;
        // An array list of Edge connecting this vertex to its neighboring vertices.
        private ArrayList _adjacencyList;

        // To support various operations such as vertex traversal
        private int _flag;

        // For the Union-Find algorithm in detecting cycles in graphs.
        //FUTURE-2005/03/24-MingYe -- Not a natural member of Vertex.
        // Consider making MST a class and Vertex.Partition a member of that class.
        private ArrayList _partition;

        public VertexId Id
        {
            get { return this._id; }
        }

        public ArrayList Edges
        {
            get { return this._adjacencyList; }
        }

        public int NumNeighbors
        {
            get { return this._adjacencyList.Count; }
        }

        public int Flag
        {
            get { return this._flag; }
            set { this._flag = value; }
        }

        public ArrayList Partition
        {
            get { return this._partition; }
            set { this._partition = value; }
        }

        public VertexId[] NeighborIds
        {
            get
            {
                VertexId[] neighborIds = new VertexId[this.NumNeighbors];
                for (int i = 0; i < this.NumNeighbors; ++i)
                {
                    neighborIds[i] = ((EdgeToNeighbor)this.Edges[i]).Neighbor.Id;
                }
                return neighborIds;
            }
        }

        #region Object and Operator Overrides
        public override int GetHashCode()
        {
            return this._id;
        }

        public override bool Equals(object obj)
        {
            Vertex other = obj as Vertex;
            return other != null && this == other;
        }

        public static bool operator==(Vertex u, Vertex v)
        {
            return u.Id == v.Id;
        }

        public static bool operator!=(Vertex u, Vertex v)
        {
            return !(u == v);
        }
        #endregion Object and Operator Overrides

        public Vertex(VertexId id)
        {
            this._id = id;
            this._adjacencyList = new ArrayList(16);
        }
    
        public void AddNeighbor(EdgeToNeighbor edge)
        {
            this._adjacencyList.Add(edge);
        }

        public bool IsNeighboring(Vertex other)
        {
            return IndexOfNeighbor(other) != -1;
        }

        // If other is a neighbor of this vertex, return its index in the AdjacencyList
        // ArrayList. Otherwise return -1.
        public int IndexOfNeighbor(Vertex other)
        {
            for (int i = 0; i < this.NumNeighbors; ++i)
            {
                if (other == ((EdgeToNeighbor)this.Edges[i]).Neighbor)
                {
                    return i;
                }
            }
            return -1;
        }

        public void RemoveNeighbor(Vertex other)
        {
            int index = this.IndexOfNeighbor(other);
            if (index != -1)
            {
                this._adjacencyList.RemoveAt(index);
            }
        }

    }
}
