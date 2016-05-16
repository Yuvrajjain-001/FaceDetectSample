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

namespace System.Windows.Ink.Analysis.MathLibrary
{
    /// <summary>
    /// Data structure to create Delauney Triangulation.
    /// This data structure allows incremental calculation of delauney
    /// triangulation.
    /// </summary>
    public class Delauney
    {
        /// <summary>
        /// The first valid edge of the triangulation.
        /// </summary>
        private Edge _startingEdge;

        /// <summary>
        /// Delauney triangle borders.
        /// </summary>
        private Vector2d[] _borders;  

        //private int _timestamp;								//@cmember Latest time stamp.

        /// <summary>
        /// Array of quad edges.
        /// </summary>
		private ArrayList _quadEdgeList = new ArrayList();        

#if INTERNAL_PARSER
        /// <summary>
        /// Starting edge.
        /// </summary>
		public Edge StartingEdge
		{
			get
			{
				return this._startingEdge;
			}
		}

        /// <summary>
        /// The quad edge array.
        /// </summary>
		public ArrayList QuadEdgeList
		{
			get
			{
				return this._quadEdgeList;
			}
		}

#endif // INTERNAL_PARSER

        /// <summary>
        /// Whether the point is inside this delauney triangle border.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>True if only if the point is inside the delauney border.</returns>
        public bool IsInside(Vector2d point)
        {
            Debug.Assert(this._borders != null, "Delauney triangulation is not yet initialized");
            return 
                Common.IsCounterClockwise(_borders[0], _borders[1], point) &&
                Common.IsCounterClockwise(_borders[1], _borders[2], point) &&
                Common.IsCounterClockwise(_borders[2], _borders[0], point);
        }

        /// <summary>
        /// Whether the rectangle is totally inside the Delauney triangle border.
        /// </summary>
        /// <param name="bound">Rectangle to test</param>
        /// <returns>True if the rectangle is inside the borders.</returns>
        public bool IsInside(Rectangle2d bound)
        {
            return  IsInside(bound.TopLeft) &&
                    IsInside(bound.TopRight) &&
                    IsInside(bound.BottomLeft) &&
                    IsInside(bound.BottomRight);

        }

