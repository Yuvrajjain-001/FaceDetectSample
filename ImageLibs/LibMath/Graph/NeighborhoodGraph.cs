//------------------------------------------------------------------------------
// <copyright from='2002' to='2004' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // For Debug class.
using System.Collections;   // For hashtable.
using System.Globalization;

namespace System.Windows.Ink.Analysis.MathLibrary
{
    /// Overview:
    /// Neighborhood graph is a class to provide methods to access neighborhood relationship.
    /// A neighborhood graph B can be constructed out of another neighborhood graph A where 
    /// a connected vertices in A forms a connected component in B (e.g. we can make neighborhood
    /// of lines from neighborhood graph of words).
    /// Thus, it is easy to compose a neighborhood graph from a finer neighborhood graph.
    /// The term "Vertex" is used to denoted a single connected component in the refine neighborhood graph.
    /// The term "Component" is used to denote a connected component on the composed graph.
    /// 
    /// This file contains set of neighborhood graph classes:
    /// 1. ComponentMapping: provide interface to map from a finer neighborhood graph to coarser neighborhood
    ///    graph and vice versa.
    /// 2. NeighborInfo: a simple struct to store neighboring component and distance.
    /// 3. NeighborhoodGraph: a base class providing all necessary interface to access neighborhood 
    ///    relationship.
    /// 4. DelauneyNeighborhoodGraph: a neighborhood graph of actual vertex points from strokes.
    /// 5. NeighborhoodGraphWithComponentMapping: a composable neighborhood graph built from another
    ///    finer neighborhood graph.
    /// 6. NeighborhoodGraphWithCache: a neighborhood graph where subsequent query can be generated quicker
    ///    by using cache.


    using Real = System.Single;
    /// Identification for a vertex (or connected component on finer graph).
    using VertexId = System.Int32;
    /// Identification for a connected component of vertices.
    using ComponentId = System.Int32;

    /// <summary>
    /// Basic componenet mapping interface.
    /// It provides way to get a component id of a vertex and function to test
    /// if a vertex belong to a component.
    /// </summary>
    internal interface ComponentMapping
    {
        /// <summary>
        /// Given a vertex id on the finer neighborhood graph, returns the component id of the
        /// coarser graph.
        /// </summary>
        /// <param name="vertexId">Id on the finer graph.</param>
        /// <returns>Id on the coarser graph.</returns>
        ComponentId GetComponentId(VertexId vertexId);

        /// <summary>
        /// Given a Id on the coarser graph, returns all the vertices of the finer graph
        /// of this component.
        /// </summary>
        /// <param name="compId">Id of a component on the coarser graph.</param>
        /// <returns>Array of Id of the vertices on the finer graph.</returns>
        VertexId[] GetVertexIds(ComponentId compId);
    }

    /// <summary>
    /// Structure to store neighbor's information.
    /// </summary>
    internal struct NeighborInfo : IComparable
    {
        public ComponentId ComponentId;
        public Real Distance;
        public NeighborInfo(ComponentId compId, Real distance)
        {
            this.ComponentId = compId;
            this.Distance = distance;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            Debug.Assert(obj is NeighborInfo, "Invalid comparison");
            NeighborInfo other = (NeighborInfo) obj;

            if (this.Distance > other.Distance)
            {
                return 1;
            }
            else if (this.Distance < other.Distance)
            {
                return -1;
            }
            
            // Next determine ordering by component id.
            return this.ComponentId - other.ComponentId;
        }

        #endregion
    }

    /// <summary>
    /// A delegate to calculate distance between two components.
    /// </summary>
    internal delegate Real DistanceMetric(ComponentId compId1, ComponentId compId2);

    /// <summary>
    /// Abstract class to provide neighborhood querying information.
    /// </summary>
    internal abstract class NeighborhoodGraph
    {

        /// <summary>
        /// Get all the direct neighbors' info of a component.
        /// Direct neighbors are neighbors which has direct edge
        /// in the delauney triangulation.
        /// </summary>
        /// <param name="compId">The id of the component.</param>
        /// <returns>Array of NeighborInfo of the direct neighbors.</returns>
        public abstract NeighborInfo[] GetDirectNeighborInfo(ComponentId compId);

