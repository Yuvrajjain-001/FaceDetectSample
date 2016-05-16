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
    /// <summary>
    /// Struct representing an interval of double between [min, max], min <= max.
    /// </summary>
    internal struct Interval : IComparable
    {
        #region Fields
        private double _min;
        private double _max;
        #endregion // Fields

        #region Static Fields
        // Default Null (uninitialized) interval.
        public static readonly Interval Null = new Interval(Double.MinValue, Double.MaxValue);
        #endregion // Static Fields

        #region Properties
        /// <summary>
        /// The minimal value of the interval.
        /// </summary>
        public double Min
        {
            get { return this._min; }

#if INTERNAL_PARSER
            set
            {
                Debug.Assert( value <= this.Max, "Interval: Min must <= Max" );

                this._min = value;
            }
#endif
        }

        /// <summary>
        /// The maximal value of the interval.
        /// </summary>
        public double Max
        {
            get { return this._max; }
            set
            {
                Debug.Assert( value >= this.Min, "Interval: Max must >= Min" );

                this._max = value;
            }
        }
        
        /// <summary>
        /// The length of the interval.
        /// </summary>
        public double Length
        {
            get
            {
                if ( this.Min == Double.MinValue || this.Max == Double.MaxValue )
                {
                    return Double.MaxValue;
                }
                return this.Max - this.Min;
            }
        }

        /// <summary>
        /// Whether the interval is empty (min == max).
        /// </summary>
        public bool IsEmpty
        {
            get { return MathLibrary.Common.IsWithinEpsilon(this.Min, this.Max); }
        }

        #endregion // Properties

        #region Methods
        /// <summary>
        /// Creates an Interval structure with the specified min and max ranges.
        /// </summary>
        public Interval(double min, double max)
        {
            // if ( min > max )
            // {
            //     Utility.SwapDouble( ref min, ref max );
            // }
            Debug.Assert( max >= min, "Interval: max must >= min" );
            this._min = min;
            this._max = max;
        }

        /// <summary>
        /// Object override.
        /// </summary>
        public override int GetHashCode()
        {
            return (int) (this.Min + this.Max);
        }
        
        /// <summary>
        /// Object override.
        /// </summary>
        public override bool Equals(Object other)
        {
            return other is Interval && this == (Interval) other;
        }

        /// <summary>
        /// Operator equality.
        /// </summary>
        public static bool operator==(Interval interval1, Interval interval2)
        {
            return MathLibrary.Common.IsWithinEpsilon(interval1.Max, interval2.Max)
                && MathLibrary.Common.IsWithinEpsilon(interval1.Min, interval2.Min);
        }
        
        /// <summary>
        /// Operator inequality.
        /// </summary>
        public static bool operator!=(Interval interval1, Interval interval2)
        {
            return !(interval1 == interval2);
        }
        
#if INTERNAL_PARSER
        /// <summary>
        /// Determines whether this interval contains the input value x, i.e.,
        /// x falls within the interval.
        /// </summary>
        public bool Contains( double x )
        {
            return (this.Min <= x) && (x <= this.Max);
        }

        /// <summary>
        /// Determines whether this interval contains the input interval.
        /// An empty interval is included by any other interval.
        /// </summary>
        public bool Contains( Interval other )
        {
            if (other.IsEmpty)
            {
                return true;
            }
            return (this.Min <= other.Min) && (other.Max <= this.Max);
        }
#endif // INTERNAL_PARSER

#if INTERNAL_PARSER
        /// <summary>
        /// Compute the union of this interval and the input interval.
        /// </summary>
        /// <returns>A new interval just includes both intervals.</returns>
        public static Interval Union( Interval interval1, Interval interval2 )
        {
            return new Interval( Math.Min( interval1.Min, interval2.Min ),
                                 Math.Max( interval1.Max, interval2.Max ) );
        }
#endif

        /// <summary>
        /// Computes the intersection of this interval and the input interval.
        /// </summary>
        /// <returns>A new interval of the intersection.</returns>
        public static Interval Intersect( Interval interval1, Interval interval2 )
        {
            double min = Math.Max(interval1.Min, interval2.Min);
            double max = Math.Min(interval1.Max, interval2.Max);

            if (min <= max)
            {
                return new Interval(min, max);
            }
            else
            {
                return new Interval(min, min);  // Empty interval
            }
        }

        /// <summary>
        /// Determines whether this interval intersects with the input interval.
        /// </summary>
        public bool IntersectsWith(Interval other)
        {
            return (this.Min <= other.Max) && (this.Max >= other.Min);
        }

        /// <summary>
        /// Offsets this interval. Change both min and max by the given amount.
        /// </summary>
        /// <param name="offset">The amount to offset by.</param>
        public void Offset(double offsetAmount)
        {
            if (this._min != Double.MinValue)
            {
                this._min += offsetAmount;
            }
            if (this._max != Double.MaxValue)
            {
                this._max += offsetAmount;
            }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Get the value located at the position specified by lambda.
        /// lambda == 0: this.Min.
        /// lambda == 1: this.Max.
        /// </summary>
        public double GetLambdaValue(double lambda)
        {
            Debug.Assert( this.Min != Double.MinValue && this.Max != Double.MaxValue,
                "Cannot call GetLambdaValue for an open interval." );
            Debug.Assert( lambda >= 0.0 && lambda <= 1.0,
                "lambda must be in the closed range [0, 1]" );

            return this.Min + (this.Max - this.Min) * lambda;
        }

        /// <summary>
        /// Get the lambda according to the value x.
        /// x == this.Min: 0.
        /// x == this.Max: 1.
        /// </summary>
        public double GetLambdaOfValue(double x)
        {
            double length = this.Length;

            Debug.Assert( length < Double.MaxValue,
                "Cannot call GetLambdaOfValue for an open interval" );

            Debug.Assert( this.Contains(x), "x must falls within the interval" );

            return (x - this.Min) / length;
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Compute the distance between this interval and another interval.
        /// If they intersect, then the distance is 0.
        /// </summary>
        /// <param name="other">The interval to calculate distance with</param>
        /// <returns>The distance.</returns>
        public double Distance(Interval other)
        {
            if (this.IntersectsWith( other ))
            {
                return 0;
            }

            if (this.Min < other.Min)
            {
                return other.Min - this.Max;
            }

            return this.Min - other.Max;
        }

        #endregion // Methods

        #region IComparable Members

        /// <summary>
        /// Compares to another object.
        /// </summary>
        /// <param name="obj">Another object. It must be an Interval.</param>
        public int CompareTo(object obj)
        {
            Debug.Assert(obj is Interval, "Cannot compare to other class other than interval");
            return CompareTo((Interval) obj);
        }

        /// <summary>
        /// Compares to another interval.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>-1: this.Min < other.Min, or this.Min = other.Min and this.Max < other.Max.
        ///           0: two intervals are identical.
        ///           1: this.Min > other.Min, or this.Min = other.Min and this.Max > other.Max.
        /// </returns>
        public int CompareTo(Interval other)
        {
            if (MathLibrary.Common.IsWithinEpsilon(this.Min, other.Min))
            {
                if (this.Max < other.Max)
                {
                    return -1;
                }
                else if (this.Max > other.Max)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (this.Min < other.Min)
                {
                    return -1;
                }
                else if (this.Min > other.Min)
                {
                    return 1;
                }
                return 0;
            }
        }

        #endregion
    }
}
