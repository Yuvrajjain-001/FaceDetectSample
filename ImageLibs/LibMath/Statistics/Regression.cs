//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
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
    /// Class representing the result of 2D linear Total Least-Squares (TLS) regression,
    /// also know as 2D principle component analysis (PCA).
    /// </summary>
    internal class RegressionResult
    {
        #region Fields
        private Vector2d _centroid;
        private Angle _angle; // direction of the fitting line
        private double _ssdError; // sum of squared residual error
        private double _eigenRatio; // ratio between the max and min eigen values
        #endregion Fields

        #region Properties
        /// <summary>
        /// The centroid of the point set.
        /// </summary>
        public Vector2d Centroid
        {
            get { return this._centroid; }
        }
        /// <summary>
        /// The orientation angle of the TLS fitting line.
        /// </summary>
        public Angle Angle
        {
            get { return this._angle; }
        }
        /// <summary>
        /// Sum of squared residual error (deviation from the regression line)
        /// </summary>
        public double SsdError
        {
            get { return this._ssdError; }
        }
        /// <summary>
        /// The smaller eigen value divided by the larger eigen value.
        /// One indication of TLS fitting quality.
        /// </summary>
        public double EigenRatio
        {
            get { return this._eigenRatio; }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// Default constructor: make private to disallow access.
        /// </summary>
        private RegressionResult()
        {
        }

        /// <summary>
        /// Constructor: convert an IncrementalRegressionResult to RegressionResult
        /// </summary>
        public RegressionResult( IncrementalRegressionResult incrementalResult )
        {
            Debug.Assert( incrementalResult.NumPoints > 0,
                "IncrementalResult must contain at least one point to be converted to RegressionResult." );

            Vector2d centroid = new Vector2d(
                (float) incrementalResult.SumX / incrementalResult.NumPoints,
                (float) incrementalResult.SumY / incrementalResult.NumPoints );

            double cxx = incrementalResult.SumXX
                - incrementalResult.SumX * incrementalResult.SumX
                / incrementalResult.NumPoints;
            double cxy = incrementalResult.SumXY
                - incrementalResult.SumX * incrementalResult.SumY
                / incrementalResult.NumPoints;
            double cyy = incrementalResult.SumYY
                - incrementalResult.SumY * incrementalResult.SumY
                / incrementalResult.NumPoints;

            double a = cxx + cyy;
            double b = cxx * cyy - cxy * cxy;
            double c = Math.Sqrt( a*a - 4*b );

            double minEigenVal = (a-c)/2;
            double maxEigenVal = (a+c)/2;

            double eigenRatio;
            if (maxEigenVal > Common.Epsilon)
            {
                eigenRatio = minEigenVal / maxEigenVal;
            }
            else
            {
                eigenRatio = 0;
            }

            a = maxEigenVal - cxx;
            double majorEigenVecNorm = Math.Sqrt( cxy*cxy + a*a );

            Angle majorAxis;
            if (majorEigenVecNorm < Common.Epsilon)
            {
                majorAxis = new Angle(0.0);
            }
            else
            {
                majorAxis = new Angle( new Vector2d(
                    cxy / majorEigenVecNorm,
                    a / majorEigenVecNorm) );

                // FUTURE-2004/09/07-MingYe --
                // Regulate the regression line orientation to [-PI/2, PI/2) so that
                // the display of block bounding boxes is mostly correct.
                // Remove this part after writing direction detection is added.
                majorAxis = majorAxis.ToHalfPi();
            }

            this._centroid = centroid;
            this._eigenRatio = eigenRatio;
            this._angle = majorAxis;
            double ssdError = cxx * majorAxis.Sin * majorAxis.Sin
                + cyy * majorAxis.Cos * majorAxis.Cos
                - 2 * cxy * majorAxis.Sin * majorAxis.Cos;
            
            Debug.Assert( ssdError > -Utility.RealEpsilon,
                "Sum of squared regression error must be nonnegative" );

            if ( ssdError < 0 )
            {
                ssdError = 0.0;
            }
	    this._ssdError = ssdError;
        }

        /// <summary>
        /// Constructor: compute RegressionResult from an array of 2D points.
        /// </summary>
        public RegressionResult( Vector2d[] points ) :
            this( new IncrementalRegressionResult( points ) )
        {
        }

        /// <summary>
        /// Constructor: compute RegressionResult from an arraylist of 2D points.
        /// </summary>
        public RegressionResult( ArrayList points ) :
            this( new IncrementalRegressionResult( points ) )
        {
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Constructor: compute RegressionResult from an array of IncrementalRegressionResult.
        /// </summary>
        public RegressionResult( IncrementalRegressionResult[] incrementalResults ) :
            this( IncrementalRegressionResult.Sum( incrementalResults ) )
        {
        }

        /// <summary>
        /// Rotate the orientation angle (major axis) of the fit by PI.
        /// </summary>
        public void ReverseMajorAxis()
        {
            this._angle = new Angle( new Vector2d( -this._angle.Cos, -this._angle.Sin ) );
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Operator equality.
        /// </summary>
        public static bool operator== (RegressionResult lhs, RegressionResult rhs)
        {
            return ( lhs.Centroid == rhs.Centroid &&
                     lhs.Angle == rhs.Angle &&
                     MathLibrary.Common.IsWithinEpsilon(lhs.SsdError, rhs.SsdError) &&
                     MathLibrary.Common.IsWithinEpsilon(lhs.EigenRatio, rhs.EigenRatio) );
        }

        /// <summary>
        /// Operator inequality.
        /// </summary>
        public static bool operator!= (RegressionResult lhs, RegressionResult rhs)
        {
            return !( lhs == rhs );
        }

        /// <summary>
        /// Override object's Equal
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
			return this == (obj as RegressionResult);
		}

        /// <summary>
        /// Override object's GetHashCode
        /// </summary>
        /// <returns>The hash code of cos and sin.</returns>
        public override int GetHashCode()
        {
            return this._angle.GetHashCode() + this._centroid.GetHashCode();
        }
        
        #endregion Methods
    }


    /// <summary>
    /// Class storing statistics for computing 2D linear Total Least-Squares (TLS) regression,
    /// also know as 2D principle component analysis (PCA), in an incremental fashion.
    /// </summary>
    internal class IncrementalRegressionResult
    {
        #region Fields
        private int     _numPoints;
        private double  _sumX; // sum of x
        private double  _sumY; // sum of y
        private double  _sumXX; // sum of x * x
        private double  _sumYY; // sum of y * y
        private double  _sumXY; // sum of x * y
        #endregion Fields

        #region Properties
        /// <summary>
        /// Number of points accumulated so far
        /// </summary>
        public int NumPoints
        {
            get { return this._numPoints; }
        }
        /// <summary>
        /// Sum of the x coordinates of all points
        /// </summary>
        public double SumX
        {
            get { return this._sumX; }
        }
        /// <summary>
        /// Sum of the y coordinates of all points
        /// </summary>
        public double SumY
        {
            get { return this._sumY; }
        }
        /// <summary>
        /// Sum of the squared x coordinates of all points
        /// </summary>
        public double SumXX
        {
            get { return this._sumXX; }
        }
        /// <summary>
        /// Sum of the squared y coordinates of all points
        /// </summary>
        public double SumYY
        {
            get { return this._sumYY; }
        }
        /// <summary>
        /// Sum of the x * y product of all points
        /// </summary>
        public double SumXY
        {
            get { return this._sumXY; }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// Do not expose this constructor to avoid passing in arbitrary values.
        /// </summary>
        private IncrementalRegressionResult(
            int numPoints,
            double sumX,
            double sumY,
            double sumXX,
            double sumYY,
            double sumXY )
        {
            this._numPoints = numPoints;
            this._sumX = sumX;
            this._sumY = sumY;
            this._sumXX = sumXX;
            this._sumYY = sumYY;
            this._sumXY = sumXY;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IncrementalRegressionResult() :
            this(0, 0.0, 0.0, 0.0, 0.0, 0.0)
        {
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public IncrementalRegressionResult(IncrementalRegressionResult other) :
            this( other.NumPoints,
                  other.SumX,
                  other.SumY,
                  other.SumXX,
                  other.SumYY,
                  other.SumXY )
        {
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Constructor from an array of  IncrementalRegressionResult.
        /// </summary>
        public static IncrementalRegressionResult Sum(IncrementalRegressionResult[] incrementalResults)
        {
            IncrementalRegressionResult sum = new IncrementalRegressionResult();

            foreach (IncrementalRegressionResult irr in incrementalResults)
            {
                sum += irr;
            }

            return sum;
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Constructor: create IncrementalRegressionResult from an array of points.
        /// </summary>
        public IncrementalRegressionResult( Vector2d[] points ): this()
        {
            AddPoints( points );
        }

        /// <summary>
        /// Constructor: create IncrementalRegressionResult from an arraylist of points.
        /// </summary>
        public IncrementalRegressionResult( ArrayList points ):
            this( (Vector2d[]) points.ToArray( typeof(Vector2d) ) )
        {
        }


        /// <summary>
        /// Addition operator.
        /// </summary>
        /// <param name="lhs">The first Incremental Regression Result</param>
        /// <param name="rhs">The second Incremental Regression Result</param>
        /// <returns></returns>
        public static IncrementalRegressionResult operator + (
            IncrementalRegressionResult lhs,
            IncrementalRegressionResult rhs)
        {
            return new IncrementalRegressionResult(
                lhs.NumPoints + rhs.NumPoints,
                lhs.SumX + rhs.SumX,
                lhs.SumY + rhs.SumY,
                lhs.SumXX + rhs.SumXX,
                lhs.SumYY + rhs.SumYY,
                lhs.SumXY + rhs.SumXY );
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Deduction operator.
        /// </summary>
        /// <param name="lhs">The first Incremental Regression Result</param>
        /// <param name="rhs">The second Incremental Regression Result</param>
        /// <returns></returns>
        public static IncrementalRegressionResult operator - (
            IncrementalRegressionResult lhs,
            IncrementalRegressionResult rhs)
        {
            return new IncrementalRegressionResult(
                lhs.NumPoints - rhs.NumPoints,
                lhs.SumX - rhs.SumX,
                lhs.SumY - rhs.SumY,
                lhs.SumXX - rhs.SumXX,
                lhs.SumYY - rhs.SumYY,
                lhs.SumXY - rhs.SumXY );
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Add a point
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint( Vector2d point )
        {
            this._numPoints ++;
            this._sumX += point.X;
            this._sumY += point.Y;
            this._sumXX += point.X * point.X;
            this._sumYY += point.Y * point.Y;
            this._sumXY += point.X * point.Y;
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Remove a point
        /// </summary>
        /// <param name="point"></param>
        public void RemovePoint( Vector2d point )
        {
            this._numPoints --;
            this._sumX -= point.X;
            this._sumY -= point.Y;
            this._sumXX -= point.X * point.X;
            this._sumYY -= point.Y * point.Y;
            this._sumXY -= point.X * point.Y;
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Add an array of points
        /// </summary>
        /// <param name="point"></param>
        public void AddPoints( Vector2d[] points )
        {
            foreach (Vector2d point in points)
            {
                AddPoint( point );
            }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Add an arraylist of points
        /// </summary>
        /// <param name="point"></param>
        public void AddPoints( ArrayList points )
        {
            AddPoints( (Vector2d[]) points.ToArray( typeof(Vector2d) ) );
        }

        /// <summary>
        /// Remove a set of points
        /// </summary>
        /// <param name="point"></param>
        public void RemovePoints( Vector2d[] points )
        {
            foreach (Vector2d point in points)
            {
                RemovePoint( point );
            }
        }

        /// <summary>
        /// Remove an arraylist of points
        /// </summary>
        /// <param name="point"></param>
        public void RemovePoints( ArrayList points )
        {
            RemovePoints( (Vector2d[]) points.ToArray( typeof(Vector2d) ) );
        }
#endif // INTERNAL_PARSER        
        #endregion Methods
    }

}
