//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
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

    /// <summary>
    /// Common functions for Math Library. Contains only static functions.
    /// </summary>
    public class Common
    {

        /// <summary>
        /// Make constructor private to avoid being instantiated.
        /// </summary>
        private Common()
        {
            // Nothing here.
        }

        /// <summary>
        /// Epsilon to compare double value.
        /// </summary>
        public static double Epsilon = 1e-6;
        public static float FloatEpsilon = 1e-6f;
        public static Real RealEpsilon = FloatEpsilon;

        public static bool IsWithinEpsilon(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Epsilon;
        }

        public static Vector2d Center(Rectangle2d rect)
        {
            return new Vector2d((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        }
        
        public static float DotProduct(Vector2d vector1, Vector2d vector2)
        {
            return vector1.Width * vector2.Width + vector1.Height * vector2.Height;
        }

        public static float PerpDotProduct(Vector2d vector1, Vector2d vector2)
        {
            return vector1.Width * vector2.Height - vector1.Height * vector2.Width;
        }

        public static Vector2d Scale(Vector2d vector, double scaleFactor)
        {
            float sf = (float) scaleFactor;
            return new Vector2d(vector.Width * sf, vector.Height * sf);
        }
        
        public static Vector2d Add(Vector2d point, Vector2d vector)
        {
            return new Vector2d(point.X + vector.Width, point.Y + vector.Height);
        }
        
        public static Vector2d Subtract(Vector2d point1, Vector2d point2)
        {
            return new Vector2d(point1.X - point2.X, point1.Y - point2.Y);
        }

        public static void Offset(ref Rectangle2d rect, Vector2d vector)
        {
            rect.Offset(new Vector2d(vector.Width, vector.Height));
        }

        public static Vector2d Normal(Vector2d vector)
        {
            return new Vector2d(vector.Height, -vector.Width);
        }

        public static double Length(Vector2d vector)
        {
            double x = vector.Width;
            double y = vector.Height;
            return Math.Sqrt(x * x + y * y);
        }

        public static Vector2d UnitNormal(Vector2d vector)
        {
            double length = Length(vector);
            Debug.Assert(length > FloatEpsilon, "Incorrect result");

            return new Vector2d((float) (vector.Height / length), (float) (-vector.Width / length));
        }

        public static Vector2d TopLeft(Rectangle2d rect)
        {
            return new Vector2d(rect.Left, rect.Top);
        }

        public static Vector2d TopRight(Rectangle2d rect)
        {
            return new Vector2d(rect.Right, rect.Top);
        }

        public static Vector2d BottomLeft(Rectangle2d rect)
        {
            return new Vector2d(rect.Left, rect.Bottom);
        }

        public static Vector2d BottomRight(Rectangle2d rect)
        {
            return new Vector2d(rect.Right, rect.Bottom);
        }
#if INTERNAL_PARSER
        /// <summary>
        /// Calculate the bounding rectangle given two points.
        /// </summary>
        /// <param name="ptStart">One corner of the rectangle.</param>
        /// <param name="ptStart">The opposite corner of the rectangle.</param>
        /// <returns></returns>
        public static Rectangle2d CalculateBound( Vector2d ptStart, Vector2d ptEnd )
        {
            return new Rectangle2d(Math.Min(ptStart.X, ptEnd.X), Math.Max(ptStart.X, ptEnd.X),
                                   Math.Min(ptStart.Y, ptEnd.Y), Math.Max(ptStart.Y, ptEnd.Y));
        }

        /// <summary>
        /// Return the unrotated vertical gap between the two rectangles, or zero
        /// if they overlap
        /// </summary>
        /// <param name="rect1">The first rectangle.</param>
        /// <param name="rect2">The second rectangle.</param>
        /// <returns>The vertical gap</returns>
        public static double VGap(Rectangle2d rect1, Rectangle2d rect2) 
        {
            double d = (rect1.Top < rect2.Top) ?
                (rect2.Top - rect1.Bottom) :
                (rect1.Top - rect2.Bottom);
            return Math.Max(d, 0);
        }
#endif

#if INTERNAL_PARSER
        /// <summary>
        /// This is 4 bit outcode of cohen-sutherland algorithm for line
        /// clipping.
        /// From the rightmost bit:
        /// bit 0: whether it is on the halfplane of the left of the rect
        /// bit 1: whether it is on the halfplane to the right of the rect
        /// bit 2: whether it is on the halfplane to the top of the rect
        /// bit 3: whether it is on the halfplane to the bottom of the rect
        /// </summary>
        /// <param name="point">Point which to calculate the outcode</param>
        /// <param name="rectangle">Rectangle for calculating the outcode</param>
        /// <returns>the outcode</returns>
        public static int Outcode(Vector2d point, Rectangle2d rectangle)
        {
            int outcode = 0;
            if (point.X < rectangle.Left)
            {
                outcode |= 0x1;
            }
            else if (point.X > rectangle.Right)
            {
                outcode |= 0x2;
            }

            if (point.Y < rectangle.Top)
            {
                outcode |= 0x4;
            }
            else if (point.Y > rectangle.Bottom)
            {
                outcode |= 0x8;
            }
            return outcode;
        }

#endif //INTERNAL_PARSER
        /// <summary>
        /// Test whether line segment defined by p0-p1 intersect a line segment defined
        /// by q0-q1
        /// </summary>
        /// <param name="p0">The start of the first line segment.</param>
        /// <param name="p1">The end of the first line segment.</param>
        /// <param name="q0">The start of the second line segment.</param>
        /// <param name="q1">The end of the second line segment.</param>
        /// <returns>True if the segments intersect, False if not.</returns>
        public static bool IsSegmentIntersecting(Vector2d p0, Vector2d p1, Vector2d q0, Vector2d q1)
        {
            double dp, dq;
            if (!ComputeIntersection(p0, p1, q0, q1, out dp, out dq))
            {
                return false;
            }
        
            return (0.0 <= dp && dp <= 1.0 && 0.0 <= dq && dq <= 1.0);
 
        }

        // Compute the intersection of two line segments (p0,p1) and (q0,q1).
        // This return true if both lines are NOT perpendicular, and
        // false otherwise.
        // If the line segments are not perpendicular, then the intersection
        // points are returned as parametrically as dp and dq where:
        //      p0 + dp (p1-p0) = r
        //  and q0 + dq (q1-q0) = r
        //  where r is the intersection points.
        //
        //  Thus if 0 <= dp <= 1 then the lines intersected at a point in line
        //  segment (p0,p1).
        //  If 0 <= dq <= 1 then the lines intersect at a point in segment (q0,q1)
        //  See
        //  http://geometryalgorithms.com/Archive/algorithm_0104/algorithm_0104B.htm for complete
        //  explanation.
        //
        public static bool ComputeIntersection(Vector2d p0, Vector2d p1, Vector2d q0, Vector2d q1,
            out double dp, out double dq)
        {
            Vector2d u = Subtract(p1, p0);
            Vector2d v = Subtract(q1, q0);
            Vector2d w = Subtract(p0, q0);
            dp = dq = 0.0;  // initialization to a known value.

            float denom = PerpDotProduct(u, v);

            if (Math.Abs(denom) < FloatEpsilon)
            {
                // Change to range instead.
                if ((PerpDotProduct(u, w) != 0.0f || PerpDotProduct(v, w) != 0.0f))
                {
                    // they are not colinear
                    return false;
                }

                // they are colinear segments, now determine if they are overlap
                double dp0, dp1; // end point of p0-p1 in eqn for q0-q1
                Vector2d w2 = Subtract(p1, q0);
                if (v.Width != 0.0)
                {
                    dp0 = w.Width / v.Width;
                    dp1 = w2.Height / v.Width;
                }
                else
                {
                    dp0 = w.Height / v.Height;
                    dp1 = w2.Height / v.Height;
                }
                
                // dp0 should be smaller than dp1
                if (dp0 > dp1)
                {
                    // swap
                    double dTemp = dp0;
                    dp0 = dp1;
                    dp1 = dTemp;
                }
                if (dp0 > 1.0 || dp1 < 0.0)
                {
                    return false;   // no overlap
                }

                // overlap points (many intersection)
                // try to return dp as the constant in eqn p0-p1 that starts the overlap
                // and dq as the constant in eqn q0-q1 that ends the overlap
                dp0 = Math.Max(0.0, dp0);  // clip to min 0
                dp1 = Math.Min(1.0, dp1);  // clip to max 1

                dp = dp0;
                
                Vector2d ptIntersect1 = new Vector2d((float) (p0.X + dp1 * u.Width),
                    (float)(p0.Y + dp1 * u.Height));

                if (v.Width != 0.0)
                {
                    dq = (ptIntersect1.X - q0.X) / v.Width;
                }
                else
                {
                    dq = (ptIntersect1.Y - q0.Y) / v.Height;
                }

                return true;
            }

            dp = PerpDotProduct(v, w) / denom;
            dq = PerpDotProduct(u, w) / denom;

            return true;
        }

        // This is the porting of v1.0's MapPoint() function
        // since what it does is actually rotate the points, thus
        // it is now renamed as RotatePoints.
        // The coordinates are updated in place.
        /// <summary>
        /// Rotate all the points, centered at given center with a given rotation angle.
        /// </summary>
        /// <param name="angle">Rotation angle.</param>
        /// <param name="center">Center of the rotation.</param>
        /// <param name="points">Points to rotate.</param>
        public static void RotatePoints(Angle angle, Vector2d center, Vector2d[] points)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                points[ i ] = RotatePoint(angle, center, points[i]);
            }
        }

        /// <summary>
        /// Rotate a point, centered at given center with a given rotation angle.
        /// </summary>
        /// <param name="angle">Rotation angle.</param>
        /// <param name="center">Center of the rotation.</param>
        /// <param name="point">The point to rotate.</param>
        public static Vector2d RotatePoint(Angle angle, Vector2d center, Vector2d point)
        {
            double xDiff = point.X - center.X;
            double yDiff = point.Y - center.Y;
            double x = center.X + xDiff * angle.Cos - yDiff * angle.Sin;
            double y = center.Y + xDiff * angle.Sin + yDiff * angle.Cos;
            return new Vector2d( (float) x, (float) y );
        }

        /// <summary>
        /// Calculate the bounding rectangle of given array of points.
        /// </summary>
        /// <param name="points">Points to calculate the bounding rectangle.</param>
        /// <returns>The bounding Rectangle2d.</returns>
        public static Rectangle2d CalculateBound(Vector2d[] points)
        {
            Debug.Assert(points.Length >= 2, "CalculateBound require at least 2 points");
            Vector2d minPoint = points[0];
            Vector2d maxPoint = minPoint;
            foreach(Vector2d p in points)
            {
                if (p.X < minPoint.X)
                {
                    minPoint.X = p.X;
                }
                else if (p.X > maxPoint.X)
                {
                    maxPoint.X = p.X;
                }

                if (p.Y < minPoint.Y)
                {
                    minPoint.Y = p.Y;
                }
                else if (p.Y > maxPoint.Y)
                {
                    maxPoint.Y = p.Y;
                }
            }

            return new Rectangle2d(minPoint.X, maxPoint.X, minPoint.Y, maxPoint.Y);

        }

#if INTERNAL_PARSER
        /// <summary>
        /// Return the unrotated horizontal gap between the two rectangles, or zero
        /// if they overlap
        /// </summary>
        /// <param name="rect1">The first rectangle.</param>
        /// <param name="rect2">The second rectangle.</param>
        /// <returns>The horizontal gap</returns>
        public static double HGap(Rectangle2d rect1, Rectangle2d rect2)
        {
            double d = (rect1.Left < rect2.Left) ?
                (rect2.Left - rect1.Right) :
                (rect1.Left - rect2.Right);
            return Math.Max(d, 0);
        }
#endif

        /// <summary>
        /// Return the minimum dTheta between the two given angles, all in radians.
        /// </summary>
        /// <param name="angle1">The first angle.</param>
        /// <param name="angle2">The second angle.</param>
        /// <returns>The minimum dTheta.</returns>
        public static double ThetaGap(double angle1, double angle2)
        {
            const double TwoPI = 2 * Math.PI;
            double diff = Math.Abs(angle1 - angle2) % TwoPI;
            diff = (diff <= Math.PI) ? diff : TwoPI - diff;
            // the angle of a line ranges in [0, pi)
            return (diff <= Math.PI/2) ? diff : Math.PI - diff;
        }

        /// <summary>
        /// return the ratio of Max / Min of the two numbers.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>The ratio</returns>
        /// <remarks>Potential problem when the value have different signs.</remarks>
        public static double Ratio(double value1, double value2)
        {
            return Math.Max(value1, value2) / Math.Min(value1, value2);
        }

        /// <summary>
        /// Calculate twice the area of an oriented triangle (a, b, c),
        /// area is positive if the triangle is oriented counterclockwise.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <param name="c">The third point.</param>
        /// <returns></returns>
        public static Real TriArea(
            Vector2d a, Vector2d b, Vector2d c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        /// <summary>
        ///		Returns TRUE if the point d is inside the circle defined by the
        ///		points a, b, c. See Guibas and Stolfi (1985) p.107.
        /// </summary>
        /// <param name="a">First point defining the circle.</param>
        /// <param name="b">First point defining the circle.</param>
        /// <param name="c">First point defining the circle.</param>
        /// <param name="d">Point to test.</param>
        /// <returns>True if d inside the circle.</returns>
        public static bool InCircle(
            Vector2d a,	
            Vector2d b,	
            Vector2d c,	
            Vector2d d
            )
        {
            return (a.X*a.X + a.Y*a.Y) * TriArea(b, c, d) -
                (b.X*b.X + b.Y*b.Y) * TriArea(a, c, d) +
                (c.X*c.X + c.Y*c.Y) * TriArea(a, b, d) -
                (d.X*d.X + d.Y*d.Y) * TriArea(a, b, c) > 0;
        }


        /// <summary>
        /// Check if tree points are in counter clockwise order.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <param name="c">The third point.</param>
        /// <returns>True if a, b, c in counter clockwise order.</returns>
        public static bool IsCounterClockwise(Vector2d a, Vector2d b, Vector2d c)
        {
            return (TriArea(a, b, c) > 0.0);
        }


        public static double DistanceSqd(Vector2d pt1, Vector2d pt2)
        {
            double x = pt2.X - pt1.X;
            double y = pt2.Y - pt1.Y;
            return (x * x + y * y);
        }

        public static double Distance(Vector2d pt1, Vector2d pt2)
        {
            return Math.Sqrt(DistanceSqd(pt1, pt2));
        }

        /// <summary>
        /// Caculates the minimal distance of two LineSegments.
        /// If the two LineSegments are intersected, then the distance is 0.
        /// </summary>
        /// <param name="line1">The first line.</param>
        /// <param name="line2">The second line.</param>
        /// <returns>The distance.</returns>
        public static double Distance(LineSegment line1, LineSegment line2)
        {
            double startPointDist1;
            double startPointLambda1;
            line2.GetSignedDistAndLambdaOfProjection(line1.StartPoint, out startPointDist1, out startPointLambda1);

            double stopPointDist1;
            double stopPointLambda1;
            line2.GetSignedDistAndLambdaOfProjection(line1.StopPoint, out stopPointDist1, out stopPointLambda1);

            double startPointDist2;
            double startPointLambda2;
            line1.GetSignedDistAndLambdaOfProjection(line2.StartPoint, out startPointDist2, out startPointLambda2);

            double stopPointDist2;
            double stopPointLambda2;
            line1.GetSignedDistAndLambdaOfProjection(line2.StopPoint, out stopPointDist2, out stopPointLambda2);

            bool isSameSign1 = (Math.Sign(startPointDist1) == Math.Sign(stopPointDist1));
            bool isSameSign2 = (Math.Sign(startPointDist2) == Math.Sign(stopPointDist2));

            if (!isSameSign1 && !isSameSign2)
            {
                // two line segments are like "+"
                return 0.0;
            }
            else
            {
                // two line segments are like "/ \" or "|-"
                Vector2d point1, point2;
                double distance1, distance2;
                double lambda1, lambda2;

                // candidate point of line1
                if(Math.Abs(startPointDist1) < Math.Abs(stopPointDist1))
                {
                    point1 = line1.StartPoint;
                    distance1 = startPointDist1;
                    lambda1 = startPointLambda1;
                }
                else
                {
                    point1 = line1.StopPoint;
                    distance1 = stopPointDist1;
                    lambda1 = stopPointLambda1;
                }

                // candidate point of line2
                if(Math.Abs(startPointDist2) < Math.Abs(stopPointDist2))
                {
                    point2 = line2.StartPoint;
                    distance2 = startPointDist2;
                    lambda2 = startPointLambda2;
                }
                else
                {
                    point2 = line2.StopPoint;
                    distance2 = stopPointDist2;
                    lambda2 = stopPointLambda2;
                }

                // if projection is on the line segment
                bool isValidLambda1 = (lambda1 > 0.0 && lambda1 < 1.0);
                bool isValidLambda2 = (lambda2 > 0.0 && lambda2 < 1.0);

                if(isValidLambda1 && isValidLambda2)
                {
                    // both are valid, return the minimal distance
                    return Math.Min(Math.Abs(distance1), Math.Abs(distance2));
                }
                else if(isValidLambda1 && !isValidLambda2)
                {
                    // point1 is valid, return its distance
                    return Math.Abs(distance1);
                }
                else if(!isValidLambda1 && isValidLambda2)
                {
                    // point2 is valid, return its distance
                    return Math.Abs(distance2);
                }
                else
                {
                    // calculate the distance of point1 from line segment2
                    if(lambda1 <= 0.0)
                    {
                        // lambda1 <= 0.0
                        distance1 = Common.Distance(point1, line2.StartPoint); 
                    }
                    else
                    {
                        // lambda1 >= 1.0
                        distance1 = Common.Distance(point1, line2.StopPoint); 
                    }

                    // calculate the distance of point2 from line segment1
                    if(lambda2 <= 0.0)
                    {
                        // lambda2 <= 0.0
                        distance2 = Common.Distance(point2, line1.StartPoint); 
                    }
                    else
                    {
                        // lambda2 >= 1.0
                        distance2 = Common.Distance(point2, line1.StopPoint); 
                    }

                    return Math.Min(distance1, distance2);
                }
            }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Caculates the minimal distance of two Polylines.
        /// If the two polylines are intersected, then the distance is 0.
        /// </summary>
        /// <param name="polyline1">The first polyline.</param>
        /// <param name="polyline2">The second polyline.</param>
        /// <returns>The distance.</returns>
        public static double Distance(Polyline polyline1, Polyline polyline2)
        {
            int count1 = polyline1.NumVertices;
            int count2 = polyline2.NumVertices;

            double minimalDistance = Double.MaxValue;
            for (int index1 = 1; index1 < count1; ++index1)
            {
                for (int index2 = 1; index2 < count2; ++index2)
                {
                    if (minimalDistance == 0)
                    {
                        break;
                    }

                    double distance = Distance(new LineSegment(polyline1[index1 - 1], polyline1[index1]),
                        new LineSegment(polyline2[index2 - 1], polyline2[index2]));

                    if (distance < minimalDistance)
                    {
                        minimalDistance = distance;
                    }
                }
            }

            return minimalDistance;
        }
#endif
        
        /// <summary>
        /// Calculate the shortest distance from a point to a line segment.
        /// This is either the nearer end point or the perpendicular distance.
        /// </summary>
        /// <param name="ptP">The point to calculate the distance.</param>
        /// <param name="ptA">Start of the line segment.</param>
        /// <param name="ptB">End of the line segment.</param>
        /// <returns>The distance squared.</returns>
        public static double DistanceSqdPointToLineSegment(
            Vector2d ptP,
            Vector2d ptA,
            Vector2d ptB)
        {
            double dab = DistanceSqd(ptA, ptB);     // length of line
            double dpa = DistanceSqd(ptP, ptA);    // from pt to startSegment

            if (dab == 0.0)
            {
                // Optimization : if the line is zero length, then just return distance
                // to the startSegment
                return dpa;
            }
    
            double dpb = DistanceSqd(ptP, ptB);

            if (dpb > (dpa + dab))
            {
                //By Pythagoras:
                // If it was a right-angle triangle (i.e. point P lies on the perpendicular
                // extending from A), then ( PB**2 == PA**2 + AB**2 )
                //
                //Hence, because PB**2 > (PA**2 + AB**2), it is a triangle with an obtuse angle,
                // and the perpendicular from P intersects the line AB outside the
                // the region between A and B, nearer to the end A.
                return dpa;
            }

            if (dpa > (dpb + dab))
            {
                //Similarly, by pythagoras, perpendicular is outside AB, nearer to B
                return dpb;
            }

            //Calculate the distance ON between the point N and the line AB.
            //
            //       N_
            //      /|  \_
            //    /  |     \_
            //  /____|________\
            // A     O         B
            //
            // By Pythagoras:
            // 1) AN**2 == ON**2 + OA**2
            // 2) BN**2 == ON**2 + OB**2
            //
            // 3) AB == OA + OB
            //
            // Squaring 3 gives:
            // 4)    AB**2 == OA**2 + 2*OA*OB + OB**2
            // Substituting OA from (1) and OB from (2) gives:
            //       AB**2 == (AN**2 - ON**2) + 2*OA*OB + (BN**2 - ON**2)
            // 5)    ON**2 - OA*OB == (AN**2 + BN**2 - AB**2) / 2
            //
            // (1) * (2) gives:
            //  AN**2 * BN**2 == ON**4 + (ON**2 * OB**2) + (ON**2 * OA**2) + (OA**2 * OB**2)
            //  AN**2 * BN**2 == ON**4 + ON**2 * ( OB**2 + OA**2 ) + (OA**2 * OB**2)
            // Substituting ( OB**2 + OA**2 ) using (4):
            //  AN**2 * BN**2 == ON**4 + (ON**2 * (AB**2 - 2*OA*OB)) + (OA**2 * OB**2)
            //  AN**2 * BN**2 == ON**4 + (ON**2 * AB**2) - (2*OA*OB * ON**2) + (OA**2 * OB**2)
            //  AN**2 * BN**2 == (ON**2 * AB**2) + (ON**2 - OA*OB)**2
            //  (ON**2 * AB**2) == (AN**2 * BN**2) - (ON**2 - OA*OB)**2
            // Substituting 5:
            //  (ON**2 * AB**2) == (AN**2 * BN**2) - ((AN**2 + BN**2 - AB**2) / 2)**2
            //
            // Resulting in:
            //  ON**2 == ( (AN**2 * BN**2) - ((AN**2 + BN**2 - AB**2) / 2)**2 ) / AB**2
            //
            // This expression mixes terms in AN and BN, which will result in a more
            // accurate floating-point calculation than a mathematically equivalent
            // expression which doesn't mix terms.

            double dTerm = (dpa + dpb - dab) * 0.5;
            double dValue = (dpa * dpb - dTerm * dTerm) / dab;

            Debug.Assert(dValue >= 0.0, "DistanceSqd should be positive or zero.");
            return dValue;
        }

#if INTERNAL_PARSER
        public static double FindMinimumIndex(double[][] matrix, out int minRow, out int minCol)
        {
            double minValue = matrix[0][0];
            minRow = 0;
            minCol = 0;
            int numRows = matrix.GetLength(0);
            int numCols = matrix.GetLength(1);

            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numCols; ++col)
                {
                    if (minValue > matrix[row][col])
                    {
                        minValue = matrix[row][col];
                        minRow = row;
                        minCol = col;
                    }
                }
            }

            return minValue;
        }

#endif //INTERNAL_PARSER
        public static double Divide(Vector2d size, Vector2d sizeRef)
        {
            Debug.Assert(!sizeRef.IsEmpty, "Division by zero");
            return DotProduct(size, sizeRef) / DotProduct(sizeRef, sizeRef);
        }

#if INTERNAL_PARSER
        public static Vector2d ProjectVector(Vector2d size, Vector2d sizeRef)
        {
            double div = Divide(size, sizeRef);
            return new Vector2d((float) (sizeRef.Width * div),
                (float) (sizeRef.Height * div));
        }

        public static Vector2d GetRemainVector(Vector2d size, Vector2d sizeRef)
        {
            return size - ProjectVector(size, sizeRef);
        }

        public static double LineVGap(Vector2d ptStart0, Vector2d ptEnd0, Vector2d ptStart1, Vector2d ptEnd1)
        {
            double dVGap = 0;
        
            if (ptStart0 == ptEnd0)
            {
                if (ptStart1 == ptEnd1)// The first line must be larger the the second one.
                {
                    dVGap = Distance(ptStart0, ptStart1);
                }
                else
                {
                    dVGap = Math.Min(Distance(ptStart0, ptStart1), Distance(ptStart0, ptEnd1));
                }
            }
            else
            {
                if (ptStart1 == ptEnd1)
                {
                    dVGap = Length(GetRemainVector(Subtract(ptStart1, ptStart0),
                        Subtract(ptEnd0, ptStart0)));
                }
                else
                {
                    if (IsSegmentIntersecting(ptStart0, ptEnd0, ptStart1, ptEnd1))
                    {
                        dVGap = 0;
                    }
                    else
                    {
                        double dTemp0 = Length(GetRemainVector(Subtract(ptStart1, ptStart0),
                            Subtract(ptEnd0, ptStart0)));
                        double dTemp1 = Length(GetRemainVector(Subtract(ptEnd1, ptStart0),
                            Subtract(ptEnd0, ptStart0)));
                        dVGap = Math.Min(dTemp0, dTemp1);
                    }
                }
            }
            return dVGap;
        }

#endif //INTERNAL_PARSER

        public static Vector2d GetLambdaPoint(Vector2d point1, Vector2d point2, double lambda)
        {
            Vector2d v = (Vector2d) Common.Subtract(point2, point1);
            return new Vector2d((float) (point1.X + v.Width * lambda), (float) (point1.Y + v.Height * lambda));
        }

        public static Vector2d MidPoint(Vector2d pt1, Vector2d pt2)
        {
            return GetLambdaPoint(pt1, pt2, 0.5);
        }

        public enum  LineIntersect
        {
            Normal = 0,
            None,
            Cover,
            Parallel
            // Intercross two lines (pt1, v1), (pt2, v2)
            //  return INTERSECTLINE_NORMAL if the two lines intercross normally
            //     the returned point will be (*pPt) == pt1 + (*pLambda1) * v1
            //                                       == pt2 + (*pLambda2) * v2
            //  return INTERSECTLINE_NONE if one of the two lines is just a point
            //     *pPt, *pLambda1, *pLambda2 are all undefined
            //  return INTERSECTLINE_COVER if two lines are covered each other
            //     pt2 + t * v2 == pt1 + ((*pLambda1) * t + (*pLambda2)) * v1
            //  return INTERSECTLINE_PARALL if two lines are parall lines
            //     the project point of (pt2 + t * v2) on line1 is
            //     (pt1 + ((*pLambda1) * t + (*pLambda2)) * v1)
        }

        public static LineIntersect IntersectLine(Vector2d pt1, Vector2d v1,
            Vector2d pt2, Vector2d v2, out double lambda1, out double lambda2,
            out Vector2d pointCross)
        {
            double lenSqr1 = DotProduct(v1, v1);
            double lenSqr2 = DotProduct(v2, v2);

            pointCross = new Vector2d(0, 0);

            if (lenSqr1 == 0 || lenSqr2 == 0)
            {
                lambda1 = lambda2 = 0;
                return LineIntersect.None;
            }

            Vector2d v = new Vector2d(pt2.X - pt1.X, pt2.Y - pt1.Y);
            double lenSqrSum = lenSqr1 + lenSqr2;
            double area = PerpDotProduct(v1, v2);

            const double intersectLineLambda1 = 1.0E-5;
            if (Math.Abs(area) <= lenSqrSum * intersectLineLambda1)
            {
                lambda1 = DotProduct(v2, v1) / lenSqr1;
                lambda2 = DotProduct(v, v1) / lenSqr1;

                double tmp = PerpDotProduct(v, v1);
                if (Math.Abs(tmp) <= lenSqr1 * intersectLineLambda1)
                {
                    return LineIntersect.Cover;
                }
                else
                {
                    return LineIntersect.Parallel;
                }
            }

            lambda1 = PerpDotProduct(v, v2) / area;
            lambda2 = PerpDotProduct(v, v1) / area;

            float x1 = pt1.X + (float) lambda1 * v1.Width;
            float y1 = pt1.Y + (float) lambda1 * v1.Height;
            float x2 = pt2.X + (float) lambda2 * v2.Width;
            float y2 = pt2.Y + (float) lambda2 * v2.Height;
            pointCross = MidPoint(new Vector2d(x1, y1), new Vector2d(x2, y2));

            return LineIntersect.Normal;
        }

        // Intercross two lines (line1 and line2), see above detailed information
        public static LineIntersect IntersectLine(LineSegment line1, LineSegment line2,
            out double lambda1, out double lambda2, out Vector2d pointCross)
        {
            return IntersectLine(line1.StartPoint, line1.Size,
                line2.StartPoint, line2.Size, out lambda1, out lambda2, out pointCross);
        }

        public static Vector2d AddPoint(Vector2d point1, Vector2d point2)
        {
            float x = point1.X + point2.X;
            float y = point1.Y + point2.Y;
            return new Vector2d(x, y);
        }

//#if INTERNAL_PARSER
        public static Vector2d SubtractPoint(Vector2d point1, Vector2d point2)
        {
            float x = point1.X - point2.X;
            float y = point1.Y - point2.Y;
            return new Vector2d(x, y);
        }
//#endif

        public static Vector2d PolarPoint(double length, double angle)
        {
            float x = (float) (length * Math.Cos(angle));
            float y = (float) (length * Math.Sin(angle));
            return new Vector2d(x, y);
        }

		// axis is a unit vector
		public static double[] ProjectPoints( Vector2d center, Vector2d axis, Vector2d[] points )
		{
			double[] proj = new double[ points.Length ];
			for (int i = 0; i < points.Length; ++i)
			{
				//FIXME: really a pain not having a vector class...
				proj[i] = Common.DotProduct( Common.Subtract( points[i], center ), axis );
			}
			return proj;
		}

        public static Vector2d SnapToGrid(Vector2d pt, Real gridSize)
        {
            return new Vector2d(
                (Real) (gridSize * Math.Round(pt.X/gridSize)), 
                (Real) (gridSize * Math.Round(pt.Y/gridSize)));
        }


// This implementation does not work properly yet (e.g., a left-bound offset on 401(k) Workshop_page4_labeled2.xml
//        public static RotatedRectangle ComputeRotatedBound(Vector2d[] points, Vector2d xAxis)
//        {
//            Debug.Assert(points.Length > 0, "Input points is an empty array");
//
//            Vector2d yAxis = new Vector2d(-xAxis.Y, xAxis.X);
//
//            // Find the projection ranges to the x and y axes.
//            float xMin = points[0].X;
//            float xMax = points[0].X;
//            float yMin = points[0].Y;
//            float yMax = points[0].Y;
//
//            for (int i = 1; i < points.Length; ++i)
//            {
//                // project to the xAxis and yAxis
//                float xProj = xAxis.Dot(points[i]);
//                float yProj = yAxis.Dot(points[i]);
//
//                // keep the min and max values
//                if (xProj < xMin)
//                {
//                    xMin = xProj;
//                }
//                if (xProj > xMax)
//                {
//                    xMax = xProj;
//                }
//                if (yProj < yMin)
//                {
//                    yMin = yProj;
//                }
//                if (yProj > yMax)
//                {
//                    yMax = yProj;
//                }
//            }
//
//            // (xMax, yMax) is position of the top-left corner in the coordinate defined by
//            // {xAxis, origin = (0,0)}.
//            // Its position in the Window's coordinate {xAxis = (1,0), origin = (0,0)} is the
//            // the point rotated by angle.
//            Vector2d topLeft = new Vector2d(xMin, yMin);
//            Angle angle = new Angle(xAxis);
//            topLeft = RotatePoint(angle, new Vector2d(0,0), topLeft);
//
//            // The upright bounding rectangle in the Window's coordinate
//            Rectangle2d uprightBound = new Rectangle2d(topLeft.X, topLeft.X + xMax - xMin, topLeft.Y, topLeft.Y + yMax - yMin);
//
//            return new RotatedRectangle(uprightBound, angle, topLeft);
//        }
	}
}
