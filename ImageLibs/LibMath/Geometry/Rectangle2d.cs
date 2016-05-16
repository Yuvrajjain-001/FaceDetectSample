//------------------------------------------------------------------------------
// <copyright from='2002' to='2003' company='Microsoft Corporation'>
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
    // This is to make it flexible in case "double" precision
    // is required in the future.
    using Real = System.Single;
    
    // There are two purposes for this struct:
    // 1. Representing a 2D bound in a specified coordinate, e.g., the bound of an ink word
    //    in the coordinate defined by the writing direction. So far most usages in the
    //    shipping code are for this purpose.
    // 2. Replacing the use of RectangleF, thus MathLibrary does not depend on System.Drawing
    //    any more. This is for representing an upright rectangle in the Windows coordinate,
    //    e.g., a non-ink context node region, a selection region etc.
    // The struct design is more optimized for 1, and also tries to provide parity for 2.
    // 
    /// <summary>
    /// Struct representing a rectangular region. Defined in the windows coordinate system
    /// where the topleft corner is (0,0), the x axis goes from left to right and the y
    /// axis goes from top to bottom.
	/// </summary>
	[Serializable]
    public struct Rectangle2d : IComparable
	{

        #region Fields
        private Real _left; // left <= right
        private Real _right;
        private Real _top; // top <= bottom
        private Real _bottom;
        #endregion

        /// <summary>
        /// Uninitialized empty rectangle.
        /// </summary>
        public static readonly Rectangle2d Empty = new Rectangle2d();

        #region Properties
        /// <summary>
        /// The X coordinate of the left side.
        /// </summary>
        [XmlIgnore]
        public Real X { get { return Left; } set { Left = value; } }

        /// <summary>
        /// The Y coordinate of the top side.
        /// </summary>
        [XmlIgnore]
        public Real Y { get { return Top; } set { Top = value; } }

        /// <summary>
        /// The X coordinate of the right side.
        /// </summary>
        [XmlAttribute]
        public Real Right
        {
            get {  return this._right; }
            set
            { 
                Debug.Assert( this.IsEmpty || value >= this._left, "right cannot be smaller than left" );
                this._right = value;
            }
        }

        /// <summary>
        /// The X coordinate of the left side.
        /// </summary>
        [XmlAttribute]
        public Real Left
        {
            get {  return this._left; }
            set
            { 
                Debug.Assert( this.IsEmpty || value <= this._right, "left cannot be larger than right" );
                this._left = value;
            }
        }
        /// <summary>
        /// The Y coordinate of the bottom side.
        /// </summary>
        [XmlAttribute]
        public Real Bottom
        {
            get {  return this._bottom; }
            set
            { 
                Debug.Assert( this.IsEmpty || value >= this._top, "bottom cannot be smaller than top" );
                this._bottom = value;
            }
        }
        
        /// <summary>
        /// The Y coordinate of the top side.
        /// </summary>
        [XmlAttribute]
        public Real Top
        {
            get {  return this._top; }
            set
            { 
                Debug.Assert( this.IsEmpty || value <= this._bottom, "top cannot be larger than bottom" );
                this._top = value;
            }
        }
        
        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public Real Width
        {
            get { return this._right - this._left; }
        }

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
        public Real Height
        {
            get { return this._bottom - this._top; }
        }

        /// <summary>
        /// Top-left corner of the rectangle.
        /// </summary>
        /// <remarks>For RectangleF parity.</remarks>
        public Vector2d Location
        {
            get { return this.TopLeft; }
        }

        /// <summary>
        /// The size of the rectangle.
        /// </summary>
        public Vector2d Size
        {
            get { return new Vector2d( this.Width, this.Height ); }
        }

        public Vector2d Center
        {
            get
            {
                return new Vector2d( (this._right + this._left)/(Real)2.0,
                                     (this._bottom + this._top)/(Real)2.0 );
            }
        }
        /// <summary>
        /// Top-left corner of the rectangle.
        /// </summary>
        public Vector2d TopLeft
        {
            get { return new Vector2d( this._left, this._top ); }
        }

        /// <summary>
        /// Bottom-left corner of the rectangle.
        /// </summary>
        public Vector2d BottomLeft
        {
            get { return new Vector2d( this._left, this._bottom ); }
        }

        /// <summary>
        /// Bottom-right corner of the rectangle.
        /// </summary>
        public Vector2d BottomRight
        {
            get { return new Vector2d( this._right, this._bottom ); }
        }

        /// <summary>
        /// Top-right corner of the rectangle.
        /// </summary>
        public Vector2d TopRight
        {
            get { return new Vector2d( this._right, this._top ); }
        }
     

        /// <summary>
        /// Area of this rectangle.
        /// </summary>
        public Real Area
        {
            get { return Width * Height; }
        }

        /// <summary>
        /// Whether this rectangle is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return false
                    || this.Width == 0
                    || this.Height == 0
                    ;
            }
        }

        #endregion Properties

		#region Object overrides
		public override bool Equals(object obj)
		{
			return obj is Rectangle2d && this == (Rectangle2d) obj;
		}

        /// <summary>
        /// Determines whether two rectangles are equal.
        /// </summary>
        /// <param name="rect1">The left-hand operand.</param>
        /// <param name="rect2">The right-hand operand.</param>
        /// <returns>True if the two rectangles are equal.</returns>
        public static bool operator==(Rectangle2d rect1, Rectangle2d rect2)
		{
            return rect1._left == rect2._left &&
                   rect1._right == rect2._right &&
                   rect1._top == rect2._top &&
                   rect1._bottom == rect2._bottom;
		}

        /// <summary>
        /// Determines whether two rectangles are not equal.
        /// </summary>
        /// <param name="rect1">The left-hand operand.</param>
        /// <param name="rect2">The right-hand operand.</param>
        /// <returns>True if the two rectangles are not equal.</returns>
        public static bool operator!=(Rectangle2d rect1, Rectangle2d rect2)
		{
			return !( rect1 == rect2 );
		}

        /// <summary>
        /// Determines if the two input rectangles are practically identical, i.e.,
        /// their maximum offset is less than the error bound RealEpsilon.
        /// </summary>
        /// <param name="rect1">One rectangle to compare.</param>
        /// <param name="rect2">The other rectangle to compare.</param>
        /// <returns>True if the two input rectangles are practically identical.</returns>
        public static bool IsWithinEpsilon(Rectangle2d rect1, Rectangle2d rect2)
        {
            return Utility.IsWithinEpsilon(rect1._left, rect2._left) &&
                Utility.IsWithinEpsilon(rect1._right, rect2._right) &&
                Utility.IsWithinEpsilon(rect1._top, rect2._top) &&
                Utility.IsWithinEpsilon(rect1._bottom, rect2._bottom);
        }

        /// <summary>
        /// Generate a has code.  Hashcode is generated based on
        /// the last 16 bits of X and Y.
        /// </summary>
        /// <returns>The hashcode for</returns>
		public override int GetHashCode()
		{
            const int mask = 0x0000000F;
            return (mask & this.Left.GetHashCode()) << 24 |
                   (mask & this.Right.GetHashCode()) << 16 |
                   (mask & this.Top.GetHashCode()) << 8 |
                   (mask & this.Bottom.GetHashCode());
		}

