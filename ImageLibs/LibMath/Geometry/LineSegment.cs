//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // Debug functionalities.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    using PointF = Vector2d;
    using SizeF = Vector2d;
    using RectangleF = Rectangle2d;

    /// <summary>
    /// Class for representing a line segment which has a start point and a stop point.
    /// </summary>
    public struct LineSegment
    {
        #region Fields
        private Vector2d point;
        private SizeF size;
        #endregion // Fields

        #region Properties
        /// <summary>
        /// The start point of this line segment.
        /// </summary>
        public Vector2d StartPoint
        {
            get
            {
                return this.point;
            }
            set
            {
                this.point = value;
            }
        }

        /// <summary>
        /// The stop point of this line segment.
        /// </summary>
        public Vector2d StopPoint
        {
            get
            {
                return Common.Add(this.point, this.size);
            }
            set
            {
                this.size = Common.Subtract(value, this.point);
            }
        }

        /// <summary>
        /// The size (cx, cy) description of the line segment.
        /// </summary>
        public SizeF Size
        {
            get
            {
                return this.size;
            }
        }

        /// <summary>
        /// The length of the line segment.
        /// </summary>
        public double Length
        {
            get
            {
                return Common.Length(this.size);
            }
        }

        /// <summary>
        /// The polar angle of the line segment.
        /// </summary>
        public double PolarAngle
        {
            get
            {
                Debug.Assert(this.size.IsEmpty == false, "Length is zero");
                return Math.Atan2(this.size.Height, this.size.Width);
            }
        }
        #endregion // Properties

        #region Methods
        /*
        public LineSegment(Vector2d point, SizeF size)
        {
            this.point = point;
            this.size = size;
        }
        */

        public LineSegment(Vector2d point1, Vector2d point2)
        {
            this.point = point1;
            this.size = Common.Subtract(point2, point1);
        }

        public override int GetHashCode()
        {
            return this.point.GetHashCode() ^ this.size.GetHashCode();
        }
        
        public override bool Equals(Object other)
        {
            if (other is LineSegment)
            {
                LineSegment line = (LineSegment) other;
                return (this == line);
            }
            return false;
        }

        public static bool operator==(LineSegment line1, LineSegment line2)
        {
            return (line1.StartPoint == line2.StartPoint && line1.Size == line2.Size);
        }    

        public static bool operator!=(LineSegment line1, LineSegment line2)
        {
            return (line1.StartPoint != line2.StartPoint || line1.Size != line2.Size);
		}

#if INTERNAL_PARSER        
        /// <summary>
        /// Offset the line segment.
        /// </summary>
        /// <param name="size">The amount to offset.</param>
        public void Offset(SizeF size)
        {
            this.point = Common.Add(this.point, size);
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Calculate the interpolated point.
        /// </summary>
        /// <param name="lambda"></param>
        /// <returns>The interpolated point.</returns>
        private Vector2d GetLambdaPoint(double lambda)
        {
            return Common.Add(this.point, Common.Scale(this.size, lambda));
        }
        
        /// <summary>
        /// Calculate the lambda of a point's projection on a line.
        /// The projection may be outside of the line segment.
        /// </summary>
        /// <param name="point0"></param>
        /// <returns>The projection point's lambda.</returns>
        public double GetInfiniteLambdaOfPoint(Vector2d point0)
        {
            return Common.Divide(Common.Subtract(point0, this.point), this.size);
        }
        
        /// <summary>
        /// Calculate the signed distance of a point from a line.
        /// The line's left side is positive, right side is negative.
        /// </summary>
        /// <param name="point0"></param>
        /// <returns>The signed distance.</returns>
        public double SignedDistFromInfiniteLine(Vector2d point0)
        {
            double tmp = Common.Length(this.size);
            if (tmp == 0)
            {
                return Common.Distance(point0, this.point);
            }
            return Common.PerpDotProduct(this.size, Common.Subtract(point0, this.point)) / tmp;
        }

        /// <summary>
        /// Calculate the signed distance and lambda at the same time
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="dist">The signed distance</param>
        /// <param name="lambda">The lambda</param>
        public void GetSignedDistAndLambdaOfProjection(Vector2d point0, out double dist, out double lambda)
        {
            double normalSquare = Common.DotProduct(this.size, this.size);

            if (normalSquare == 0.0)
            {
                // this.size == 0.0
                dist = Common.Distance(point0, this.point);
                lambda = 0.5;
                return;
            }

            SizeF offset = Common.Subtract(point0, this.point);

            dist = Common.PerpDotProduct(this.size, offset) / Math.Sqrt(normalSquare);
            lambda = Common.DotProduct(this.size, offset) / normalSquare;
        }
        
        /// <summary>
        /// Calculate the distance of a point from a line.
        /// The distance is always positive.
        /// </summary>
        /// <param name="point0"></param>
        /// <returns></returns>
        public double DistFromInfiniteLine(Vector2d point0)
        {
            double dist = SignedDistFromInfiniteLine(point0);
            return (dist >= 0 ? dist : -dist);
        }

        /// <summary>
        /// Calculate the distance of a point from a line segment.
        /// If projection of the point is outside of the line segment,
        /// the distance will be from one of the line segment's endcaps to the point.
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="lambda"></param>
        /// <returns></returns>
        public double DistFromLine(Vector2d point0, out double lambda)
        {
            lambda = 0;
            
            if (!size.IsEmpty)
            {
                lambda = GetInfiniteLambdaOfPoint(point0);
            }

            if (lambda <= 0)
            {
                lambda = 0;
                return Common.Distance(point0, this.point);
            }
            if (lambda >= 1)
            {
                lambda = 1;
                return Common.Distance(point0, Common.Add(this.point, this.size));
            }
            
            return DistFromInfiniteLine(point0);
        }

        /// <summary>
        /// Calculate the distance of a point from a line segment.
        /// If projection of the point is outside of the line segment,
        /// the distance will be from one of the line segment's endcaps to the point.
        /// </summary>
        /// <param name="point0"></param>
        /// <returns></returns>
        public double DistFromLine(Vector2d point0)
        {
            double lambda;
            return DistFromLine(point0, out lambda);
        }

        /// <summary>
        /// Calculate the projection point of a point to a line segment.
        /// </summary>
        /// <param name="point0"></param>
        /// <returns></returns>
        public Vector2d ProjectPoint(Vector2d param_point)
        {
            double lambda = GetInfiniteLambdaOfPoint(param_point);
            return GetLambdaPoint(lambda);
        }

    
        #endregion // Methods
    }
}
