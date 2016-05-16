//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'> 
// Copyright (c) Microsoft Corporation. All Rights Reserved. 
// Information Contained Herein is Proprietary and Confidential. 
// </copyright> 
//
//------------------------------------------------------------------------------
using System;
using System.Collections;   // For ArrayList
using System.Diagnostics;   // Debug functionalities.
using System.Text;          // For StringBuilder.
using System.Globalization; // For CultureInfo.
using System.IO;

namespace System.Windows.Ink.Analysis.MathLibrary
{
    using PointF = Vector2d;

    /// <summary>
    /// Class for representing a polyline.
    /// </summary>
    public class Polyline
    {
        #region Fields
        /// <summary>
        /// Vertices array.
        /// </summary>
        protected PointF[] _vertices; 

        /// <summary>
        /// Bounding rectangle.
        /// </summary>
        protected Rectangle2d _bound;         

        protected double _length;
        private bool _isLengthCalculated;

#if INTERNAL_PARSER
        protected double _sumTurningAngle;
        private bool _isSumTurningAngleCalculated;
#endif // INTERNAL_PARSER


        #endregion

        #region Properties
        public Rectangle2d Bound
        {
            get
            {

#if DEBUG
                // This is put under #if DEBUG because FXCop complain that IsInitialized is never be called in retail.
                // But it won't compile if IsInitialized only avaiable on DEBUG build
                Debug.Assert( IsInitialized(), "Should be initialized before calling Bound" );
#endif
                if( this._bound.IsEmpty )
                {
                    this._bound = Common.CalculateBound( _vertices );
                }
                return this._bound;
            }
        }

        /// <summary>
        /// The vertices of the polyline.
        /// </summary>
        public PointF[] Vertices
        {
            get
            {
#if DEBUG
                // This is put under #if DEBUG because FXCop complain that IsInitialized is never be called in retail.
                // But it won't compile if IsInitialized only avaiable on DEBUG build
                Debug.Assert( IsInitialized(), "Should be initialized before getting vertices" );
#endif
                return _vertices;
            }
        }

        /// <summary>
        /// The number of vertices in this polyline.
        /// </summary>
        public int NumVertices
        {
            get
            {
                Debug.Assert( _vertices != null, "Can only call Length after being initialized" );
                return _vertices.Length;
            }
        }

        /// <summary>
        /// Indexer
        /// </summary>
        public PointF this[ int index ] // indexer
        {    
            get
            {
                Debug.Assert( _vertices != null, "Can only call Length after being initialized" );
                return _vertices[ index ];
            }
        }

        public double Length
        {
            get
            {
#if DEBUG
                // This is put under #if DEBUG because FXCop complain that IsInitialized is never be called in retail.
                // But it won't compile if IsInitialized only avaiable on DEBUG build
                Debug.Assert( IsInitialized(), "Should be initialized before calling Length" );
#endif
                if( !this._isLengthCalculated )
                {
                    this._length = 0.0;
                    for ( int i = 1; i < this.NumVertices; ++i) 
                    {
                        this._length += Common.Distance( _vertices[i-1], _vertices[i] );
                    }
                    this._isLengthCalculated = true;
                }
                return this._length;
            }
        }

#if INTERNAL_PARSER
        public double SumTurningAngle
        {
            get
            {
#if DEBUG
                Debug.Assert( IsInitialized(), "Should be initialized before calling SumTurningAngle" );
#endif
                if( !this._isSumTurningAngleCalculated )
                {
                    this._sumTurningAngle = 0;
                    if ( this.NumVertices >=3 )
                    {
                        Vector2d vector1 = Common.Subtract( _vertices[1], _vertices[0] );
                        for ( int i = 2; i < this.NumVertices-1; ++i) 
                        {
                            Vector2d vector2 = Common.Subtract( _vertices[i+1], _vertices[i] );
                            this._sumTurningAngle += new Angle( vector2 - vector1 ).Radian;
                            vector1 = vector2;
                        }
                    }
                    this._isSumTurningAngleCalculated = true;
                }
                return this._sumTurningAngle;
            }
        }
#endif // INTERNAL_PARSER

        #endregion

        #region Methods
        /// <summary>
        /// Public constructor.
        /// </summary>
        public Polyline()
        {

        }

        /// <summary>
        /// Constructor for default case, where the duplicate will be removed.
        /// This is the one used for representing stroke's polyline.
        /// </summary>
        /// <param name="points">Array of points.</param>
        public Polyline(PointF[] points)
        {
            InitializePoints(points, /* filterDuplicate = */ true);
        }

