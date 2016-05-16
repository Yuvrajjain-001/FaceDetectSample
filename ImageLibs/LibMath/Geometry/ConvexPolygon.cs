//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'> 
// Copyright (c) Microsoft Corporation. All Rights Reserved. 
// Information Contained Herein is Proprietary and Confidential. 
// </copyright> 
//
//------------------------------------------------------------------------------
using System;
using System.Collections;   // For ArrayList, Stack
using System.Diagnostics;   // Debug functionalities.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    /// <summary>
    /// Class for representing convex polygon; designed for calculating convex hull.
    /// 
    /// Vertices in the polygon is ordered in COUNTER CLOCKWISE direction in the
    /// cartesian coordinates. Notice: the Y axis of screen coordinates system is 
    /// the reverse of the standard cartesian system in geometry book (Y positive is down
    /// in screen coordinates, and up in standard cartesian coordinates).  
    /// Thus, care should be take when reading the codes & comments while comparing with screen coordinates).
    /// </summary>
    public class ConvexPolygon : Polyline
    {
        #region Fields
        /// <summary>
        /// Private fields
        /// </summary>
        private const float Epsilon = 1e-3F;

        #endregion

        #region Properties
        #endregion

        #region Methods
        /// <summary>
        /// Public constructor.
        /// </summary>
        public ConvexPolygon()
        {
            // nothing here
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">Other polygon to copy.</param>
        public ConvexPolygon( ConvexPolygon other )
        {
            this._vertices = new Vector2d[ other.NumVertices ];
            other.Vertices.CopyTo( this._vertices, 0 );
            this._bound = other._bound;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="points">Points to calculate the convex hull.</param>
        public ConvexPolygon( Vector2d[] points )
        {
            CalculateConvexHull( points );
        }

        public void CalculateConvexHull( Vector2d[] points )
        {
            Debug.Assert( points.Length >= 3, "Convex hull calculation requires at least 3 points" );
            // Vector2d[] tmpHull = QuickHull( points );     // Calculate convex hull using QuickHull algorithm.
            IterativeQuickHull( points );

            // When all points are aligned, the output is just 2 points.
            // Thus make it into skinny triangle so it is a valid convex
            // hull.
            if( this._vertices.Length == 2 )
            {
                this._vertices = ConvexPolygon.MakeIntoTriangle( this._vertices );
            }
            // Debug.Assert( IsSameHull( tmpHull, _vertices ), "Not the same convex hull" );
            this._bound = Rectangle2d.Empty;            // Uninitialize the bounding rectangle.
        }

#if INTERNAL_PARSER
        public void AddPointsAndRecalculateConvexHull( Vector2d[] points )
        {
            if( this._vertices != null )
            {
                Vector2d[] newPoints = new Vector2d[ this._vertices.Length + points.Length ];
                this._vertices.CopyTo( newPoints, 0 );
                points.CopyTo( newPoints, this._vertices.Length );
                CalculateConvexHull( newPoints );
            }
            else
            {
                CalculateConvexHull( points );
            }
        }
#endif // DEBUG


        #region Indexing Helper

#if DEBUG
        /// <summary>
        /// Find the leftmost vertex index on the Convex Polygon.
        /// If the Convex Polygon is generated with the Convex Hull calculation
        /// in this class, the leftmost vertex is the first vertex.
        /// </summary>
        /// <returns>The index of the leftmost vertex.</returns>
        public int LeftmostVertexIndex()
        {
            // Find the leftmost point of this convex polygon vertex.
            int ip = 0;
            Vector2d vp = this[0];
            for (int i = 1; i < this._vertices.Length; ++i)
            {
                Vector2d v = this[i];
                if (v.X < vp.X ||               // v is on the left of vp
                    (v.X == vp.X && v.Y < vp.Y))    // v is at the same X coordinate, find the smaller y.
                {
                    vp = v;
                    ip = i;
                }
            }
            Debug.Assert(ip == 0, "The convex polygon is not calculated by the algorithm in this class");
            return ip;
        }
#endif // DEBUG

        /// <summary>
        /// Given the current index, return the previous index.  If the current index is 0,
        /// it will return the index of the last vertex.
        /// </summary>
        /// <param name="currentIndex">The current index.</param>
        /// <returns>The previous index.</returns>
        public int PrevIndex(int currentIndex)
        {
            Debug.Assert(0 <= currentIndex && currentIndex < this.Vertices.Length,
                "Invalid current index");
            return (currentIndex == 0) ? this.Vertices.Length - 1 : currentIndex - 1;
        }

        /// <summary>
        /// Given the current index, return the next index.  If the current index is for 
        /// the last vertex, it will return the first index (= 0).
        /// </summary>
        /// <param name="currentIndex">The current index.</param>
        /// <returns>The next index.</returns>
        public int NextIndex(int currentIndex)
        {
            Debug.Assert(0 <= currentIndex && currentIndex < this.Vertices.Length,
                "Invalid current index");
            return (++currentIndex == this.Vertices.Length) ? 0 : currentIndex;
        }
        #endregion // Indexing Helper

#if INTERNAL_PARSER
        /// Calculate the squared distance between two convex polygon. 
        /// Distance between two polygon is defined as the minimum distance between any
        /// points in those two convex polygon.
        /// </summary>
        /// <param name="other">The other convex polygon.</param>
        /// <returns>The square of the distance, zero if the two convex polygon intersect.</returns>
        public double DistanceSqd( ConvexPolygon other )
        {
            double distSqd1 = this.DumbDistanceSqd(other);
            // double distSqd2 = this.BetterDistanceSqd(other);
            // double distSqd3 = this.BestDistanceSqd(other);
            // Debug.Assert( distSqd1 == distSqd3, "Distance Sqd does not match " + distSqd1.ToString() + " vs " + distSqd3.ToString() );

            return distSqd1;
        }
        
        /// <summary>
        /// Calculate the squared distance between two convex polygon. 
        /// Distance between two polygon is defined as the minimum distance between any
        /// points in those two convex polygon.
        /// </summary>
        /// <param name="other">The other convex polygon.</param>
        /// <returns>The square of the distance, zero if the two convex polygon intersect.</returns>
        public double BetterDistanceSqd( ConvexPolygon other )
        {
            if( this.IntersectsWith( other ) )
            {
                return 0.0;
            }

            // The algorithm below are just for a quick implementation but not the most
            // efficient one.  
            // The algorithm is based on the fact that the minimum distance between
            // two convex hull is achieved by vertices and/or edges not involved in
            // the combined hull of the two.
                       
            Vector2d[] visibleVertices1 = this.VisibleVertices(other.Vertices[0]);
            Vector2d[] visibleVertices2 = other.VisibleVertices(this.Vertices[0]);

            // Find internal vertices that realize the minimum distance.
            // Note that the first and the last vertices on internalEdges do not count
            // because they're actually in the enclosing hull.
            int iMin1 = 0;
            int iMin2 = 0;
            double minDistanceSqd = double.MaxValue;
            for( int i1 = 0; i1 < visibleVertices1.Length; ++i1 )
            {
                for( int i2 = 0; i2 < visibleVertices2.Length; ++i2 )
                {
                    double distanceSqd = Common.DistanceSqd( visibleVertices1[ i1 ], visibleVertices2[ i2 ] );
                    if( distanceSqd < minDistanceSqd )
                    {
                        iMin1 = i1;
                        iMin2 = i2;
                        minDistanceSqd = distanceSqd;
                    }
                }
            }

            if( iMin2 > 0 )
            {
                minDistanceSqd = Math.Min(minDistanceSqd,
                                         Common.DistanceSqdPointToLineSegment(visibleVertices1[ iMin1 ], 
                                                                              visibleVertices2[ iMin2 - 1],
                                                                              visibleVertices2[ iMin2 ] ));
            }

            if (iMin2 < visibleVertices2.Length - 1)
            {
                minDistanceSqd = Math.Min(minDistanceSqd,
                                          Common.DistanceSqdPointToLineSegment(visibleVertices1[ iMin1 ], 
                                                                               visibleVertices2[ iMin2],
                                                                               visibleVertices2[ iMin2 + 1 ] ));
            }

            if (iMin1 > 0)
            {
                minDistanceSqd = Math.Min(minDistanceSqd,
                                          Common.DistanceSqdPointToLineSegment(visibleVertices2[ iMin2 ],
                                                                               visibleVertices1[ iMin1 - 1 ],
                                                                               visibleVertices1[ iMin1 ] ));
            }

            if (iMin1 < visibleVertices2.Length -1 )
            {
                minDistanceSqd = Math.Min(minDistanceSqd,
                                          Common.DistanceSqdPointToLineSegment(visibleVertices2[ iMin2 ],
                                                                               visibleVertices1[ iMin1 ],
                                                                               visibleVertices1[ iMin1 + 1 ] ));
            }

            return minDistanceSqd;
                          
        }
        /// <summary>
        /// Brute force approach to calculate distance sqd between convex polygon.
        /// Used to test the correctness of the "optimized" algorithm.
        /// </summary>
        /// <param name="other">Other convex polygon.</param>
        /// <returns>The square of the distance, zero if the two convex polygon intersect.</returns>
        public double DumbDistanceSqd( ConvexPolygon other )
        {
            if( this.IntersectsWith( other ) )
            {
                return 0.0;
            }

            double minDistanceSqd = double.MaxValue;
            int iPrev = this.NumVertices - 1;

            for( int i = 0; i < this.NumVertices; iPrev = i++ )
            {
                int jPrev = other.NumVertices - 1;
                for( int j = 0; j < other.NumVertices; jPrev = j++ )
                {
                    double distanceSqd = MinEdgeDistanceSqd( this._vertices[ iPrev ], this._vertices[ i ],
                                            other.Vertices[ jPrev ], other.Vertices[ j ] );

                    if (distanceSqd < minDistanceSqd)
                    {
                        minDistanceSqd = distanceSqd;
                    }
                }
            }
            return minDistanceSqd;
        }

#endif // INTERNAL_PARSER

#if INTERNAL_PARSER
        /// <summary>
        /// Calculate minimum distance between two edges.  Edge is defined by its two end points;
        /// Edge a = (a1, a2) and Edge b = (b1, b2).
        /// This is an brute-forec calculation (non-optimized), it compares each endpoint with
        /// the other edge. (Only true for 2D case).
        /// </summary>
        /// <param name="a1">The first endpoint of the first edge.</param>
        /// <param name="a2">The second endpoint of the first edge.</param>
        /// <param name="b1">The first endpoint of the second edge.</param>
        /// <param name="b2">The second endpoint of the second edge.</param>
        /// <returns></returns>
        private static double MinEdgeDistanceSqd(Vector2d a1, Vector2d a2, Vector2d b1, Vector2d b2)
        {
            return Math.Min(Common.DistanceSqdPointToLineSegment(a1, b1, b2),
                        Math.Min(Common.DistanceSqdPointToLineSegment(a2, b1, b2),
                            Math.Min(Common.DistanceSqdPointToLineSegment(b1, a1, a2),
                                      Common.DistanceSqdPointToLineSegment(b2, a1, a2))));


        }

        /// <summary>
        /// This is the implementation based on Edelsbrunner algorithm found in:
        /// H. Edelsbrunner, Computing the extreme distance between two convex polygon,
        /// Journal of Algorithms, Volume 6, Issue 2 (June 1985).
        /// The main idea is as follow.
        /// 1. Find a sequence of visible vertices of the convex polygon from a point
        ///    in the other polygon.  Do the same thing for the other polygon.
        ///    The minimum distance is attained between edges on these vertices.
        /// 2. Do binary elimination, select midpoints on the lists, connect both
        ///    midpoints.  Based on the angle between the line connecting both midpoints
        ///    with the previous and next edges, it can eliminate half portion of the
        ///    vertices to test.
        /// </summary>
        /// <param name="other">Other convex polygon.</param>
        /// <returns>The minimum distance squared between this convex polygon with the other
        /// convex polygon.</returns>
        private double BestDistanceSqd( ConvexPolygon other )
        {           
            if( this.IntersectsWith( other ) )
            {
                return 0.0;
            }
            Vector2d[] visibleVertices = this.VisibleVertices(other.Vertices[0]);
            Vector2d[] otherVisibleVertices = other.VisibleVertices(this.Vertices[0]);
            return CalculateDistanceSqd(visibleVertices, otherVisibleVertices);

        }


        /// <summary>
        /// Determine vertices of this convex polygon "visible" from the reference
        /// point.  A vertex is visible if at least one of its edges is visible
        /// from the reference point.  It is assumed that the reference point should
        /// be outside the convex polygon.
        /// If we walk along the edges of a polygon in counter clockwise direction,
        /// the edge is visible from the reference point if the reference point is on 
        /// the right side (the triarea is negative). 
        /// Thus visible vertices started when the triarea is positive and ends when
        /// the triarea is negative.   
        /// The complication on the code here is just to ensure we have a contiguous
        /// sequence (in case the visible vertices stradle the first point in the polygon).
        /// </summary>
        /// <param name="referencePoint">Reference point</param>
        /// <returns>Array of vertices visible from the reference point.</returns>
        public Vector2d[] VisibleVertices(Vector2d referencePoint)
        {
            // Need to collect all edges that has negative triarea.
            int i = 0;
        
            while ((i < this.NumVertices - 1) &&
                    Common.TriArea(this._vertices[i], this._vertices[i+1], referencePoint) > 0)
            {
                ++i;
            }

            int j = i + 1;
            while ((j < this.NumVertices - 1) &&
                    Common.TriArea(this._vertices[j], this._vertices[j+1], referencePoint) < 0)
            {
                ++j;
            }

            if (j == this.NumVertices - 1 &&
                    Common.TriArea(this._vertices[j], this._vertices[0], referencePoint) < 0)
            {
                ++j;
            }

            if (i == 0)
            {
                int k = 0;
                int kPrev = this.NumVertices - 1;
                while (Common.TriArea(this._vertices[kPrev], this._vertices[k], referencePoint) < 0)
                {
                    k = kPrev;
                    --kPrev;
                }
                if ( k > 0 )
                {
                    Vector2d[] results = new Vector2d[ this.NumVertices - k + j + 1 ];
                    for (int l = k; l < this.NumVertices; ++l)
                    {
                        results[l-k] = this._vertices[l];
                    }
                    int l2 = this.NumVertices - k;
                    for (int l = 0; l < j + 1; ++l)
                    {
                        int lMod = ( l < this.NumVertices ) ? l : 0;
                        results[l2 + l] = this._vertices[lMod];
                    }
                    return results;
                }
            }
            // else
            {
                Vector2d[] results = new Vector2d[ j - i + 1 ];
                for (int l = i; l < j + 1; ++l)
                {
                    int lMod = ( l < this.NumVertices ) ? l : 0;
                    results[l - i] = this._vertices[lMod];
                }
                return results;
            }
        }

        
        /// <summary>
        /// Calculate the angle defined by line segment PQ and QR.  It returns
        /// between -PI/2 to 3/2 PI
        /// </summary>
        /// <param name="p">The starting point.</param>
        /// <param name="q">The corner of the angle to measure.</param>
        /// <param name="r">The end point.</param>
        /// <returns></returns>
        private static double Angle(Vector2d p, Vector2d q, Vector2d r)
        {
            Vector2d qp = Common.Subtract(p, q);
            Vector2d qr = Common.Subtract(r, q);
            float dotProduct = Common.DotProduct(qp, qr);
            double cosValue = dotProduct / (Common.Length(qp) * Common.Length(qr));
            double angle = Math.Acos(cosValue);
            float triarea = Common.TriArea(q, p, r);
            if (triarea < 0)
            {
                if (angle < Math.PI/2.0)
                {
                    return -angle;
                }
                else
                {
                    return 2 * Math.PI - angle;
                }
            }
            else
            {
                return angle;
            }
        }

        /// <summary>
        /// Calculate distance squared of two convex polygons.
        /// Only the visible vertices of each polygons are given (thus p and q
        /// are actually only a subset of vertices in the actual polygon).
        /// This is implementation of the BINARY ELIMINATION and FINAL PHASE
        /// asd described by this paper:
        /// Edelsbrunner, H. "Computing the Extreme Distances between Two Convex Polygon", 
        /// Journal of Algorithm 6, 213-224 (1985).
        /// </summary>
        /// <param name="p">Visible vertices of polygon P from a point in Q</param>
        /// <param name="q">Visible vertices of polygon Q from a point in P</param>
        /// <returns>The squared of the minimum distance between two convex polygon.</returns>
        private static double CalculateDistanceSqd(Vector2d[] p, Vector2d[] q)
        {
            const double HalfPi = Math.PI / 2;
            
            // BINARY ELIMINATION 
            int p1 = 0;
            int p2 = p.Length - 1;
            int q2 = 0;
            int q1 = q.Length - 1;
        
            int numP = p.Length;    // Number of points in visible vertices of P.
            int numQ = q.Length;    // Number of points in the visible vertices of Q.

            int totalNum = numP + numQ;    // For debugging.
            while (numP > 2 || numQ > 2)
            {
                // Note, we use the same indexing as in the paper, p1 <= p2 
                // and q2 < q1 (NOTICE the reverse of the indexing scheme between P & Q).
                Debug.Assert(p1 <= p2 && q2 <= q1, "Incorrect index ordering");

                if (numQ <= 2)
                {
                    // Swap P and Q.  Thus always make P smaller size than Q.
                    Vector2d[] oldQ = q;
                    int oldQ1 = q1;
                    int oldQ2 = q2;
                    q = p;
                    q2 = p1;
                    q1 = p2;
                    numQ = q1 - q2 + 1;

                    p = oldQ;
                    p1 = oldQ2;
                    p2 = oldQ1;
                    numP = p2 - p1 + 1;
                }

                // Case 1
                if (numP == 1)
                {    
                    int mp = p1;
                    int mq = (q1+q2)/2;
                    double beta1 = Angle(p[mp], q[mq], q[mq+1]);
                    double beta2 = Angle(q[mq-1], q[mq], p[mp]);
                    
                    if (beta1 >= HalfPi)
                    {
                        q1 = mq;
                    }
                    if (beta2 >= HalfPi)
                    {
                        q2 = mq;
                    }
                }   
                // Case 2: One of the sequence contains only two vertices.
                else if (numP == 2)    
                {
                    int mp = p2;
                    int mq = (q1+q2)/2;

                    double alpha1 = Angle(p[p1], p[mp], q[mq]);
                    double beta1 = Angle(p[mp], q[mq], q[mq+1]);
                    double beta2 = Angle(q[mq-1], q[mq], p[mp]);

                    if (beta1 < 0.0)
                    {
                        beta1 += (2 * Math.PI);
                    }

                    // Case 2.1
                    if (alpha1 > 0.0)
                    {
                        // Step (1)
                        if (alpha1 + beta1 > Math.PI)
                        {
                            if (alpha1 >= HalfPi && beta2 >= 0.0)
                            {
                                p1 = p2;
                            }

                            if (beta1 >= HalfPi)
                            {
                                q1 = mq;
                            }
                        }
                    

                        // Step (2)
                        if (beta2 >= HalfPi)
                        {
                            q2 = mq;
                        }

                        // Step (3)
                        if (alpha1 < beta2 && beta2 < HalfPi)
                        {
                            // Calculate whether orthogonal projection of mq exist in line segment
                            // p1-p2.
                            // The calculation is done using this formula
                            // Let vector s = p2-p1
                            //     vector t = mq-p1
                            // The dot product of s and t (s.t) is |s|.|t|.cos(alpha)
                            // where |v| is the length of vector v,
                            //      and alpha is the angle between the two vectors.
                            //
                            // The length of the projection of t on a unit vector with the direction of s
                            // is equal to  ( s.t / |s| )
                            // 
                            // The projection of mq is in the line segment p1-p2 iff
                            //      ( s.t / |s| ) >= 0 &&  ( s.t / |s| ) <= |s|
                            // or
                            //      ( s.t / |s|^2 ) >= 0 && ( s.t / |s|^2 ) <= 1.0

                            double p1p2DistanceSqd = Common.DistanceSqd(p[p1], p[p2]);
                            float dotProduct = Common.DotProduct(p[p1]-p[p2], p[p1]-q[mq]);
                            double lambda = dotProduct / p1p2DistanceSqd;

                            if ( 0 <= lambda && lambda <= 1.0 )
                            {
                                // orthogonal projection of mq exists in s(p1,p2)
                                q2 = mq;
                            }
                            else
                            {
                                p2 = p1;
                            }
                        }
                    }
                        // Case 2.2
                    else    
                    {    // alpha1 <= 0
                        p2 = p1;
                        if (beta1 >= Math.PI)
                        {
                            q1 = mq;
                        }
                        if (beta2 >= Math.PI)
                        {
                            q2 = mq;
                        }
                    }
                }
                    // Case 3. Both sequences contain at least three vertices each.
                else
                {
                    Debug.Assert(numQ > 2 && numP > 2);
                    
                    int mp = (p1+p2)/2;
                    int mq = (q1+q2)/2;
                    double alpha1 = Angle(p[mp-1], p[mp], q[mq]);
                    double alpha2 = Angle(q[mq],p[mp], p[mp+1]);
                    double beta1 = Angle(p[mp], q[mq], q[mq+1]);
                    double beta2 = Angle(q[mq-1], q[mq], p[mp]);

                    // Case 3.1: Each of alpha1, alpha2, beta1, and beta2 is positive.
                    if (alpha1 > 0.0 && alpha2 > 0.0 && beta1 > 0.0 && beta2 > 0.0)
                    {
                        // Step (1)
                        if (alpha1 + beta1 > Math.PI)
                        {
                            if (alpha1 >= HalfPi)
                            {
                                p1 = mp;
                            }
                            if (beta1 >= HalfPi)
                            {
                                q1 = mq;
                            }
                        }
                        // Step (2)
                        if (alpha2 + beta2 > Math.PI)
                        {
                            if (alpha2 >= HalfPi)
                            {
                                p2 = mp;
                            }
                            if (beta2 >= HalfPi)
                            {
                                q2 = mq;
                            }
                        }
                    }
                    else
                    {
                        // Case 3.2 At least one of either alpha1, alpha2, beta1 or beta2 is nonpositive.
                        if (alpha1 <= 0 || alpha2 <= 0)
                        {
                            if (alpha1 <= 0)
                            {
                                p2 = mp;
                            }
                            else
                            {
                                p1 = mp;
                            }

                            if (beta1 >= Math.PI)
                            {
                                q1 = mq;
                            }
                            if (beta2 >= Math.PI)
                            {
                                q2 = mq;
                            }
                        }
                        else
                        {
                            if (beta1 <= 0)
                            {
                                q2 = mq;
                            }
                            else
                            {
                                Debug.Assert(beta2 <= 0);
                                q1 = mq;
                            }

                            if (alpha1 >= Math.PI)
                            {
                                p1 = mp;
                            }

                            if (alpha2 >= Math.PI)
                            {
                                p2 = mp;
                            }
                        }
                    }

                }

                Debug.Assert(p1 <= p2 && q2 <= q1, "Incorrect indexing");
                numP = p2 - p1 + 1;
                numQ = q1 - q2 + 1;
                Debug.Assert(numP + numQ < totalNum, "Non-decreasing");
                totalNum = numP + numQ;
            }
            
            Debug.Assert( numP <= 2 && numQ <= 2 );

            // FINAL PHASE
            if ( numP == 1 )
            {
                if ( numQ == 1 )
                {
                    Debug.Assert( p1 == p2 && q2 == q1 );
                    return Common.DistanceSqd(p[p1], q[q1]);
                }
                else
                {
                    return Common.DistanceSqdPointToLineSegment(p[p1], q[q2], q[q1]);
                }
            }

            if ( numQ == 1 )
            {
                return Common.DistanceSqdPointToLineSegment(q[q1], p[p1], p[p2]);
            }

            Debug.Assert(numP == 2 && numQ == 2);
            
            double p1q = Common.DistanceSqdPointToLineSegment(p[p1], q[q2], q[q1]);
            double p2q = Common.DistanceSqdPointToLineSegment(p[p2], q[q2], q[q1]);
            double q1p = Common.DistanceSqdPointToLineSegment(q[q1], p[p1], p[p2]);
            double q2p = Common.DistanceSqdPointToLineSegment(q[q2], p[p1], p[p2]);
            return Math.Min(p1q, Math.Min(p2q, Math.Min(q1p, q2p)));
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Test if this convex polygon contains the point.  Polygon should be ordered
        /// in counter clockwise direction.
        /// </summary>
        /// <param name="pt">Point to test.</param>
        /// <returns>True if the points is inside the polygon.</returns>
        public bool Contains( Vector2d pt )
        {
#if DEBUG
            Debug.Assert( IsInitialized(),
                "Convex polygons should be initialized before calling this function" );
#endif

            int prev = NumVertices - 1;
            for( int i = 0; i < NumVertices; prev = i++ )
            {
                // Test if the point is on the left side of the edge.
                Vector2d d = this._vertices[ i ] - this._vertices[ prev ];
                Vector2d e = pt - this._vertices[ prev ];
                if( Common.PerpDotProduct( d, e ) < 0.0 )
                {
                    // If at least the point pt is on the right left of an edge
                    // then the point pt cannot be inside the convex polygon.
                    return false;    
                }
            }
            return true;
        }

        /// <summary>
        /// Test whether this convex polygon intersect with other convex polygon.
        /// </summary>
        /// <param name="other">Other convex polygon</param>
        /// <returns>True if both convex polygon are intersecting each other, false otherwise.</returns>
        public bool IntersectsWith(ConvexPolygon other)
        {
            return IntersectionTest(this, other);
        }

        #region Convex Hull Calculations
        #region Recursive QuickHull Algorithm
#if INTERNAL_PARSER
        /// <summary>
        /// Recursive implementation of QuickHull algorithm.
        /// </summary>
        /// <param name="points">Input points.</param>
        /// <returns>The sequence of points defining the quickhull.</returns>
        private Vector2d[] QuickHull( Vector2d[] points )
        {
            Debug.Assert( points.Length >= 3, "QuickHull require at least 3 points" );

            ArrayList hull = new ArrayList( points.Length / 2 ); // May need to be adjusted here.

            // Find the leftmost (p0) and rightmost (p1) points.
            Vector2d p0 = points[ 0 ];
            Vector2d p1 = p0;

            for( int i = 1; i < points.Length; ++i )
            {
                Vector2d pi = points[ i ];
                if( pi.X < p0.X || ( pi.X == p0.X && pi.Y < p0.Y ) )
                {
                    p0 = pi;
                }
                else if( pi.X > p1.X || ( pi.X == p1.X && pi.Y > p1.Y ) )
                {
                    p1 = pi;
                }
            }

            // The leftmost and rightmost point define a dividing line p01.  Points that are
            // to the left of p01 are collected in pts01, and points that are to the right
            // of p01 are collected in pts10.
            // Points that are on the dividing line need not be processed further.
            Vector2d p01 = Common.Subtract( p1, p0 );  // vector from p0 to p1
            Vector2d n01 = Common.UnitNormal( p01 );   // the normal of vector p01

            // Prepare array to hold points to the left and right of line from p0 to p1.
            ArrayList leftPts  = new ArrayList( points.Length / 2 );
            ArrayList rightPts = new ArrayList( points.Length / 2 );

            foreach( Vector2d pi in points )
            {
                Vector2d p0i = new Vector2d( pi.X - p0.X, pi.Y - p0.Y );
                float distanceSqd = Common.DotProduct( p0i, n01 );

                if( distanceSqd > Epsilon )
                {
                    leftPts.Add( pi );
                }
                else if( distanceSqd < -Epsilon )
                {
                    rightPts.Add( pi );
                }
            }

            // Divide and conquer.
            hull.Add( p0 );
            if( leftPts.Count > 0 )
            {
                QuickHull( p0, p1, leftPts, hull );
            }
            hull.Add( p1 );
            if( rightPts.Count > 0 )
            {
                QuickHull( p1, p0, rightPts, hull );
            }

            // Now turn the arraylist into array of points.
            return (Vector2d[]) hull.ToArray( typeof( Vector2d ) );
        }

        /// <summary>
        /// Helper function for the recursive quickhull algorithm.
        /// </summary>
        /// <param name="p0">Point defining the start of the current edge.</param>
        /// <param name="p1">Point defining the end of the current edge.</param>
        /// <param name="points">Points outside the current edge.</param>
        /// <param name="hull">Result of the convex hull so far.</param>
        private void QuickHull( Vector2d p0, Vector2d p1, ArrayList points, ArrayList hull )
        {
            // First find a point farthest away from line p0-p1.
            Vector2d p01 = Common.Subtract( p1, p0 );
            Vector2d n01 = Common.UnitNormal( p01 );   // UnitNormal vector.

            float maxDistanceSqd = 0.0f;
            int   maxIndex = -1;
            for( int i = 0; i < points.Count; ++i )
            {
                Vector2d p0i = Common.Subtract( (Vector2d) points[ i ], p0 );
                float distanceSqd = Common.DotProduct( p0i, n01 );
                if( distanceSqd - maxDistanceSqd > Epsilon )
                {
                    maxIndex    = i;
                    maxDistanceSqd = distanceSqd;
                }
            }

      
            // maxIndex is the index of the point with maximum distanceSqd from p0-p1 line.
            if( maxIndex >= 0 ) 
            {  
                Vector2d p2 = (Vector2d) points[ maxIndex ];
                // Set the capacity to the same size as the original point to avoid re-alloc.
                ArrayList pts02 = new ArrayList( points.Count );
                Vector2d p02 = Common.Subtract( p2, p0 );
                Vector2d n02 = Common.UnitNormal( p02 );

                ArrayList pts21 = new ArrayList( points.Count );
                Vector2d p21 = Common.Subtract( p1, p2 );
                Vector2d n21 = Common.UnitNormal( p21 );

                foreach( Vector2d pi in points )
                {
                    Vector2d p0i = Common.Subtract( pi, p0 );
                    if( Common.DotProduct( p0i, n02 ) > Epsilon )
                    {
                        pts02.Add( pi );
                    }
                    else
                    {
                        Vector2d p2i = Common.Subtract( pi, p2 );
                        if( Common.DotProduct( p2i, n21 ) > Epsilon )
                        {
                            pts21.Add( pi );
                        }
                    }
                }

                if( pts02.Count > 0 )
                {
                    QuickHull( p0, p2, pts02, hull );
                }

                hull.Add( p2 );

                if( pts21.Count > 0 )
                {
                    QuickHull( p2, p1, pts21, hull );
                }
            }
        }
#endif // INTERNAL_PARSER

        #endregion Recursive QuickHull Algorithm

        #region Non-recursive (iterative) QuickHull Algorithm

        /// <summary>
        /// Edge of a polygon.
        /// </summary>
        private struct Edge
        {
            /// <summary>
            /// Start of the edge.
            /// </summary>
            public Vector2d StartPoint;       
            /// <summary>
            /// End of the edge.
            /// </summary>
            public Vector2d EndPoint;
            /// <summary>
            /// Unit normal pointing outward of the edge (to the right side of
            /// the edge, walking from start to end point).
            /// </summary>
            public Vector2d Normal;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="start">Start point</param>
            /// <param name="end">End point</param>
            public Edge( Vector2d start, Vector2d end )
            {
                StartPoint = start;
                EndPoint = end;
                if( start == end )
                {
                    Normal = new Vector2d( 0f, 0f );
                }
                else
                {
                    Normal = Common.Subtract( end, start );
                    Normal = Common.UnitNormal( Normal );
                }
            }

            /// <summary>
            /// Whether a given point is on the outside of this edge.  
            /// Outside is defined as to the right of the line segment while
            /// walking from StartPoint to EndPoint.
            /// </summary>
            /// <param name="point">The point to test.</param>
            /// <returns>Whether the point is outside the line segment.</returns>
            public bool IsOutside( Vector2d point )
            {
                Vector2d startToPointVector = Common.Subtract( point, StartPoint );
                return Common.DotProduct( startToPointVector, Normal ) > Epsilon;
            }
        }

        private struct HullTask
        {
            public Edge Edge;
            public int StartIndex;
            public int NumPoints;

            internal HullTask( Edge edge, int startIndex, int numPoints )
            {
                Edge = edge;
                StartIndex = startIndex;
                NumPoints = numPoints;
            }

            internal int EndIndex
            {
                get
                {
                    return StartIndex + NumPoints;
                }
            }

        }

        /// <summary>
        /// Find the index of point farthest given a hull task.
        /// </summary>
        /// <param name="p0">The start of the line.</param>
        /// <param name="p1">The end of the line.</param>
        /// <param name="points">Array of points to test.</param>
        /// <returns>Index of the farthest point or -1 if none is found (e.g. all points are in the line).</returns>
        private int IndexOfFarthestPoint( HullTask task )
        {
            Vector2d p0 = task.Edge.StartPoint;
            Vector2d n01 = task.Edge.Normal;

            float maxDistanceSqd = 0.0f;
            int   maxIndex = -1;
            int   endIndex = task.EndIndex;
            for( int i = task.StartIndex; i < endIndex; ++i )
            {
                Vector2d p0i = Common.Subtract( this._vertices[ i ], p0 );
                float distanceSqd = Common.DotProduct( p0i, n01 );
                if( distanceSqd - maxDistanceSqd > Epsilon )
                {
                    maxIndex    = i;
                    maxDistanceSqd = distanceSqd;
                }
            }
            return maxIndex;
        }

        /// <summary>
        /// An iterative version of QuickHull algorithm.
        /// Variable "vertices" is used as in-place storage during calculation
        /// (removing the need to create additional array for every iteration
        /// and removing too many copying).  However this means during the calculation
        /// the value of vertices are transient/not valid.
        /// </summary>
        /// <param name="points">The set of points to calculate the hull.</param>
        protected void IterativeQuickHull( Vector2d[] points )
        {
            Debug.Assert( points.Length >= 3, "QuickHull require at least 3 points" );
            this._vertices = new Vector2d[ points.Length ];
            points.CopyTo( this._vertices, 0 );

            // Find the leftmost (p0) and rightmost (p1) points.
            Vector2d p0 = this._vertices[ 0 ];
            Vector2d p1 = p0;
            int farthestPointIndex = 0;

            for( int i = 1; i < this._vertices.Length; ++i )
            {
                Vector2d pi = this._vertices[ i ];
                if( pi.X < p0.X || ( pi.X == p0.X && pi.Y < p0.Y ) )
                {
                    p0 = pi;
                }
                else if( pi.X > p1.X || ( pi.X == p1.X && pi.Y > p1.Y ) )
                {
                    p1 = pi;
                    farthestPointIndex = i;
                }
            }

            Stack taskStack = new Stack();
            {
                HullTask mainTask = new HullTask();
                mainTask.Edge.StartPoint = p0;
                mainTask.Edge.EndPoint = p0;
                mainTask.StartIndex = 0;
                mainTask.NumPoints = this._vertices.Length;
                taskStack.Push( mainTask );
                // taskStack.Push( new HullTask( new Edge( p0, p0 ), 0, points.Length ) );

            }
            bool firstEntry = true;

            ArrayList hull = new ArrayList( this._vertices.Length );
            //hull.Add( p0 );

            while( taskStack.Count > 0 )
            {
                HullTask task = (HullTask) taskStack.Pop();

                // hull.Add( task.Edge.EndPoint );   // start point is on the hull

                if( task.NumPoints == 0 ||
                    ( task.NumPoints == 1 && this._vertices[ task.StartIndex ] == task.Edge.EndPoint ) )
                {
                    hull.Add( task.Edge.StartPoint );
                }
                else
                {
                    if( firstEntry )
                    {
                        firstEntry = false; // next time around, call IndexOffarthestPoint
                    }
                    else
                    {   // if not first entry, calculate the index of farthest point
                        farthestPointIndex =  IndexOfFarthestPoint( task );
                    }

                    if( farthestPointIndex < 0 )
                    {
                        hull.Add( task.Edge.StartPoint );
                    }
                    else
                    {
                        Vector2d farthestPoint = this._vertices[ farthestPointIndex ];
                        Edge edge0 = new Edge( task.Edge.StartPoint, farthestPoint );
                        Edge edge1 = new Edge( farthestPoint, task.Edge.EndPoint );
                        // int numPointsLeft = task.NumPoints;
                        int leftPlacement = task.StartIndex;
                        int rightPlacement = task.EndIndex - 1;

                        // Move all points which are outside the first edge to the left of the
                        // range, and move all the points outside the second edge to the right
                        // end of the range.  All the points which are inside any of the edge
                        // will be discarded (i.e. overwritten).
                        this.MovePoints(ref leftPlacement, ref rightPlacement, edge0, edge1);
                        
                        // Push new task to the stack.   The right part first.
                        ++rightPlacement;   // the start of the right array
                        
                        taskStack.Push( new HullTask( edge1, rightPlacement, task.EndIndex - rightPlacement ) );
                    
                        if( leftPlacement > task.StartIndex )
                        {
                            taskStack.Push( new HullTask( edge0, task.StartIndex, leftPlacement - task.StartIndex ) );
                        }
                        else
                        {
                            hull.Add( task.Edge.StartPoint );   // Optimization to save one iteration.
                        }                        
                    }
                }
            }

            this._vertices = new Vector2d[ hull.Count ];
            for( int i = 0; i < hull.Count; ++i )
            {
                this._vertices[ i ] = (Vector2d) hull[ i ];
            }            
        }
        
        /// <summary>
        /// Helper function to be called by IterativeQuickHull()
        /// Move all points which are outside the first edge to the left of the
        /// range, and move all the points outside the second edge to the right
        /// end of the range.  All the points which are inside any of the edge
        /// will be discarded (i.e. overwritten).
        /// </summary>
        private void MovePoints(ref int leftPlacement, ref int rightPlacement,
                                Edge edge0, Edge edge1)
        {
            int leftIndex = leftPlacement;
            int rightIndex = rightPlacement;

            while( leftIndex <= rightIndex ) // not yet cross
            {
                // Advance the leftIndex until a point inside edge0 is found. 
                while( ( leftIndex < rightIndex ) && edge0.IsOutside( this._vertices[ leftIndex ] ) )
                {
                    if( leftPlacement != leftIndex )    
                    {   // compacting
                        this._vertices[ leftPlacement ] = this._vertices[ leftIndex ];
                    }
                    ++leftIndex;
                    ++leftPlacement;
                    // --numPointsLeft;
                }

                // Advance the rightIndex until a point inside edge1 is found.
                while( ( leftIndex < rightIndex ) && edge1.IsOutside( this._vertices[ rightIndex ] ) )
                {
                    if( rightPlacement != rightIndex )
                    {
                        this._vertices[ rightPlacement ] = this._vertices[ rightIndex ];
                    }
                    --rightIndex;
                    --rightPlacement;
                    // --numPointsLeft;
                }

                // Possibly need to switch an element.
                if( leftIndex <= rightIndex )    // two pointers have not met
                {
                    // now switch points[ leftIndex ] with points[ rightIndex ].
                    Vector2d tmp = this._vertices[ rightIndex ];

                    if( edge1.IsOutside( this._vertices[ leftIndex ] ) )
                    {
                        this._vertices[ rightPlacement ] = this._vertices[ leftIndex ];
                        --rightPlacement;
                        //--rightIndex;
                    }

                    if( edge0.IsOutside( tmp ) )
                    {
                        this._vertices[ leftPlacement ] = tmp;
                        ++leftPlacement;
                    }

                    ++leftIndex;
                    --rightIndex;
                }
            }  
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Testing whether two hull are the same.
        /// </summary>
        /// <param name="hull1">The array of points of the first convex hull.</param>
        /// <param name="hull2">The array of points of the second convex hull.</param>
        /// <returns>True if both convex hull are the same (same points and ordering).</returns>
        private static bool IsSameHull( Vector2d[] hull1, Vector2d[] hull2 )
        {
            if( hull1.Length != hull2.Length )
            {
                return false;
            }
            for( int i = 0; i < hull1.Length; ++i )
            {
                if( hull1[ i ] != hull2[ i ] )
                {
                    return false;
                }
            }
            return true;
        }
#endif // INTERNAL_PARSER

        #endregion Non-recursive (iterative) QuickHull Algorithm
        #endregion Convex Hull Calculations

        /// <summary>
        /// Function to make line into skinny triangle by adding
        /// a "third" point. This is
        /// to ensure that the output/input of convex polygon
        /// calculation actually a region (not just a line).
        /// </summary>
        /// <param name="points">Array of 2 points</param>
        /// <returns>The new array of 2 points</returns>
        public static Vector2d[] MakeIntoTriangle( Vector2d[] points )
        {
            Debug.Assert( points.Length == 2, "Should have 2 points" );

            Vector2d pt1 = points[ 0 ];
            Vector2d pt2 = points[ 1 ];
            if( pt1 == pt2 )    // Very special case, it is only a dot.
            {
                pt2.X = pt1.X + 0.01f;  // Move it slightly to the right to avoid zero size
            }
            Vector2d  v12 = Common.Subtract( pt2, pt1 );
            Vector2d vPerp = Common.Normal( v12 );
            double vPerpLength = Common.Length( vPerp );
            vPerp.Width  = (float) ( 0.001 * vPerp.Width / vPerpLength );
            vPerp.Height = (float) ( 0.001 * vPerp.Height / vPerpLength );

            Vector2d[] vtx = new Vector2d[ 3 ];
            vtx[ 0 ] = pt1;
            vtx[ 1 ] = pt2;
            vtx[ 2 ] = new Vector2d(
                pt1.X + 0.5f * v12.Width + vPerp.Width,
                pt1.Y + 0.5f * v12.Height + vPerp.Height
                );

            return vtx;
        }

        #endregion

        #region Rotating calipers
        /// Rotating Caliper is a paradigm used to solve a number of Computational Geometry problems.
        /// It was introduced by Shamos, and the Toussaint coinned the term.
        /// A good overview of Rotating Caliper algorithm can be found in:
        /// Pirzadeh, Hormoz.  "Computational Geometry with the Rotating Calipers". Master Thesis,
        /// McGill University.
        /// Online reference on Rotating Caliper:
        ///     http://cgm.cs.mcgill.ca/~orm/rotcal.frame.html
        /// Overview of the convex polygon intersection algorithm:
        ///     http://www-perso.iro.umontreal.ca/~plante/compGeom/algorithm.html


        /// <summary>
        /// Test whether two convex polygon intersect.
        /// This test uses the rotating caliper paradigm (see the reference for convex polygon 
        /// intersection algorithm above):
        ///     1. first finding a bridge/pocket, i.e. where the rotating calipers switch
        ///        ordering.  
        ///     2. A bridge/pocket is a line connecting two vertices so that it creates an edge
        ///        of the convex hull for both convex polygon.  This bridge with edges of the two
        ///        convex polygon creates a "sail" (the bridge as the "mast" and the edges
        ///        of the two polygons create concave edges for the sail).  Note: From the mast,
        ///        traversing the two polygon should be done in opposite direction, one advancing (counter clockwise),
        ///        the other is backward (clockwise).  The direction is determined by the relative position of
        ///        one edge over other polygon vertex when the bridge is found.
        ///     3. The intersection (if any) can be found by tracing the concave edges of the sail.
        ///        Note: it does not need to find all bridges/pockets, once it finds a pocket it can
        ///        determine whether the polygons intersect.
        ///        
        /// </summary>
        /// <param name="p">The first convex polygon.</param>
        /// <param name="q">The second convex polygon.</param>
        /// <returns></returns>
        static private bool IntersectionTest(ConvexPolygon p, ConvexPolygon q)
        {
            // Quick check, if the bounding boxes are not intersecting then both of the 
            // convex polygons are not intersecting.
            if (!p.Bound.IntersectsWith(q.Bound))
            {
                return false;
            }

            // 
            // Compute the first pocket/bridge
            // 
            int pStart = 0;
            int qStart = 0;
#if DEBUG
            Debug.Assert(p.LeftmostVertexIndex() == pStart, "Convex polygon not created through our function");
            Debug.Assert(q.LeftmostVertexIndex() == qStart, "Convex polygon not created through our function");
#endif // DEBUG
            // Indices where caliper is attached to.
            int pi = pStart;
            int qi = qStart;
            int piNext = p.NextIndex(pi);
            int qiNext = q.NextIndex(qi);
            // The first caliper is a downard caliper (parallel to Y axis, going down or -PI/2).
            Vector2d caliper = new Vector2d(0, -1); 
            // The position of the vertex in convex polygon Q with respect to current
            // caliper in P. (== whether the current vertex in Q is on the left of 
            // the caliper in P).
            bool isQLeftOfP = Common.TriArea(p[pi], p[pi] + caliper, q[qi]) > 0;
            Vector2d pu = p[piNext] - p[pi];
            Vector2d qu = q[qiNext] - q[qi];
            // Edge angles to determine next caliper position.
            double pAngle = Math.Atan2(pu.Y, pu.X);
            double qAngle = Math.Atan2(qu.Y, qu.X);
            double pNextAngle = pAngle;
            double qNextAngle = qAngle;
            bool doneP = false;
            bool doneQ = false;
#if DEBUG
            int maxStep = p.NumVertices + q.NumVertices;
            int iStep = 0;
#endif
            do
            {
                // To ensure that the algorithm does not go into infinite loop.
#if DEBUG
                Debug.Assert(++iStep <= maxStep, "Something wrong, more steps than number of vertices");
#endif
                // Candidate for the bridge.
                int pBridge = pi;
                int qBridge = qi;
                // Use edge of P as the next caliper (advance to the next P vertices)
                if (pAngle <= qAngle)
                { 
                    caliper = pu;
                    pi = piNext;
                    piNext = p.NextIndex(pi);
                    pu = p[piNext] - p[pi];
                    pNextAngle = Math.Atan2(pu.Y, pu.X);
                    // Detectinig whether we are already pass the beginning.
                    if (pNextAngle < pAngle)
                    {   // If we do then we need to add 2 PI.
                        pNextAngle += 2 * Math.PI;
                        doneP = (pNextAngle > 3.0 * Math.PI / 2.0);   // Pass the first position (caliper > 3/2 PI)
                    }
                } 

                // Use edge of Q as the next caliper (advance to the next Q vertices)
                // Note the "<=" sign, so we may advance both on P & Q if they have the
                // same angles.
                if (qAngle <= pAngle)
                {
                    caliper = qu;
                    qi = qiNext;
                    qiNext = q.NextIndex(qi);
                    qu = q[qiNext] - q[qi];
                    qNextAngle = Math.Atan2(qu.Y, qu.X);
                    if (qNextAngle < qAngle)
                    {
                        qNextAngle += 2 * Math.PI;
                        doneQ = (qNextAngle > 3.0 * Math.PI / 2.0); // Pass the first position (caliper > 3/2 PI) 
                    }
                }
            
                pAngle = pNextAngle;
                qAngle = qNextAngle;
                // Now detect whether vertex in Q switches position with respect to caliper in P from
                // previous step.
                bool newIsQLeftOfP = Common.TriArea(p[pi], p[pi] + caliper, q[qi]) > 0;
                if (isQLeftOfP != newIsQLeftOfP)
                {
                    // Found a pocket/bridge
                    if (isQLeftOfP)
                    {
                        return IsSailIntersecting(p, q, pBridge, qBridge);
                    }
                    else
                    {
                        return IsSailIntersecting(q, p, qBridge, pBridge);
                    }
                }
            } while (!(doneP && doneQ));
            // No pocket or bridge.
            // Meaning one is properly inside the other.
            return true;
        }


        /// <summary>
        /// Test whether a sail edge of two different polygon edges intersecting each other.
        /// </summary>
        /// <param name="p">The first polygon which need to be traverse in counter clockwise order (advancing)</param>
        /// <param name="q">The second polygon which need to be traversed in clockwise order (decreasing index)</param>
        /// <param name="pBridge">Index of the bridge on P</param>
        /// <param name="qBridge">Index of the bridge on Q</param>
        /// <returns></returns>
        static public bool IsSailIntersecting(
            ConvexPolygon p, ConvexPolygon q,
            int pBridge, int qBridge)   // P should be advancing
        {
            int pi = pBridge;
            int pNext = p.NextIndex(pi);
            int qi = qBridge;
            int qPrev = q.PrevIndex(qi);

            bool finished;
            do
            {
                finished = true;
                while (Common.TriArea(p[pi], p[pNext], q[qPrev]) < 0.0)
                {
                    qi = qPrev;
                    if (qi == qBridge)
                    {
                        return false;
                    }
                    qPrev = q.PrevIndex(qi);
                    finished = false;
                }
                while (Common.TriArea(q[qi], q[qPrev], p[pNext]) > 0.0)
                {   
                    pi = pNext;
                    if (pi == pBridge)
                    {
                        return false;
                    }
                    pNext = p.NextIndex(pi);
                    finished = false;                       
                }
            } 
            while (!finished);

            // Intersection is found.
            // The actual location of intersection can be found by intersecting line segments:
            // line (p[pi], p[piNext])  line (q[qPrev], q[qi])
            return true;
            
        }

     #endregion
    }
}
