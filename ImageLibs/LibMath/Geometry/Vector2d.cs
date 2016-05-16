//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // For Debug class.
using System.Xml.Serialization;

namespace System.Windows.Ink.Analysis.MathLibrary
{
    using Real = System.Single;
    
    /// <summary>
	/// Vector2d is a structure representing both points and vector in 2d space.
	/// This class is intended to replace PointF and SizeF so MathLibrary does 
	/// not depend on System.Drawing anymore.
	/// </summary>
	[Serializable]
    public struct Vector2d
    {

        #region Fields
        private Real _x;
        private Real _y;
        #endregion

        #region Properties
        /// <summary>
        /// Value in the X axis.
        /// </summary>
        public Real X
        {
            get { return this._x; }
            set { this._x = value; }
        }

        /// <summary>
        /// Value in the Y axis.
        /// </summary>
        public Real Y
        {
            get { return this._y; }
            set { this._y = value; }
        }

       
        /// <summary>
        /// The length of this vector.
        /// </summary>
        [XmlIgnore]
        public Real Norm 
        {
            get { return (Real) Math.Sqrt( _x * _x + _y * _y); }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// True if both x and y values are zero.
        /// </summary>
        /// <remarks>To be revisited: incorrect when Real = System.Double.</remarks>
        public bool IsNull
        {
            get { return this._x == 0.0f && this._y == 0.0f; }
        }
#endif // INTERNAL_PARSER

        // This will be obsolete soon.
        // [Obsolete]
        /// <summary>
        /// Width (to support compatibility with SizeF)
        /// </summary>
        [XmlIgnore]
        public Real Width
        {
            get { return this._x; }
            set { this._x = value; }
        }

        // This will be obsolete soon.
        // [Obsolete]
        /// <summary>
        /// Height (to support compatibility with SizeF)
        /// </summary>
        [XmlIgnore]
        public Real Height
        {
            get { return this._y; }
            set { this._y = value; }
        }


        // This will be obsolete soon.
        // [Obsolete]
        /// <summary>
        /// True if both X and Y are zero.
        /// This to provide compatibility with SizeF.  
        /// </summary>
        public bool IsEmpty
        {
            get { return this._x == 0.0f && this._y == 0.0f; }
        }
        #endregion // Properties

		#region Object overrides
		public override bool Equals(object obj)
		{
			return obj is Vector2d && this == (Vector2d) obj;
		}

        /// <summary>
        /// Generate a hash code.  Hashcode is generated based on
        /// the last 16 bits of X and Y.
        /// </summary>
        /// <returns>The hashcode for</returns>
		public override int GetHashCode()
		{
			int x = (int) this.X;
			int y = (int) this.Y;
			return ((x & 0x0000FFFF) << 16) | (y & 0x0000FFFF);
		}
		#endregion // Object overrides

        #region Constructors
        /// <summary>
        /// Constructor from Real inputs.
        /// </summary>
        /// <param name="x">X value</param>
        /// <param name="y">Y value</param>
        public Vector2d(Real x, Real y)	
        { 
            this._x = x; 
            this._y = y; 
        }

        /// <summary>
        /// Constructor from double inputs.
        /// </summary>
        /// <remarks>To be revisited. Problematic when Real = System.Double.</remarks>
        public Vector2d(double x, double y)	: this((Real)x, (Real)y)
        { 
        }

        /// <summary>
        /// Constructor in the polar coordinate
        /// </summary>
        /// <param name="angle">The orientation angle of the vector</param>
        /// <param name="length">The length of the vector</param>
        public Vector2d( Angle angle, Real length ) :
            this( angle.Cos * length, angle.Sin * length )
        {
        }
        #endregion // Constructors

        #region Methods and operators

//#if INTERNAL_PARSER
        /// <summary>
        /// Make this vector to have 1 unit length.
        /// </summary>
        public void Normalize()
        {
            Real len = this.Norm;
            this._x /= len;
            this._y /= len;
        }
//#endif // INTERNAL_PARSER

        /// <summary>
        /// Operator equality
        /// </summary>
        public static bool operator==(Vector2d u, Vector2d v)
        {
            return MathLibrary.Common.IsWithinEpsilon(u.X, v.X)
                && MathLibrary.Common.IsWithinEpsilon(u.Y, v.Y);
        }

        /// <summary>
        /// Operator inequality
        /// </summary>
        public static bool operator!=(Vector2d u, Vector2d v)
        {
            return !( u == v );
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Determines if the two input vectors are practically identical, i.e.,
        /// the maximum of their x and y differences is less than the error bound RealEpsilon.
        /// </summary>
        /// <param name="u">One vector to compare.</param>
        /// <param name="v">The other vector to compare.</param>
        /// <returns>True if the two input vectors are practically identical.</returns>
        public static bool IsWithinEpsilon(Vector2d u, Vector2d v)
        {
            return Utility.IsWithinEpsilon(u.X, v.X) && Utility.IsWithinEpsilon( u.Y, v.Y );
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Vector addition.
        /// </summary>
        /// <param name="u">First vector.</param>
        /// <param name="v">Second vector.</param>
        /// <returns>The sum of the two vectors.</returns>
        public static Vector2d operator+(Vector2d u, Vector2d v)
        {
            return new Vector2d(u.X + v.X, u.Y + v.Y);
        }

        /// <summary>
        /// Vector subtraction.
        /// </summary>
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
        /// <returns>The difference (first - second) vector.</returns>
        public static Vector2d operator-(Vector2d u, Vector2d v)
        {
            return new Vector2d(u.X - v.X, u.Y - v.Y);
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Unary minus.
        /// </summary>
        /// <param name="u">Input vector.</param>
        /// <returns>The reverse of the input vector.</returns>
        public static Vector2d operator-(Vector2d u)
        {
            return new Vector2d(-u.X, -u.Y);
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Multiply a vector with a real number.
        /// </summary>
        /// <param name="r">Scaling factor</param>
        /// <returns>The newly scaled vector.</returns>
        private Vector2d Multiply(Real r)
        {
            return new Vector2d(r * this.X, r * this.Y);
        }


        /// <summary>
        /// Multiply/scale a vector by a scale.
        /// </summary>
        /// <param name="u">Vector to multiply.</param>
        /// <param name="r">Scale or multiplication factor.</param>
        /// <returns>The newly scaled vector.</returns>
        public static Vector2d operator*(Vector2d u, Real r)
        {
            return u.Multiply(r);
        }

        /// <summary>
        /// Multiply/scale a vector by a scale.
        /// </summary>
        /// <param name="u">Vector to multiply.</param>
        /// <param name="r">Scale or multiplication factor.</param>
        /// <returns>The newly scaled vector.</returns>
        public static Vector2d operator*(Real r, Vector2d u)
        {
            return u.Multiply(r);
        }

        /// <summary>
        /// Dot product.
        /// </summary>
        /// <param name="v">The other vector</param>
        /// <returns>This dot product between this vector to the other vector.</returns>
        public Real Dot(Vector2d v)
        {
            return this.X * v.X + this.Y * v.Y;
        }

        /// <summary>
        /// Dot product as multiplication operator.
        /// </summary>
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
        /// <returns>The dot product of the two vectors.</returns>
        public static Real operator*(Vector2d u, Vector2d v)
        {
            return u.Dot(v);
        }

        /// <summary>
        /// Dot product of the perpendicular vector and the input vector v.
        /// </summary>
        /// <param name="v">The other vector.</param>
        /// <remarks>The perpendicular vector of (x, y) is (-y, x).</remarks>
        public Real PerpDot(Vector2d v)
        {
            return this._x * v.Y - this._y * v.X;
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Divide the projected vector to v by |v|, the length of v.
        /// </summary>
        /// <param name="v">The reference vector</param>
        /// <returns>A value r such that r * |v| = w, where w is the projection of this vector to v.</returns>
        public Real ProjectionDivide(Vector2d v)
        {
            return (this * v) / (v * v);
        }

        /// <summary>
        /// Project this vector to a reference vector.
        /// </summary>
        /// <param name="v">The reference vector.</param>
        /// <returns>The projected vector.</returns>
        public Vector2d ProjectTo(Vector2d v)
        {
            Real projectionScale = this.ProjectionDivide(v);
            return (projectionScale * v);
        }

        /// <summary>
        /// Fractional vector from this point to the destination point.
        /// </summary>
        /// <param name="v">The destination point.</param>
        /// <param name="lambda">Fractional value.</param>
        /// <returns>The fractional vector to the destination point.</returns>
        public Vector2d LambdaVectorTo(Vector2d destPoint, Real lambda)
        {
            return this + lambda * (destPoint - this);
        }

        /// <summary>
        /// Fractional vector from this point to the destination point.
        /// </summary>
        /// <param name="v">The destination point.</param>
        /// <param name="lambda">Fractional value.</param>
        /// <returns>The fractional vector to the destination point.</returns>
        /// <remarks>To be revisited. Problematic when Real = System.Double.</remarks>
        public Vector2d LambdaVectorTo(Vector2d destPoint, double lambda)
        {
            return this + ((Real) lambda) * (destPoint - this);
        }

        /// <summary>
        /// Fractional vector from this u vector to the destination vector.
        /// </summary>
        /// <param name="u">The starting vector.</param>
        /// <param name="v">The destination vector.</param>
        /// <param name="lambda">Fractional value.</param>
        /// <returns>The fractional vector to the destination.</returns>
        public static Vector2d GetLambdaVector(Vector2d u, Vector2d v, Real lambda)
        {
            return u.LambdaVectorTo(v, lambda);
        }

        /// <summary>
        /// Fractional vector from this u vector to the destination vector.
        /// </summary>
        /// <param name="u">The starting vector.</param>
        /// <param name="v">The destination vector.</param>
        /// <param name="lambda">Fractional value.</param>
        /// <returns>The fractional vector to the destination.</returns>
        /// <remarks>To be revisited. Problematic when Real = System.Double.</remarks>
        public static Vector2d GetLambdaVector(Vector2d u, Vector2d v, double lambda)
        {
            return u.LambdaVectorTo(v, lambda);
        }

        public double DistanceSquared( Vector2d other )
        {
            double dx = this.X - other.X;
            double dy = this.Y - other.Y;
            return dx * dx + dy * dy;
        }

        public double Distance( Vector2d other )
        {
            return Math.Sqrt( this.DistanceSquared( other ) );
        }
#endif // INTERNAL_PARSER

        #endregion // Methods and operators
        
    }
}
