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
    /// Struct for a rotated rectangle. A rotated rectangle is a rectangle rotated
    /// by an angle about the specified center (the center of the rectangle if unspecified).
    /// </summary>
    public class RotatedRectangle
    {
        #region Fields
        private Rectangle2d _originalRectangle; // original rectangle (unrotated, upright)
        private Angle _angle; // rotation angle (counterclockwise from the positive x axis)
        private Vector2d _center; // rotation center
        #endregion // Fields

        #region Properties
        /// <summary>
        /// The original rectangle ((unrotated, upright)
        /// </summary>
        public Rectangle2d OriginalRectangle
        {
            get { return _originalRectangle; }
            set {  _originalRectangle = value; }
        }

        /// <summary>
        /// The counterclockwise rotation angle
        /// </summary>
        public Angle Angle 
        {
            get { return _angle; }
            set { _angle = value; }
        }

        /// <summary>
        /// The rotation center
        /// </summary>
        public Vector2d Center
        {
            get { return this._center; }
            set { this._center = value; }
        }

        /// <summary>
        /// The polyline of the rectangle. For debug display purposes.
        /// </summary>
        public Polyline Polyline
        {
            get
            {
                // Do not assert for !_originalRectangle.IsEmpty, because potentially the original
                // rectangle is a thin rectangle (either Height or Width is not zero).
                Debug.Assert(_originalRectangle.Height > 0 ||
                    _originalRectangle.Width > 0, "Rotated rectangle has not been set");

                        
                Vector2d[] points = new Vector2d[]
                                    { this.TopLeft, this.TopRight, this.BottomRight, this.BottomLeft };
              
                // Don't ever filter out duplicate here, because all client expect this polyline
                // to have 4 vertices... always!.
                return new Polyline( points, /* filterDuplicate = */ false );
            }
        }

        /// <summary>
        /// The top-left corner point of the rectangle after rotation.
        /// </summary>
        public Vector2d TopLeft
        {
            get
            {
                return Common.RotatePoint( this._angle,
                                           this._center,
                                           this._originalRectangle.TopLeft );
            }
        }

        /// <summary>
        /// The top-right corner point of the rectangle after rotation.
        /// </summary>
        public Vector2d TopRight
        {
            get
            {
                return Common.RotatePoint( this._angle,
                                           this._center,
                                           this._originalRectangle.TopRight );
            }
        }

        /// <summary>
        /// The bottom-left corner point of the rectangle after rotation.
        /// </summary>
        public Vector2d BottomLeft
        {
            get
            {
                return Common.RotatePoint( this._angle,
                                           this._center,
                                           this._originalRectangle.BottomLeft );
            }
        }

        /// <summary>
        /// The bottom-right corner point of the rectangle after rotation.
        /// </summary>
        public Vector2d BottomRight
        {
            get
            {
                return Common.RotatePoint( this._angle,
                                           this._center,
                                           this._originalRectangle.BottomRight );
            }
        }
        #endregion // Properties

        #region Methods
        /// <summary>
        /// Creates an empty upright rectangle.
        /// </summary>
        public RotatedRectangle()
        {
        }
        
        /// <summary>
        /// Creates an identical RotatedRectangle as the input.
        /// </summary>
        /// <param name="other"></param>
        public RotatedRectangle( RotatedRectangle other ) :
            this( other.OriginalRectangle, other.Angle, other.Center )
        {
        }

        /// <summary>
        /// Creates a RotatedRectangle by rotating the input upright rectangle about its
        /// center and by the specified angle counterclockwise.
        /// </summary>
        /// <param name="originalRect">The upright rectangle to rotate.</param>
        /// <param name="angle">The counterclockwise rotation angle.</param>
        public RotatedRectangle( Rectangle2d originalRect, Angle angle )
        {
            this._angle = angle;
            this._originalRectangle = originalRect;
            this._center = originalRect.Center;
        }

        /// <summary>
        /// Creates a RotatedRectangle by rotating the input upright rectangle about the
        /// specified center and by the specified angle counterclockwise.
        /// </summary>
        /// <param name="rect">The upright rectangle to rotate.</param>
        /// <param name="angle">The rotation angle.</param>
        /// <param name="center">The counterclockwise rotation center.</param>
        public RotatedRectangle( Rectangle2d rect, Angle angle, Vector2d center )
        {
            this._originalRectangle = rect;
            this._center = center;
            this._angle = angle;
        }
        #endregion

    }
}
