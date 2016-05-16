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
	/// <summary>
	/// QuadEdge is tuple of four edge, represent an edge, its reverse,
	/// its dual, and the reverse of its dual.
	/// </summary>
	public class QuadEdge
	{
        // Quad edge representation.
        private Edge[] _edges;

#if INTERNAL_PARSER
        // Timestamp marker for various uses.
        private int _timestamp;
#endif // INTERNAL_PARSER

        /// <summary>
        /// Whether this QuadEdge is deleted.
        /// </summary>
        private bool _isDeleted;

        /// <summary>
        /// Whether this QuadEdge is deleted.
        /// </summary>
        public bool IsDeleted
        {
            get { return this._isDeleted; }
            set { this._isDeleted = value; }
        }

        /// <summary>
        /// Array of 4 edges.
        /// </summary>
        public Edge[] Edges
        {
            get { return this._edges; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public QuadEdge()
        {

        }

        /// <summary>
        /// Initialize this structure.
        /// </summary>
        public void Initialize()
        {
            this._edges = new Edge[4];
            for (int i = 0; i < 4; ++i)
            {
				this._edges[i] = new Edge(i, this);
            }
            this._edges[0].Next = this._edges[0];
            this._edges[1].Next = this._edges[3];
            this._edges[2].Next = this._edges[2];
            this._edges[3].Next = this._edges[1];
            this._isDeleted = false;
#if INTERNAL_PARSER
            this._timestamp = 0;
#endif // INTERNAL_PARSER
        }

        /// <summary>
        /// To delete this edge.
        /// </summary>
        public void Delete()
        {
            this.IsDeleted = true;
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Whether this edge is visited at a timestamp.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public bool IsVisited(int timestamp)
        {
            return this._timestamp == timestamp;
        }
#endif // INTERNAL_PARSER
        
#if INTERNAL_PARSER
        /// <summary>
        /// Visit and timestamp this edge.
        /// </summary>
        /// <param name="timestamp">Timestamp to use.</param>
        public void Visit(int timestamp)
        {
            this._timestamp = timestamp;
        }
#endif // INTERNAL_PARSER

	}
}