        /// <summary>
        /// Whether the polyline is inside the delauney borders.
        /// </summary>
        /// <param name="polyline">Polyline to test.</param>
        /// <returns>True if all points in the polyline is inside the delauney borders.</returns>
        public bool IsInside(Polyline polyline)
        {
            if (!IsInside(polyline.Bound))
            {
                for (int i = 0; i < polyline.NumVertices; ++i)
                {
                    if (!IsInside(polyline[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Locate an edge defining a triangle containing the location of the given vertex.
        /// This is used to determine which triangle to update *before* the given vertex
        /// is inserted, thus the vertex itself is not yet in the Delauney.
        /// </summary>
        /// <param name="pt">Vertex.</param>
        /// <returns>The edge where the triangle containing the location exist.</returns>
        public Edge Locate(Vertex pt)
        {
            Edge e = this._startingEdge;
			int iteration = 0;
			int maxIteration = this._quadEdgeList.Count;
	
            // To prevent infinite loop.
            while(iteration++ < maxIteration) 
            {
                if (pt.IsWithinEpsilon(e.Org()) || pt.IsWithinEpsilon(e.Dest()))
                {
                    return e;
                }
                else if( pt.IsRightOf(e) )
                {
                    e = e.Sym();
                }
                else if( !pt.IsRightOf(e.Onext()) )
                {
                    e = e.Onext();
                }
                else if( !pt.IsRightOf(e.Dprev()) )
                {
                    e = e.Dprev();
                }
                else
                {
                    return e;
                }
            }

			return null;
        }
        
        /// <summary>
        /// Constructing a delauney triangulation, given the initial border vertices.
        /// The border vertices should be in counter clockwise order.
        /// </summary>
        /// <param name="a">The first vertex.</param>
        /// <param name="b">The second vertex.</param>
        /// <param name="c">The third vertex.</param>
        public Delauney(Vertex a, Vertex b, Vertex c)
        {
            Debug.Assert(a.Id < 0 && b.Id < 0 && c.Id < 0,
                "Delauney border vertices should have negative id");
            Edge ea = MakeEdge();
            ea.EndPoints(a, b);
            Edge eb = MakeEdge();
            Splice(ea.Sym(), eb);
            eb.EndPoints(b, c);
            Edge ec = MakeEdge();
            Splice(eb.Sym(), ec);
            ec.EndPoints(c, a);
            Splice(ec.Sym(), ea);
            this._startingEdge = ea;
            this._borders = new Vector2d[3]{ a.Location, b.Location, c.Location };
            //this._timestamp = 0;	

        }

        /// <summary>
        ///		This operator affects the two Edge rings around the origins of a and b,
        ///		and, independently, the two Edge rings around the left faces of a and b.
        ///		In each case, (i) if the two rings are distinct, Splice will combine
        ///		them into one; (ii) if the two are the same ring, Splice will break it
        ///		into two separate pieces.
        ///		Thus, Splice can be used both to attach the two edges together, and
        ///		to break them apart. See Guibas and Stolfi (1985) p.96 for more details
        ///		and illustrations.
        /// </summary>
        /// <param name="a">The first edge.</param>
        /// <param name="b">The second edge.</param>
        private static void Splice(Edge a, Edge b)
        {
            Edge alpha = a.Onext().Rot();
            Edge beta  = b.Onext().Rot();
	
            Edge t1 = b.Onext();
            Edge t2 = a.Onext();
            Edge t3 = beta.Onext();
            Edge t4 = alpha.Onext();
	
            a.Next = t1;
            b.Next = t2;
            alpha.Next = t3;
            beta.Next = t4;
        }
        
        /// <summary>
        /// Remove an edge from the subdivision and mark it as deleted.
        /// </summary>
        /// <param name="e">Edge to delete</param>
        private static void DeleteEdge(Edge e) 
        {
            // Removal steps.
            Splice(e, e.Oprev());
            Splice(e.Sym(), e.Sym().Oprev());

            // Mark deleted.
            e.QuadEdge.Delete();
        }

        /************* Topological Operations for Delaunay Diagrams *****************/

        /// <summary>
        ///		Add a new Edge e connecting the destination of a to the
        ///		origin of b, in such a way that all three have the same
        ///		left face after the connection is complete.
        ///		Additionally, the data pointers of the new Edge are set.
        /// </summary>
        /// <param name="a">The edge with the destination point.</param>
        /// <param name="b">The edge with the origin point.</param>
        /// <returns>The new edge.</returns>
        private Edge Connect( 
            Edge a,	
            Edge b	
        )
        {
            Edge e = MakeEdge();
            Splice(e, a.Lnext());
            Splice(e.Sym(), b);
            e.EndPoints(a.Dest(), b.Org());
            return e;
        }

        /// <summary>
        ///		Essentially turns Edge e counterclockwise inside its enclosing
        ///		quadrilateral. The data pointers are modified accordingly.
        /// </summary>
        /// <param name="e">The edge</param>
        private static void Swap( Edge e )
        {
            Edge a = e.Oprev();
            Edge b = e.Sym().Oprev();
            Splice(e, a);
            Splice(e.Sym(), b);
            Splice(e, a.Lnext());
            Splice(e.Sym(), b.Lnext());
            e.EndPoints(a.Dest(), b.Dest());
        }

        /// <summary>
        /// Make a new quad edge, and return the first edge.
        /// </summary>
        /// <returns>The first edge of the quad edge.</returns>
        private Edge MakeEdge()
        {
            QuadEdge qe = new QuadEdge();
            qe.Initialize();
			this._quadEdgeList.Add(qe);
            return qe.Edges[0];
        }

        /// <summary>
        /// Insert a vertex to the Delauney diagram.
        /// </summary>
        /// <param name="x"></param>
        public void InsertSite(Vertex x)
        {
            // Find the edge where this vertex will be inserted.
            Debug.Assert(x.Id >= 0, "Negative vertex id only allowed for borders");
            Edge e = Locate(x);
            if(e == null)
            {
                return;
            }
            // Vertex is very closed to other vertex.
            if (x.IsWithinEpsilon(e.Org()) || x.IsWithinEpsilon(e.Dest()))
            {
                return;
            }
            else if(x.IsOnEdge(e)) 
            {
                e = e.Oprev();
                DeleteEdge(e.Onext());
            }
	
            // Connect the new point to the vertices of the containing
            // triangle (or quadrilateral, if the new point fell on an
            // existing Edge.)

            Edge baseEdge = MakeEdge();
            baseEdge.EndPoints(e.Org(), x);
            Splice(baseEdge, e);
            
            this._startingEdge = baseEdge;
            do 
            {
                baseEdge = Connect(e, baseEdge.Sym());
                e = baseEdge.Oprev();
            } 
            while (e.Lnext() != this._startingEdge);
	
            // Examine suspect edges to ensure that the Delaunay condition
            // is satisfied.
            do 
            {
                Edge t = e.Oprev();
                if (t.Dest().IsRightOf(e) &&
                    Common.InCircle(e.Org().Location, t.Dest().Location, e.Dest().Location, x.Location)) 
                {
                    Swap(e);
                    e = e.Oprev();
                }
                else if (e.Onext() == this._startingEdge)  // no more suspect edges
                {
                    return;
                }
                else 
                { // pop a suspect Edge
                    e = e.Onext().Lprev();
                }
            } 
            while( true );
        }

        /// <summary>
        /// Return enumerator for edges (to be used by foreach command).
        /// </summary>
        public EnumerableEdges Edges
        {
            get 
            {
                return new EnumerableEdges(this._quadEdgeList);
            }
        }
    }


    /// <summary>
    /// class to enumerate edges.
    /// </summary>
    public class EnumerableEdges : IEnumerable
    {
        /// <summary>
        /// Array of QuadEdges to iterate.
        /// </summary>
        private ArrayList _quadEdgeList;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="quadEdgeList">Quad Edge list to iterate</param>
        public EnumerableEdges(ArrayList quadEdgeList)
        {
            this._quadEdgeList = quadEdgeList;
        }

        /// <summary>
        /// Make default constructor private so it won't be called from
        /// outside.
        /// </summary>
        private EnumerableEdges()
        {
        }
        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new EdgeEnumerator(this._quadEdgeList);
        }

        #endregion

    }

    /// <summary>
    /// Class to provide enumerator of valid edges on a Delauney triangulation structure.
    /// It will skip through deleted edges and border edges (edge connecting to the border).
    /// </summary>
    internal class EdgeEnumerator : IEnumerator
    {
        /// <summary>
        /// Current index ont the quad edge list.
        /// </summary>
        private int _currentIndex;

        /// <summary>
        /// The quad edge list to iterate.
        /// </summary>
        private ArrayList _quadEdgeList;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="quadEdgeList">Quad edge list to iterate over.</param>
        public EdgeEnumerator(ArrayList quadEdgeList)
        {
            this._quadEdgeList = quadEdgeList;
            this._currentIndex = -1;
        }

        /// <summary>
        /// Make this private so it won't be called.
        /// </summary>
        private EdgeEnumerator()
        {
        }

        #region IEnumerator Members

        /// <summary>
        /// Resetting the iterator.
        /// </summary>
        public void Reset()
        {
            this._currentIndex = -1;
        }

        /// <summary>
        /// Return the current object.
        /// </summary>
        public object Current
        {
            get
            {
                Debug.Assert( 0 <= this._currentIndex && this._currentIndex < this._quadEdgeList.Count,
							  "EdgeEnumerator access beyond valid index");
                return ((QuadEdge) this._quadEdgeList[this._currentIndex]).Edges[0];
            }
        }

        /// <summary>
        /// Move to the next valid edge.
        /// </summary>
        /// <returns>False if no valid edge available next, true otherwise.</returns>
        public bool MoveNext()
        {
            while (++this._currentIndex < this._quadEdgeList.Count)
            {
                QuadEdge qe = (QuadEdge) this._quadEdgeList[this._currentIndex];
                if (!qe.IsDeleted)
                {
                    Edge edge = qe.Edges[0];
                    if (!edge.IsBorder())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

    }

}