        /// <summary>
        /// Get the neighbors of a given component.
        /// </summary>
        /// <param name="compId">The given component.</param>
        /// <returns>Array of nearest neighbor of the component.</returns>
        public ComponentId[] GetDirectNeighbors(ComponentId compId)
        {
            NeighborInfo[] neighborInfoArray = this.GetDirectNeighborInfo(compId);
            ComponentId[] result = new ComponentId[neighborInfoArray.Length];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = neighborInfoArray[i].ComponentId;
            }
            return result;
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Get all direct neighbors, however the distance is calculated with the
        /// given distance metric.
        /// </summary>
        /// <param name="compId">Component Id</param>
        /// <param name="distMetric">Distance Metric delegate</param>
        /// <returns>Array of NeighborInfo, distance is calculated based on the given distance metric.</returns>
        public NeighborInfo[] GetDirectNeighborInfo(ComponentId compId, DistanceMetric distMetric)
        {
            NeighborInfo[] neighborInfoArray = this.GetDirectNeighborInfo(compId);
            for (int i = 0; i < neighborInfoArray.Length; ++i)
            {
                neighborInfoArray[i].Distance = distMetric(compId, neighborInfoArray[i].ComponentId);
            }
            return neighborInfoArray;
        }

        /// <summary>
        /// Get all direct neighbors within a specified distance.
        /// </summary>
        /// <param name="compId">The given component.</param>
        /// <param name="distance">Distance threshold.</param>
        /// <returns>List of direct neighbors whose distance is less than the specified threshold.</returns>
        public NeighborInfo[] GetDirectNeighborsWithin(ComponentId compId, Real distance)
        {
            NeighborInfo[] neighborInfoArray = this.GetDirectNeighborInfo(compId);
            Array.Sort(neighborInfoArray);
            int i = 0;
            while (i < neighborInfoArray.Length && neighborInfoArray[i].Distance < distance)
            {
                ++i;
            }
            NeighborInfo[] result = new NeighborInfo[i];
            Array.Copy(neighborInfoArray, result, i);
            return result;       
        }

        /// <summary>
        /// Get all components within a specified distance.
        /// </summary>
        /// <param name="compId">The given component.</param>
        /// <param name="distance">Distance threshold.</param>
        /// <returns>List of direct neighbors whose distance is less than the specified threshold.</returns>
        public NeighborInfo[] GetNeighborsWithin(ComponentId compId, Real distance)
        {
            NeighborInfo[] neighborInfoArray = this.GetDirectNeighborInfo(compId);
            int numNeighbors = neighborInfoArray.Length;
            // Initialize capacity to 4 times the number of direct neighbors.
            Hashtable visited = new Hashtable(4 * numNeighbors);
            visited.Add(compId, null);
            SortedList queue = new SortedList(8 * numNeighbors);
            for (int i = 0; i < numNeighbors; ++i)
            {
                queue.Add(neighborInfoArray[i], neighborInfoArray[i]);
            }
            ArrayList result = new ArrayList(4 * numNeighbors);
            Real currentDistance = 0.0f;
            while (queue.Count > 0 && currentDistance < distance)
            {
                NeighborInfo current = (NeighborInfo) queue.GetByIndex(0);
                queue.RemoveAt(0);
                if (current.Distance < distance)
                {
                    if (!visited.ContainsKey(current.ComponentId))
                    {
                        NeighborInfo[] currentNeighbors = this.GetDirectNeighborInfo(current.ComponentId);
                        for (int i = 0; i < currentNeighbors.Length; ++i)
                        {
                            currentNeighbors[i].Distance += current.Distance;
                            if (currentNeighbors[i].Distance < distance && !visited.ContainsKey(currentNeighbors[i].ComponentId))
                            {
                                queue.Add(currentNeighbors[i], currentNeighbors[i]);
                            }
                        }
                        visited.Add(current.ComponentId, null);
                        result.Add(current);
                    }
                }
                else
                {   // Already beyond the specified limit.
                    break;
                }
            }
            return (NeighborInfo[]) result.ToArray(typeof(NeighborInfo));
        }

