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
    /// Struct representing an angle.
    /// </summary>
    public struct Angle
    {
        #region Fields
        private double _cos;
        private double _sin;
        // An invalid angle (both cosine and sine are zero).
        public static readonly Angle Null = new Angle();
        #endregion

        #region Properties
        /// <summary>
        /// The cosine value of the angle.
        /// </summary>
        public double Cos
        {
            get
            {
#if DEBUG
                // This is put under #if DEBUG because FXCop complain that IsValid is never be called in retail.
                // But it won't compile if IsValid only avaiable on DEBUG build
                Debug.Assert(this.IsValid, "Angle is not initialized");
#endif
                return this._cos;
            }
        }
        

        /// <summary>
        /// The sine value of the angle.
        /// </summary>
        public double Sin
        {
            get
            {
#if DEBUG
                // This is put under #if DEBUG because FXCop complain that IsValid is never be called in retail.
                // But it won't compile if IsValid only avaiable on DEBUG build
                Debug.Assert(this.IsValid, "Angle is not initialized");
#endif
                return this._sin;
            }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// The tangent value of the angle.
        /// </summary>
        public double Tan
        {
            get
            {
                if (this.Cos == 0.0)
                {
                    if (this.Sin > 0.0)
                    {
                        return Double.MaxValue;
                    }

                    return Double.MinValue;
                }

                return this.Sin / this.Cos;
            }
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Angle in radian (-Pi to Pi].
        /// </summary>
        public double Radian
        {
            get
            {
                return Math.Atan2(this.Sin, this.Cos);
            }
            set
            {
                this._cos = Math.Cos(value);
                this._sin = Math.Sin(value);
            }
        }
        #endregion

        #region Methods
        
        /// <summary>
        /// Creates an Angle struct from the specified radian angle.
        /// </summary>
        /// <param name="radian"></param>
        public Angle(double radian)
        {
            this._cos = Math.Cos(radian);
            this._sin = Math.Sin(radian);
        }

        /// <summary>
        /// Creates an Angle from the direction of the input vector.
        /// </summary>
        /// <param name="vector">The vector of the desired angle.</param>
        public Angle(Vector2d vector)
        {
            double dLength = Common.Length(vector);
            if (dLength > 0.0)
            {
                this._cos = vector.Width / dLength;
                this._sin = vector.Height / dLength;
            }
            else
            {
                this._sin = 0.0;
                this._cos = 0.0;
            }
        }

        /// <summary>
        /// Creates an Angle by copying.
        /// </summary>
        /// <param name="angle">The angle to copy.</param>
        public Angle(Angle angle)
        {
            this._cos = angle.Cos;
            this._sin = angle.Sin;
        }

        /// <summary>
        /// Whether this angle is valid, i.e., sqr(cos(angle)) + sqr(sin(angle) = 1.0.
        /// </summary>
        /// <returns>True if the angle is valid.</returns>
        public bool IsValid
        {
            get
            {
                return Utility.IsWithinEpsilon(this._cos * this._cos + this._sin * this._sin, 1.0);
            }
        }

        /// <summary>
        /// Returns the negative radian angle of the specified angle.
        /// </summary>
        /// <param name="angle">The input angle.</param>
        /// <returns>New angle representing the negative radian angle of the input.</returns>
        public static Angle operator-(Angle angle)
        {
            Angle result;
            result._cos = angle.Cos;
            result._sin = -angle.Sin;
            return result;
        }
       

        /// <summary>
        /// Returns an Angle struct that represents the sum of this angle and the input angle.
        /// </summary>
        /// <param name="rhs">The other angle.</param>
        /// <returns>New angle represent the sum of the two angles.</returns>
        private Angle Add(Angle rhs)
        {
#if DEBUG
            Debug.Assert(this.IsValid && rhs.IsValid, "Cannot add undefined angle");
#endif // DEBUG
            Angle result;
            result._cos = this._cos * rhs.Cos - this._sin * rhs.Sin;
            result._sin = this._sin * rhs.Cos + this._cos * rhs.Sin;
#if DEBUG
            Debug.Assert(result.IsValid, "Angle addition result in invalid result");
#endif
            return result;
        }

        /// <summary>
        /// Returns an Angle struct that represents the sum of the two input angles.
        /// </summary>
        /// <param name="lhs">The lefthand side angle</param>
        /// <param name="rhs">The righthand side angle</param>
        /// <returns>New angle representing the sum of the two angles</returns>
        public static Angle operator+(Angle lhs, Angle rhs)
        {
            return lhs.Add(rhs);
        }

        /// <summary>
        /// Subtracts the specified angle from this angle.
        /// </summary>
        /// <param name="rhs">The angle to substract.</param>
        /// <returns>New angle representing the result.</returns>
        private Angle Subtract(Angle rhs)
        {
#if DEBUG
            Debug.Assert(this.IsValid && rhs.IsValid, "Cannot add undefined angle");
#endif // DEBUG
            Angle result;
            result._cos = this._cos * rhs.Cos + this._sin * rhs.Sin;
            result._sin = this._sin * rhs.Cos - this._cos * rhs.Sin;
#if DEBUG
            Debug.Assert(result.IsValid, "Angle addition result in invalid result");
#endif
            return result;
        }

        /// <summary>
        /// Returns an Angle struct that represents the subtraction of the two input angles.
        /// </summary>
        /// <param name="lhs">The left-hand operand.</param>
        /// <param name="rhs">The right-hand operand.</param>
        /// <returns>New angle representing the subtraction result.</returns>
        public static Angle operator-(Angle lhs, Angle rhs)
        {
            return lhs.Subtract( rhs );
        }

        /// <summary>
        /// Operator equality.
        /// </summary>
        /// <param name="lhs">The left-hand operand.</param>
        /// <param name="rhs">The right-hand operand.</param>
        /// <returns>True if both angle are the same.</returns>
        /// <remarks>Cannot use property comparison because we may use this for comparison with Angle.Null</remarks>
        public static bool operator==(Angle lhs, Angle rhs )
        {   // We don't use lhs.Equal( rhs ) to avoid boxing/unboxing.
            return MathLibrary.Common.IsWithinEpsilon(lhs._cos, rhs._cos)
                && MathLibrary.Common.IsWithinEpsilon(lhs._sin, rhs._sin);
        }

        /// <summary>
        /// Operator inequality.
        /// </summary>
        /// <param name="lhs">Left hand side angle.</param>
        /// <param name="rhs">Right hand side angle.</param>
        /// <returns></returns>
        public static bool operator!=(Angle lhs, Angle rhs )
        {
            return !(lhs == rhs);
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Determines if the difference between the two input angles is less than the
        /// error bound RealEpsilon.
        /// </summary>
        /// <param name="u">One vector to compare.</param>
        /// <param name="v">The other vector to compare.</param>
        /// <returns>True if the the difference is less than the error bound.</returns>
        public static bool IsWithinEpsilon(Angle angle1, Angle angle2)
        {
            return Utility.IsWithinEpsilon(angle1.Radian, angle2.Radian);
        }
#endif // INTERNAL_PARSER


        /// <summary>
        /// Converts this angle to a unit vector {cos(angle), sin(angle)}.
        /// </summary>
        /// <returns>A new Vector2d struct representing the unit vector.</returns>
        public Vector2d ToUnitVector()
        {
            return new Vector2d( this.Cos, this.Sin );
        }

        /// <summary>
        /// Converts the angle from the range (-Pi, Pi] to (-Pi/2, Pi/2] so that
        /// the two lines of the old and new angles have the same orientation.
        /// </summary>
        /// <returns>A new angle in the range of (-Pi/2, Pi/2].</returns>
        public Angle ToHalfPi()
        {
            if ( this.Radian > Math.PI / 2.0 )
            {
                // Quadrant 2 -> 4
                return new Angle( this.Radian - Math.PI );
            }
            else if ( this.Radian <= -Math.PI / 2.0 )
            {
                // Quadrant 3 -> 1
                return new Angle( this.Radian + Math.PI );
            }
            else
            {
                return new Angle( this );
            }
        }
        
        /// <summary>
        /// Override object's Equal
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is Angle && this == (Angle) obj;
        }

        /// <summary>
        /// Override object's GetHashCode
        /// </summary>
        /// <returns>The hash code of cos and sin.</returns>
        public override int GetHashCode()
        {
            return this._cos.GetHashCode() + this._sin.GetHashCode();
        }

        #endregion

    }
}
