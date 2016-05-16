//------------------------------------------------------------------------------
// <copyright from='2002' to='2004' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // For Debug class.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    using Real = System.Single;
    using VertexId = System.Int32;
    
    /// <summary>
    /// Vertex of a graph in 2D space.
	/// </summary>
	public struct Vertex
	{
        private Vector2d _location;
        private VertexId _id;

        /// <summary>
        /// Vertex id.
        /// </summary>
        public VertexId Id 
        {

            get { return _id; }
        }

        /// <summary>
        /// Location of the vertex.
        /// </summary>
        public Vector2d Location
        {
            get { return this._location; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="location">Location of the vertex.</param>
        /// <param name="id">Vertex id.</param>
        public Vertex(Vector2d location, VertexId id)
        {
            this._location = location;
            this._id = id;
        }

        /// <summary>
        /// Whether this vertex is within epsilon of the given vertex.
        /// </summary>
        /// <param name="vertex">The given vertex.</param>
        /// <returns>True if the location of this vertex is within epsilon of the given vertex.</returns>
        public bool IsWithinEpsilon(Vertex vertex)
        {
            return (this.Location - vertex.Location).Norm < Common.RealEpsilon;
        }

        /// <summary>
        /// Whether this point is on the right of the edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <returns>True if this vertex is on the right side of the edge.</returns>
        public bool IsRightOf(Edge edge)
        {
            return Common.IsCounterClockwise(this.Location, edge.Dest().Location, edge.Org().Location);
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Whether the point is on the left of the edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <returns>True if this vertex is on the left side of the edge.</returns>
        public bool IsLeftOf(Edge edge)
        {
            return Common.IsCounterClockwise(this.Location, edge.Org().Location, edge.Dest().Location);
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Test if this point is within Epsilon neighborhood of the edge.
        /// </summary>
        /// <param name="edge">Edge to test.</param>
        /// <returns>True if the point is on the edge.</returns>
        public bool IsOnEdge(Edge edge)
        {
            Vector2d edgeOrg = edge.Org().Location;
            Real t1 = (this._location - edgeOrg).Norm;
            if (t1 < Common.RealEpsilon)
            {
                return true;
            }

            Vector2d edgeDest = edge.Dest().Location;
            Real t2 = (this._location - edgeDest).Norm;
            if (t2 < Common.RealEpsilon)
            {
                return true;
            }

            Real t3 = (edgeOrg - edgeDest).Norm;
            if (t1 > t3 || t2 > t3)
            {
                return false;
            }
	
            // Line2d line = new Line2d(edgeOrg, edgeDest);
            // return Math.Abs(line.Evaluate(this)) < Common.RealEpsilon;
            Vector2d t = edgeDest - edgeOrg;
            Real len = t.Norm;
            Real a =   t.Y / len;
            Real b = - t.X / len;
            Real c = -(a*edgeOrg.X + b*edgeOrg.Y);
            return Math.Abs(a * this._location.X + b* this._location.Y + c) < Common.RealEpsilon;

        }

        #region Object overrides
        public override bool Equals(object obj)
        {
            if (!(obj is Vertex)) 
            {
                return false;
            }
            return this.Equals((Vertex) obj);
        }

        public bool Equals(Vertex v)
        {
            return this.Location == v.Location && this.Id == v.Id;
        }

        public static bool operator==(Vertex u, Vertex v)
        {
            return u.Equals(v);
        }

        public static bool operator!=(Vertex u, Vertex v)
        {
            return !u.Equals(v);
        }

        /// <summary>
        /// Generate a has code.  Hashcode is generated based on
        /// the location.
        /// </summary>
        /// <returns>The hashcode for the location.</returns>
        public override int GetHashCode()
        {
            return this.Location.GetHashCode();
        }
        #endregion // Object overrides

        
    }
}