        /// <summary>
        /// Get all neighbors within a specified distance.
        /// </summary>
        /// <param name="compId">The given component.</param>
        /// <param name="distance">Distance threshold.</param>
        /// <returns>List of direct neighbors whose distance is less than the specified threshold.</returns>
        public NeighborInfo[] GetNeighborsWithin(ComponentId compId, Real distance, DistanceMetric distMetric)
        {
            NeighborInfo[] neighborInfoArray = this.GetDirectNeighborInfo(compId);
            int numNeighbors = neighborInfoArray.Length;
            // Array.Sort(neighborInfoArray);
            // Initialize capacity to 4 times the number of direct neighbors.
            Hashtable visited = new Hashtable(4 * numNeighbors);
            visited.Add(compId, null);
            SortedList queue = new SortedList(8 * numNeighbors);
            for (int i = 0; i < numNeighbors; ++i)
            {
                neighborInfoArray[i].Distance = distMetric(compId, neighborInfoArray[i].ComponentId);
                queue.Add(neighborInfoArray[i], neighborInfoArray[i]);
            }
            ArrayList result = new ArrayList(4 * numNeighbors);
            Real currentDistance = 0.0f;
            while (queue.Count > 0 && currentDistance < distance)
            {
                NeighborInfo current = (NeighborInfo) queue.GetByIndex(0);
                queue.RemoveAt(0);
                if (current.Distance < distance)
                {
                    if (!visited.ContainsKey(current.ComponentId))
                    {
                        NeighborInfo[] currentNeighbors = this.GetDirectNeighborInfo(current.ComponentId);
                        for (int i = 0; i < currentNeighbors.Length; ++i)
                        {
                            currentNeighbors[i].Distance = distMetric(compId, currentNeighbors[i].ComponentId);
                            if (currentNeighbors[i].Distance < distance && !visited.ContainsKey(currentNeighbors[i].ComponentId))
                            {
                                if (!queue.ContainsKey(currentNeighbors[i]))
                                {
                                    queue.Add(currentNeighbors[i], currentNeighbors[i]);
                                }
                            }
                        }
                        visited.Add(current.ComponentId, null);
                        result.Add(current);
                    }
                }
                else
                {   // Already beyond the specified limit.
                    break;
                }
            }
            return (NeighborInfo[]) result.ToArray(typeof(NeighborInfo));
        }
#endif

    }


    /// <summary>
    /// Neighborhood graph class of a Delauney Diagram.
	/// </summary>
	internal class DelauneyNeighborhoodGraph : NeighborhoodGraph
	{
        /// <summary>
        /// Underlying delauney graph of vertices.
        /// </summary>
        private Delauney _delauney;

        /// <summary>
        /// Cached edges for every vertex.
        /// </summary>
        private Edge[] _cachedEdges;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="delauney">The delauney triangulation of the vertices.</param>
        public DelauneyNeighborhoodGraph(Delauney delauney, int numVertices)
        {
            this._delauney = delauney;
            this.CacheEdges(numVertices);
        }

        /// <summary>
        /// Get all the direct neighbors' info of a component.
        /// Direct neighbors are neighbors which has direct edge
        /// in the delauney triangulation.
        /// </summary>
        /// <param name="compId">The id of the component.</param>
        /// <returns>Array of NeighborInfo of the direct neighbors.</returns>
        public override NeighborInfo[] GetDirectNeighborInfo(ComponentId compId)
        {
            ArrayList outboundEdges = this.GetOutboundEdges(compId);
            NeighborInfo[] neighborInfos = new NeighborInfo[outboundEdges.Count];
            for (int i = 0; i < neighborInfos.Length; ++i)
            {
                Edge outboundEdge = (Edge) outboundEdges[i];
                neighborInfos[i] = 
                    new NeighborInfo(outboundEdge.Dest().Id, outboundEdge.Norm());
            }
            return neighborInfos;
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Given a location coordinates, find the nearest vertices.
        /// This is used to find nearest vertices given a point (not a real vertex).
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public NeighborInfo[] GetDirectNeighborInfo(Vector2d location)
        {
            Edge edge = this._delauney.Locate(new Vertex(location, -1));
            if (edge != null)
            {
                NeighborInfo[] neighbors = new NeighborInfo[3];
                for (int i = 0; i < 3; ++i)
                {
                    Vertex v = edge.Org();
                    if (v.Id >= 0)
                    {
                        neighbors[i].ComponentId = v.Id;
                        neighbors[i].Distance = (location - v.Location).Norm;
                    }
                    else
                    {
                        neighbors[i].ComponentId = -1;
                    }
                    edge = edge.Dprev();
                }
                
                // If there is no valid component.
                if (neighbors[0].ComponentId < 0 &&
                    neighbors[1].ComponentId < 0 ||
                    neighbors[2].ComponentId < 0)
                {
                    return new NeighborInfo[0];
                }

                return neighbors;
            }
            else
            {
                return new NeighborInfo[0];
            }
        }

#endif

        /// <summary>
        /// Return array of all outbound edges from the given component.
        /// Outbound edge is an edge where the origin is a vertex belong
        /// to the component, and the destination is a vertex outside the
        /// component.
        /// </summary>
        /// <param name="compId">ComponentId</param>
        /// <returns>ArrayList of outbound edges.</returns>
        public ArrayList GetOutboundEdges(ComponentId compId)
        {
            ArrayList outboundEdges = new ArrayList(16);
            
            Edge foundEdge = this._cachedEdges[compId];
            // It's possible that the edge is not found because the vertex
            // not really inserted (In InsertSite) due to other vertex already
            // within Epsilon.
            
            if (foundEdge != null)
            {
                Edge edge = foundEdge;
                do 
                {
                    if (!edge.IsBorder())   // don't include border
                    {
                        outboundEdges.Add(edge);
                    }
                    edge = edge.Onext();
                } 
                while (edge != foundEdge);
            }
            return outboundEdges;
        }

        /// <summary>
        /// Caching edge connected to the vertex.
        /// </summary>
        /// <param name="numVertices">Number of vertices in this graph.</param>
        private void CacheEdges(int numVertices)
        {
            this._cachedEdges = new Edge[numVertices];
            foreach (Edge edge in this._delauney.Edges)
            {
                int id = edge.Org().Id;
                Debug.Assert(0 <= id && id < numVertices, "Invalid vertices");
                if (this._cachedEdges[id] == null)
                {
                    this._cachedEdges[id] = edge;
                }
                id = edge.Dest().Id;
                if (this._cachedEdges[id] == null)
                {
                    this._cachedEdges[id] = edge.Sym();
                }
            }
        }
    }