        /// <summary>
        /// Constructor with ability to specify whether to filter out duplicate.
        /// Some usage of polyline require it not to filter out duplicate (for
        /// example for representing the rotated bounding box, it always assumed
        /// to have four vertices).
        /// </summary>
        /// <param name="points">Array of points.</param>
        /// <param name="filterDuplicate">Whether to filter out duplicate.</param>
        public Polyline(PointF[] points, bool filterDuplicate)
        {
            InitializePoints(points, filterDuplicate);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">Other polyline to copy.</param>
        public Polyline( Polyline other )
        {
            _vertices = (PointF[]) other.Vertices.Clone();
        }

        /// <summary>
        /// Initialize a polyline with an array of points and whether to 
        /// filter out duplicates.
        /// This method is a helper method for the constructor.
        /// </summary>
        /// <param name="points">Array of points to use for this polyline.</param>
        /// <param name="filterDuplicate">Whether to filter out duplicate.</param>
        private void InitializePoints(PointF[] points, bool filterDuplicate)
        {
            Debug.Assert(points.Length > 0, "Polyline cannot have empty point");
            if (filterDuplicate)
            {
                _vertices = FilterOutDuplicatePoints(points);
            }
            else
            {
                _vertices = points;
            }

            if (this._vertices.Length == 1)
            {
                // Special case where stroke has only one point (small dot)
                // Make it into a very small line segment.
                _vertices = new PointF[2];
                _vertices[0] = _vertices[1] = points[0];
                _vertices[1].X += 0.001f;     // move end point slightly to the right.
            }
        }

#if DEBUG
        public bool IsInitialized()
        {
            return _vertices != null;
        }
#endif

#if INTERNAL_PARSER
        /// <summary>
        /// Get an array of the points in one segment of this polyline.
        /// </summary>
        /// <param name="index">The begin index of the segment.</param>
        /// <param name="count">The count of points in the segment.</param>
        /// <returns>The array of points.</returns>
        public PointF[] GetVertices(int index, int count)
        {
            if (count <= 0)
            {
                return new PointF[0];
            }

            Debug.Assert(index >= 0 && index + count <= NumVertices, "Invalid index or count");
            PointF[] verticesCopy = new PointF[count];

            for (int i = 0; i < count; i ++)
            {
                verticesCopy[i] = this[i + index];
            }

            return verticesCopy;
        }
#endif // INTERNAL_PARSER
        
        /// <summary>
        /// Add number of points as new vertices.
        /// </summary>
        /// <param name="newPoints">New points to add</param>
        public void AddVertices( PointF[] newPoints )
        {
            if( _vertices == null )
            {
                _vertices = (PointF[]) newPoints.Clone();
            }
            else
            {
                PointF[] newVertices = new PointF[ _vertices.Length + newPoints.Length ];
                _vertices.CopyTo( newVertices, 0 );
                newPoints.CopyTo( newVertices, _vertices.Length );
                _vertices = newVertices;
            }

            this._bound = Rectangle2d.Empty;       // Unitialize the bound.
        }


        /// <summary>
        /// What kind of peaks to find (to use with FindPeaks).
        /// </summary>
        public enum PeakMode
        {
            Min,
            Max,
            MinMax
        }

        /// <summary>
        /// Find a polyline's local maxima.  Use the hysteresis value to filter out "false" maxima, 
        /// that is, maxima that are less than the hysteresis height from the the previous minima.
        /// </summary>
        /// <param name="hysteresis">The hysteresis value (see above).</param>
        /// <param name="peakMode">What kind of peaks to find.</param>
        /// <returns>Array of index of the peaks.</returns>
        public int[] FindPeaks( double hysteresis, PeakMode peakMode )
        {
            int iDirection = 1;
            
            switch( peakMode )
            {
                case PeakMode.Max:
                    iDirection = 1;
                    break;

                case PeakMode.Min:
                    iDirection = -1;
                    break;

                case PeakMode.MinMax:
                {
                    int[] maxPeaks = FindPeaks( hysteresis, PeakMode.Max );
                    int[] minPeaks = FindPeaks( hysteresis, PeakMode.Min );
                    int[] results = new int[ maxPeaks.Length + minPeaks.Length ];
                    maxPeaks.CopyTo( results, 0 );
                    minPeaks.CopyTo( results, maxPeaks.Length );
                    Array.Sort( results );

#if DEBUG
                    for( int i = 0; i < results.Length - 1; ++i )
                    {
                        Debug.Assert( results[ i ] != results[ i + 1 ], "Duplicate peaks found" );
                    }
#endif 
                    return results;
                }
                
                default:
                    Debug.Assert( false, "Invalid peak mode" );
                    break;
            }

            Debug.Assert( peakMode == PeakMode.Max || peakMode == PeakMode.Min,
                "Invalid peak mode" );

            // No need to throw here because it is already done in
            // the above switch case.

            int iToggle = 0;

            double yPrevious;
            double yMin;
            double yMax;
            yPrevious = yMin = yMax = iDirection*_vertices[0].Y;

            double dbDiff;
            int iMin = 0;
            
            ArrayList peaks = new ArrayList( NumVertices );

            for (int i = 1; i < this.NumVertices; ++i) 
            {
                double y = iDirection*_vertices[i].Y;

                if (y < yMin) 
                {
                    yMin = y;
                    iMin = i;
                }
                if (y > yMax)
                {
                    yMax = y;
                }
                if (y > yPrevious)
                {
                    // y is currently increasing, so we are on the 
                    // lookout for a new peak that is above the given
                    // hysteresis
                    dbDiff = y - yMin;
                    if ((iToggle < 0) && (dbDiff > hysteresis))
                    {
                        // we found a peak, and it is not the first
                        // peak
                        int iSeg = iMin; 

                        //Note: reco used iSeg = (iLastBottom + iMin)/2
                        peaks.Add(iSeg);
                        
                        iToggle = 1;
                        yMax = y;
                    }
                    else if ((iToggle == 0) && (dbDiff > hysteresis)) 
                    {
                        iToggle = 1;
                    }
                }
                else
                {
                    // y is currently decreasing, so we are looking for
                    // a bottom
                    dbDiff = yMax - y;
                    if ((iToggle > 0) && (dbDiff > hysteresis)) 
                    {
                        iToggle = -1;
                        yMin = y;
                        iMin = i;
                    }
                    else if ((iToggle == 0) && (dbDiff > hysteresis)) 
                    {
                        iToggle = -1;
                    }
                }
                yPrevious = y;
            }
            return (int[]) peaks.ToArray( typeof( int ) );
        }




        /// <summary>
        /// Create a subsegment of this polyline.
        /// </summary>
        /// <param name="startIndex">Start index of the point.</param>
        /// <param name="numPoints">Number of points.</param>
        /// <returns>New polyline.</returns>
        public Polyline Subsegment( int startIndex, int numPoints )
        {
#if DEBUG
                // This is put under #if DEBUG because FXCop complain that IsInitialized is never be called in retail.
                // But it won't compile if IsInitialized only avaiable on DEBUG build
            Debug.Assert( IsInitialized(), "Unitialized polyline" );
#endif
            Debug.Assert( startIndex >= 0 && startIndex + numPoints <= NumVertices,
                "Invalid range" );
            PointF[] newVertices = new PointF[ numPoints ];
            Array.Copy( this._vertices, startIndex, newVertices, 0, numPoints );
            return new Polyline( newVertices );
        }


#if INTERNAL_PARSER
        /// <summary>
        /// Parse a string and return a new polyline structure.
        /// </summary>
        /// <param name="s">String to parse</param>
        /// <returns>Polyline</returns>
        public static Polyline Parse( string s, IFormatProvider formatProvider )
        {
            char[] separators = { ' ' };
            string[] coords = s.Split( separators );
         
            Debug.Assert( coords.Length % 2 == 0, "Each vertex should have x and y values" );
            
            int numVertices = coords.Length / 2;
            Debug.Assert( numVertices >= 2, "At least contains 2 vertices" );

            PointF[] vertices = new PointF[ numVertices ];
            for( int i = 0; i < numVertices; ++i )
            {
                vertices[ i ].X = float.Parse( coords[ 2*i ], formatProvider );
                vertices[ i ].Y = float.Parse( coords[ 2*i + 1 ], formatProvider );
            }
            return new Polyline( vertices );
        }

        /// <summary>
        /// Parse a string and return a polyline structure.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <returns>Polyline</returns>
        public static Polyline Parse( string s )
        {
            return Parse( s, CultureInfo.CurrentCulture );
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// String representation of the polyline.
        /// </summary>
        /// <param name="formatProvider">Format provider telling how to write based on the culture.</param>
        /// <returns>String </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            StringBuilder builder = new StringBuilder( 12 * this.NumVertices );
            foreach( PointF pt in this.Vertices )
            {
                builder.AppendFormat (formatProvider, "{0} {1} ", pt.X, pt.Y );
            }

            // Remove the last trailing blank/space.
            if( builder.Length >= 1 )
            {
                Debug.Assert( builder[ builder.Length - 1 ] == ' ', "The last character should be a trailing blank" );
                --builder.Length;
            }
            return builder.ToString();
        }

        public override string ToString ()
        {
            return ToString( CultureInfo.CurrentCulture );
        }

        public Polyline Resample( double dStep )
        {
            // resample step size = curve length / # segments.
            int nSegs = (int) ( this.Length / dStep + 0.5 );
            if ( nSegs == 0 )   // at least one segment
            {
                nSegs++;
            }
            dStep = this.Length / nSegs;

            double dRestLen = 0;
            PointF ptLast = _vertices[0];
            ArrayList newVertices = new ArrayList();
            for(int i = 1; i < NumVertices; i++)
            {
                PointF ptCurr = Vertices[i];
                double dCurLen = Common.Distance(ptLast, ptCurr);
                double dRestCurLen = dCurLen;
                double dCurPos = 0;
                while( dRestCurLen > dRestLen )
                {
                    dCurPos += dRestLen;
                    PointF point = Common.GetLambdaPoint(ptLast, ptCurr, dCurPos/dCurLen);
                    newVertices.Add(point);
                    dRestCurLen -= dRestLen;
                    dRestLen = dStep;
                }
                dRestLen = dRestLen - dRestCurLen;
                ptLast = ptCurr;
            }

            // add the last vertex
            if(newVertices.Count < nSegs+1)
            {
                Debug.Assert((newVertices.Count == nSegs));
                newVertices.Add(ptLast);
            }
            
            return new Polyline( (PointF[]) newVertices.ToArray( typeof( PointF ) ) );
        }

        /// <summary>
        /// Calculate upright bound after a rotation to this polyline. 
        /// </summary>
        /// <param name="center">Center of the rotation.</param>
        /// <param name="angle">Angle of rotation.</param>
        /// <returns>The bounding rectangle of the rotated polyline.</returns>
        public Rectangle2d CalculateRotatedBoundBox(PointF point, Angle angle)
        {
            PointF[] verticesCopy = (PointF[]) this._vertices.Clone();
            Common.RotatePoints(-angle, point, verticesCopy);
            return Common.CalculateBound(verticesCopy);
        }


        /// <summary>
        /// Filter out duplicate points.
        /// </summary>
        /// <param name="points">Array of points to filter.</param>
        /// <returns>The filtered out array</returns>
        private static PointF[] FilterOutDuplicatePoints(PointF[] points)
        {
            Debug.Assert(points.Length > 0,
                "Polyline should at least have one point");

            PointF[] filteredPoints = new PointF[points.Length];
            
            filteredPoints[0] = points[0];
            int filteredLength = 1;  // length of the filteredPoints array
            for (int i = 1; i < points.Length; ++i)
            {
                if (points[i] != points[i-1])   // Not the same as previous points
                {
                    filteredPoints[filteredLength++] = points[i];
                }
            }

            if (filteredLength == points.Length)
            {
                return filteredPoints;
            }
            else
            {
                Debug.Assert(filteredLength < points.Length, "Filtered points should have less points");
                PointF[] finalResults = new PointF[filteredLength];
                System.Array.Copy(filteredPoints, finalResults, filteredLength);
                return finalResults;
            }
        }
        #endregion

        private int _hashCode;
        public override int GetHashCode()
        {
            if (this._hashCode == 0)
            {
                unchecked
                {    // ignore any overflow/underflow here.
                    byte[] buffer = new byte[10];
                    MemoryStream ms = new MemoryStream(buffer);
                    using (BinaryWriter writer = new BinaryWriter(ms))
                    {
                        writer.Write((short) this.NumVertices);
                        writer.Write((short) this.Vertices[0].X);
                        writer.Write((short) this.Vertices[0].Y);
                        int last = this.NumVertices - 1;
                        writer.Write((short) (this.Vertices[last].X - this.Vertices[0].X));
                        writer.Write((short) (this.Vertices[last].Y - this.Vertices[0].Y));
                    }                
                    this._hashCode = Utility.Fnv1HashCode(buffer);
                }
            }
            return this._hashCode;
        }

        public override bool Equals(object otherObject)
        {
            Polyline otherPolyline = otherObject as Polyline;
            if (otherPolyline == null)
            {
                return false;
            }
            return this.Equals(otherPolyline);
        }

        public bool Equals(Polyline other)
        {
            const float tolerance = 0.5f;
            if (this.NumVertices != other.NumVertices)
            {
                return false;
            }
            for (int i = 0; i < this.NumVertices; ++i)
            {
                if (Math.Abs(this.Vertices[i].X - other.Vertices[i].X) > tolerance ||
                    Math.Abs(this.Vertices[i].Y - other.Vertices[i].Y) > tolerance)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
