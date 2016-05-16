//------------------------------------------------------------------------------
// <copyright from='2002' to='2004' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // Debug functionalities.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    using Real = System.Single;

  	/// <summary>
	/// Edge is a line segment between two vertices.
	/// Edge is directed (i.e. there is an origin and a destination vertex).
	/// To calculate Delauney/Voronoi Diagram quickly, Edge is stored in a structure
	/// called QuadEdge where the edge, its inverse, its dual and the inverse of 
	/// its dual are stored together.
	/// </summary>
	public class Edge
	{

        /// <summary>
        /// The number of this edge (the position in the quad edge).
        /// </summary>
        private int _num;

        // The next ccw (counter clock wise) edge on the origin.
        private Edge _next;	

        // The origin
        private Vertex _data;
	
        /// <summary>
        /// The quad edge where it belongs.
        /// </summary>
        private QuadEdge _quadEdge;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Edge()			
        {
        }

		public Edge(int num, QuadEdge quadEdge)
		{
			this._num = num;
			this._quadEdge = quadEdge;
		}

        /// <summary>
        /// The data  of this edge (it's origin).
        /// </summary>
        public Vertex Data
        {
            get { return this._data; }
            set { this._data = value; }
        }

        /// <summary>
        /// The quad edge where this edge belongs to.
        /// </summary>
        public QuadEdge QuadEdge
        {
            get { return this._quadEdge; }
        }
    
        /// <summary>
        /// Number of this edge (for identifying edge in QuadEdge).
        /// </summary>
        public int Num
        {
            get { return this._num; }
        }

        /// <summary>
        /// Counter clock wise edge on the origin.
        /// </summary>
        public Edge Next
        {
            get { return this._next; }
            set { this._next = value; }
        }

        #region Navigation Methods
        // See Guibas & Stolfi for details.

        /// <summary>
        /// The dual of this edge from L to R.
        /// </summary>
        /// <returns>The dual of this edge from L to R.</returns>
        public Edge Rot()
        {
            return QuadEdge.Edges[(Num < 3) ? Num + 1 : Num - 3];
        }

        /// <summary>
        /// The dual of this edge from R to L.
        /// </summary>
        /// <returns>The dual of this edge from R to L.</returns>
        public Edge InvRot()
        {
            return QuadEdge.Edges[(Num > 0) ? Num - 1 : Num + 3];
        }

        /// <summary>
        /// The reverse of this edge.
        /// </summary>
        /// <returns>The reverse of this edge.</returns>
        public Edge Sym()
        {
            return QuadEdge.Edges[(Num < 2) ? Num + 2 : Num - 2];
        }
		
	    /// <summary>
	    /// The next counter clockwise edge on the origin.
	    /// </summary>
	    /// <returns>The next counter clockwise edge on the origin.</returns>
        public Edge Onext()
        {
            return this.Next;
        }

        /// <summary>
        /// The next clockwise edge on the origin.
        /// </summary>
        /// <returns>The next clockwise edge on the origin.</returns>
        public Edge Oprev()
        {
            return Rot().Onext().Rot();
        }

#if INTERNAL_PARSER
        /// <summary>
        /// The next counter clockwise edge on the destination.
        /// </summary>
        /// <returns>The next counter clockwise edge on the destination.</returns>
        public Edge Dnext()
        {
            return Sym().Onext().Sym();
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// The next clockwise edge on the destination.
        /// </summary>
        /// <returns>The next clockwise edge on the destination.</returns>
        public Edge Dprev()
        {
            return InvRot().Onext().InvRot();
        }	

        /// <summary>
        /// The next counter clockwise edge on the left face of this edge.
        /// </summary>
        /// <returns>The next counter clockwise edge on the left face of this edge.</returns>
        public Edge Lnext()
        {
            return InvRot().Onext().Rot();
        }

        /// <summary>
        /// The clockwise edge on the left face of this edge.
        /// </summary>
        /// <returns></returns>
        public Edge Lprev()
        {
            return Onext().Sym();
        }				

#if INTERNAL_PARSER
        /// <summary>
        /// The next counter clockwise edge on the right face of this edge.
        /// </summary>
        /// <returns>The next counter clockwise edge on the right face of this edge.</returns>
        public Edge Rnext()
        {
            return Rot().Onext().InvRot();
        }					

        /// <summary>
        /// The next counter clockwise edge on the right face of this edge (Const version).
        /// </summary>
        /// <returns>The next counter clockwise edge on the right face of this edge (Const version).</returns>
        public Edge Rprev()
        {
            return Sym().Onext();
        }	
#endif // INTERNAL_PARSER				
        #endregion // Navigation Methods

        /// <summary>
        /// The origin.
        /// </summary>
        /// <returns></returns>
        public Vertex Org()
        {
            return this.Data;
        }

        /// <summary>
        /// The destination.
        /// </summary>
        /// <returns></returns>
        public Vertex Dest()
        {
            return this.Sym().Data;
        }

        /// <summary>
        /// For the Delauney structure, the initial border vertices have negative Id.
        /// </summary>
        /// <returns>Whether this edge connect to a border vertex.</returns>
        public bool IsBorder()
        {
            return (this.Org().Id < 0) || (this.Dest().Id < 0);
        }

        /// <summary>
        /// Length of this edge.
        /// </summary>
        /// <returns>The length of this edge.</returns>
        public Real Norm()
        {
            return (this.Org().Location - this.Dest().Location).Norm;
        }

        /// <summary>
        /// Set the end points of this edge to given vertices.
        /// </summary>
        /// <param name="origin">The new origin.</param>
        /// <param name="destination">The new destination.</param>
        public void  EndPoints( Vertex origin, Vertex destination)
        {
            this.Data = origin;
            this.Sym().Data = destination;
        }



	}
}