    /// <summary>
    /// Composable neighborhood graph: constructing a coarser neighborhood graph from
    /// a finer neighborhood graph by providing a component mapping.
    /// </summary>
    internal class NeighborhoodGraphWithComponentMapping : NeighborhoodGraph
    {
        /// <summary>
        /// The finer neighborhood graph, where this coarser neighborhood graph (NG) is based on.
        /// </summary>
        private NeighborhoodGraph _nGraph;
        /// <summary>
        /// The component mapping between finer to coarser NG and vice versa.
        /// </summary>
        protected ComponentMapping _compMapping;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nGraph">The finer neighborhod graph to base from.</param>
        /// <param name="compMapping">Component Mapping.</param>
        public NeighborhoodGraphWithComponentMapping(NeighborhoodGraph nGraph, ComponentMapping compMapping)
        {
            this._nGraph = nGraph;
            this._compMapping = compMapping;
        }

        /// <summary>
        /// Given an ID on this coarse graph, returns all its neighbor.
        /// </summary>
        /// <param name="compId">The component id.</param>
        /// <returns>Array of neighbors' information.</returns>
        public override NeighborInfo[] GetDirectNeighborInfo(Int32 compId)
        {
            Hashtable shortestDistances = new Hashtable(32);
            VertexId[] vids = this._compMapping.GetVertexIds(compId);
            for(int vix = 0; vix < vids.Length; ++vix)
            {
                NeighborInfo[] neighbors = this._nGraph.GetDirectNeighborInfo(vids[vix]);
                for(int nix = 0; nix < neighbors.Length; ++nix)
                {
                    NeighborInfo ni = neighbors[nix];
                    ComponentId neighborCompId = this._compMapping.GetComponentId(ni.ComponentId);
                    if (neighborCompId != compId)
                    {   // not pointing back to itself
                        Real shortestDistance = Real.MaxValue;
                        if (shortestDistances.ContainsKey(neighborCompId))
                        {
                            shortestDistance = (Real) shortestDistances[neighborCompId];
                        }
                        Real edgeDistance = ni.Distance;
                        if (edgeDistance < shortestDistance)
                        {
                            shortestDistances[neighborCompId] = edgeDistance;
                        }
                    }
                }
            }

            // Create an array of NeighborInfo from the map (hashtable).
            NeighborInfo[] result = new NeighborInfo[shortestDistances.Count];
            int i = 0;
            foreach (ComponentId neighborCompId in shortestDistances.Keys)
            {
                result[i].ComponentId = neighborCompId;
                result[i].Distance = (Real) shortestDistances[neighborCompId];
                ++i;
            }
            return result;            
        }
    }


    /// <summary>
    /// A class that provide quick access to neighbors. 
    /// It is useful when query for neighbor relationship
    /// is done many times and almost touching large portion
    /// of available component.
    /// Building and storing cache is relatively expensive, 
    /// but performing neighborhood query is relatively quick 
    /// afterward.
    /// </summary>
    internal class NeighborhoodGraphWithCache : NeighborhoodGraph
    {
        private NeighborhoodGraph _nGraph;
        private Hashtable _cachedDirectNeighbors;

        public NeighborhoodGraphWithCache(NeighborhoodGraph nGraph, int capacity)
        {
            this._nGraph = nGraph;
            // Initialize neighborInfo cache.
            this._cachedDirectNeighbors = new Hashtable(capacity);
        }

        /// <summary>
        /// Get neighbor info of a given component.
        /// </summary>
        /// <param name="compId">The component id.</param>
        /// <returns>Array of neighbor info.</returns>
        public override NeighborInfo[] GetDirectNeighborInfo(ComponentId compId)
        {
            if (!this._cachedDirectNeighbors.Contains(compId))
            {
                this._cachedDirectNeighbors.Add(compId, this._nGraph.GetDirectNeighborInfo(compId));
            }
            return (NeighborInfo[]) this._cachedDirectNeighbors[compId];
        }
    }
}