//#if INTERNAL_DPU
		public override string ToString()
		{
			return String.Format("< Rect {0},{1}:{2}x{3}>", Left, Top, Width, Height);
		}
//#endif

		#endregion Object overrides



        /// <summary>
        /// Creates a Rectangle2d with the four specified sides.
        /// </summary>
        /// <param name="left">The X coordinate of the left side</param>
        /// <param name="right">The X coordinate of the right side</param>
        /// <param name="top">The Y coordinate of the top side</param>
        /// <param name="bottom">The Y coordinate of the bottom side</param>
        public Rectangle2d( Real left, Real right, Real top, Real bottom )
        {
            Debug.Assert( left <= right, "left cannot be larger than right" );
            Debug.Assert( top <= bottom, "top cannot be larger than bottom" );

            this._left = left;
            this._right = right;
            this._top = top;
            this._bottom = bottom;
        }

        /// <summary>
        /// Creates a Rectangle2d with the specified top-left corner location and size.
        /// </summary>
        /// <param name="location">The top-left corner location</param>
        /// <param name="size">The size of the rectangle.</param>
        public Rectangle2d(Vector2d topLeft, Vector2d bottomRight) :
            this( topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y )
        { 
        }

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="rect">Other rectangle to copy.</param>
        public Rectangle2d(Rectangle2d other) :
            this( other.Left, other.Right, other.Top, other.Bottom )
        {
        }

        public static Rectangle2d FromXYWH(Real x, Real y, Real width, Real height)
        {
            return new Rectangle2d(x, x+width, y, y+height);
        }

        public static Rectangle2d FromPosSize(Vector2d pos, Vector2d size)
        {
            return new Rectangle2d(pos, pos+size);
        }

        public static Rectangle2d FromCoords(Real x1, Real y1, Real x2, Real y2)
        {
            return new Rectangle2d(x1, x2, y1, y2);
        }

        /// <summary>
        /// Determines whether this rectangle contains the point (x,y).
        /// </summary>
        /// <param name="x">Value of X coordinate of the point.</param>
        /// <param name="y">Value of Y coordinate of the point.</param>
        /// <returns>True if the point (x,y) is contained in this rectangle.</returns>
        /// ISSUE-2003/05/28-HerryS -- Should we consider the point in the 
        /// right/bottom border to be inside the rectangle?
        public bool Contains(Real x, Real y)
        {
            return ( this.Left <= x && x < this.Right &&
                     this.Top <= y && y < this.Bottom );

        }

        /// <summary>
        /// Determines whether the specified point is contained by this rectangle.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is contained inside the rectangle.</returns>
        public bool Contains(Vector2d point)
        {
            return Contains(point.X, point.Y);
        }


        /// <summary>
        /// Determines whether a rectangle is contained inside this rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test.</param>
        /// <returns>True if the rect is contained inside this rectangle.</returns>
        public bool Contains(Rectangle2d other)
        {
            return Contains(other.TopLeft) && Contains(other.BottomRight);
        }

        /// <summary>
        /// Inflates this rectangle by the specified x and y amounts.
        /// </summary>
        /// <param name="x">The inflation in horizontal direction.</param>
        /// <param name="y">The inflation in vertical direction.</param>
        /// <remarks>This rectangle is inflated or deflated.</remarks>
        public void Inflate(Real x, Real y)
        {
            this._left -= x;
            this._right +=  x;
            this._top -= y;
            this._bottom += y;
        }

        /// <summary>
        /// Inflates this rectangle by the specified size.
        /// </summary>
        /// <param name="size">The inflation size.</param>
        /// <remarks>This rectangle is inflated or deflated.</remarks>
        public void Inflate(Vector2d size)
        {
            this.Inflate(size.X, size.Y);
        }

        /// <summary>
        /// Returns a new rectangle by inflating rect by the given x,y size.
        /// </summary>
        /// <param name="rect">The original rectangle.</param>
        /// <param name="x">Inflation size in the horizontal direction.</param>
        /// <param name="y">Inflation size in the vertical direction.</param>
        /// <returns>A new inflated rectangle.</returns>
        public static Rectangle2d Inflate(Rectangle2d rect, Real x, Real y)
        {
            Rectangle2d inflatedRect = new Rectangle2d(rect);
            inflatedRect.Inflate(x, y);
            return inflatedRect;
        }

        /// <summary>
        /// Offsets this rectangle by x and y.
        /// </summary>
        /// <param name="x">The offset in horizontal direction.</param>
        /// <param name="y">The offset in vertical direction.</param>
        /// <remarks>This rectangle is relocated.</remarks>
        public void Offset(Real x, Real y)
        {
            this._left += x;
            this._right += x;
            this._top += y;
            this._bottom += y;
        }

        /// <summary>
        /// Offsets this rectangle by the specified size.
        /// </summary>
        /// <param name="size">The offset size.</param>
        /// <remarks>This rectangle is relocated.</remarks>
        public void Offset(Vector2d size)
        {
            this.Offset(size.X, size.Y);
        }

        /// <summary>
        /// Determines whether this rectangle intersects with the given rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test.</param>
        /// <returns>True if the rectangle intersects with this rectangle.</returns>
        public bool IntersectsWith(Rectangle2d rect)
        {
            bool intersects = false;
            if (!this.IsEmpty && !rect.IsEmpty)
            {
                intersects = OverlapsInX(rect) && OverlapsInY(rect);
            }
            return intersects;
        }

        /// <summary>
        /// Determines whether this rectangle has horizontal overlap with the input rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test.</param>
        /// <returns>True if this rectangle has horizontal overlap with the input rectangle.</returns>
        private bool OverlapsInX(Rectangle2d other)
        {
            return (this.Left <= other.Left && other.Left < this.Right ) ||
                   (other.Left <= this.Left && this.Left < other.Right);
        }

        /// <summary>
        /// Determines whether this rectangle has vertical overlap with the input rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test.</param>
        /// <returns>True if this rectangle has vertical overlap with the input rectangle.</returns>
        private bool OverlapsInY(Rectangle2d other)
        {
            return (this.Top <= other.Top && other.Top < this.Bottom) ||
                   (other.Top <= this.Top && this.Top < other.Bottom);
        }

        /// <summary>
        /// Replaces this rectangle with the intersection of itself and the input rectangle.
        /// If there is no intersection, a Rectangle2d.Empty is returned.
        /// </summary>
        /// <param name="r">The rectangle to intersect with.</param>
        public void Intersect(Rectangle2d r)
        {
            //Intersect(r);
            float l0 = this._left, r0 = this._right, t0 = this._top, b0 = this._bottom;
            float l1 = r._left, r1 = r._right, t1 = r._top, b1 = r._bottom;
 
            if(r0 < l1 || r1 < l0 || b0 < t1 || b1 < t0)
            {
                //this._right = l0-1;
                this = Empty; //make it empty
                return;
            }
            
            if(l1 > l0) this._left = l1;
            if(r1 < r0) this._right = r1;
            if(t1 > t0) this._top = t1;
            if(b1 < b0) this._bottom = b1;
        }

        /// <summary>
        /// Returns a Rectangle2d structure that represents the intersection of two rectangles.
        /// If there is no intersection, a Rectangle2d.Empty is returned.
        /// </summary>
        /// <param name="rect1">One rectangle to intersect.</param>
        /// <param name="rect2">The other rectangle to intersect.</param>
        public static Rectangle2d Intersect(Rectangle2d rect1, Rectangle2d rect2)
        {
            Rectangle2d rect = new Rectangle2d(rect1);
            rect.Intersect(rect2);
            return rect;
        }

        /// <summary>
        /// Returns a Rectangle2d structure that represents the union of two rectangles, i.e.,
        /// the smallest of rectangle that properly contains both rectangles.
        /// </summary>
        /// <param name="rect1">Rectangle 1</param>
        /// <param name="rect2">Rectangle 2</param>
        /// <returns>A new Rectangle2d representing the union.</returns>
        public static Rectangle2d Union(Rectangle2d rect1, Rectangle2d rect2)
        {
            if(rect1.IsEmpty && rect2.IsEmpty) return Rectangle2d.Empty;
            if(rect1.IsEmpty) return new Rectangle2d(rect2);
            if(rect2.IsEmpty) return new Rectangle2d(rect1);

            return new Rectangle2d( Math.Min( rect1.Left, rect2.Left ),
                                    Math.Max( rect1.Right, rect2.Right ),
                                    Math.Min( rect1.Top, rect2.Top ),
                                    Math.Max( rect1.Bottom, rect2.Bottom ) );
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Parse a string to form a Rectangle2d.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="formatProvider">IFormatProvider to use.</param>
        /// <returns>Rectangle2d represented by the string s.</returns>
        public static Rectangle2d Parse(string s, IFormatProvider formatProvider)
        {
            char[] separators = { ' ' };
            string[] coords = s.Split( separators );
         
            Debug.Assert( coords.Length == 4, "Rectangle format is \'X Y Width Height\'" );
            float x = float.Parse( coords[ 0 ], formatProvider );
            float y = float.Parse( coords[ 1 ], formatProvider );
            float width = float.Parse( coords[ 2 ], formatProvider );
            float height = float.Parse( coords[ 3 ], formatProvider );            
            return new Rectangle2d(x, x+ width, y, y + height);

        }

        /// <summary>
        /// Write a Rectangle2d to a string
        /// </summary>
        public string ToString(IFormatProvider formatProvider)
        {
            return this.Left.ToString(formatProvider) + " " +
                this.Top.ToString(formatProvider) + " " +
                this.Width.ToString(formatProvider) + " " +
                this.Height.ToString(formatProvider);
        }

#endif 

        #region IComparable Members

        /// <summary>
        /// Interface for IComparable
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            Debug.Assert(obj is Rectangle2d, "Comparing incompatible object");
            return this.CompareTo((Rectangle2d) obj);
        }
        #endregion // IComparable Members

        /// <summary>
        /// Comparing two Rectangle2d, comparison is done for the top left corner;
        /// left to right, top to bottom.
        /// </summary>
        /// <param name="other">Other rectangle.</param>
        /// <returns></returns>
        public int CompareTo(Rectangle2d other)
        {
            Real diffTop = this.Top - other.Top;
            if (diffTop > 0.0f)
            {
                return 1;
            }
            else if (diffTop < 0.0f)
            {
                return -1;
            }
            else 
            {
                Real diffLeft = this.Left - other.Left;
                if (diffLeft > 0.0f)
                {
                    return 1;
                }
                else if (diffLeft < 0.0f)
                {
                    return -1;
                }
                else 
                {
                    return 0;
                }
            }
        }
    }
}
