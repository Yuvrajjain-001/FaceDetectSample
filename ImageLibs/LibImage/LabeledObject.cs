using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections;
using System.Xml.Serialization;
using System.IO;

using System.Windows.Ink.Analysis.MathLibrary;

using Dpu.Utility;
using Dpu.ImageProcessing;

namespace Dpu.ImageProcessing
{

    public interface IOverlay
    {
        void Draw(Graphics gfx);
    }

    [Serializable]
    [XmlInclude(typeof(LabeledBox))]
    [XmlInclude(typeof(LabeledColorBox))]
    [XmlInclude(typeof(LabeledFace))]
    [XmlInclude(typeof(LabeledInterestPoint))]
    public abstract class LabeledObject : IOverlay
    {
        public LabeledObject() { }

        public abstract void Draw(Graphics gfx);
    }


    [Serializable]
    public class LabeledInterestPoint : LabeledObject
    {
        public Vector2d Location;
        public float Scale;
        public Angle Angle;

        public LabeledInterestPoint(float x, float y, float scale, float angle)
        {
            Location = new Vector2d(x, y);
            Scale = scale;
            Angle = new Angle(-angle + Math.PI);
        }

        public LabeledInterestPoint() { }


        public override void Draw(Graphics gfx)
        {

            Pen penBox = new Pen(Color.Cyan, 1.0f);
            Pen penLine = new Pen(Color.Red, 1.0f);

            // Changed
            float size = (float)(8 * Scale / 1.414213562373095);
            Rectangle2d rect = Rectangle2d.FromXYWH(Location.X - size, Location.Y - size, size * 2, size * 2);
            RotatedRectangle rot = new RotatedRectangle(rect, Angle);
            Vector2d topLeft = rot.TopLeft;
            Vector2d topRight = rot.TopRight;
            Vector2d botLeft = rot.BottomLeft;
            Vector2d botRight = rot.BottomRight;
            gfx.DrawLine(penBox, topLeft.X, topLeft.Y, topRight.X, topRight.Y);
            gfx.DrawLine(penBox, topRight.X, topRight.Y, botRight.X, botRight.Y);
            gfx.DrawLine(penBox, botRight.X, botRight.Y, botLeft.X, botLeft.Y);
            gfx.DrawLine(penBox, botLeft.X, botLeft.Y, topLeft.X, topLeft.Y);
            Vector2d up = Common.RotatePoint(Angle, new Vector2d(0, 0), new Vector2d(-size, 0));
            gfx.DrawLine(penLine, Location.X, Location.Y, Location.X + up.X, Location.Y + up.Y);
        }
    }

    [Serializable]
    public class LabeledEllipse : LabeledObject
    {
        public PointF Location;
        public float Major;
        public float Minor;
        public float Angle;

        public LabeledEllipse(float x, float y, float angle, float major, float minor)
        {
            Location = new PointF(x, y);
            Major = major;
            Minor = minor;
            Angle = angle;
        }

        public LabeledEllipse() { }


        public override void Draw(Graphics gfx)
        {
            Matrix oldTrans = gfx.Transform.Clone();
            Matrix newTrans = gfx.Transform.Clone();
            Matrix rot = new Matrix();
            Matrix trans = new Matrix();

            rot.Rotate(Angle);
            trans.Translate(Location.X, Location.Y);

            trans.Multiply(rot);
            newTrans.Multiply(trans);

            gfx.Transform = newTrans;

            Pen penBox = new Pen(Color.Cyan, 1.0f);
            Pen penLine = new Pen(Color.Red, 1.0f);

            gfx.DrawEllipse(penBox, -Major / 2, -Minor / 2, Major, Minor);

            gfx.DrawLine(penLine, 0, 0, Major, 0);

            gfx.Transform = oldTrans;
        }
    }



    /// <summary>
    /// An object in an image whose bounding box has been found.
    /// </summary>
    [Serializable]
    public class LabeledBox : LabeledObject
    {
        public Rectangle2d Box;
        public string Name;
        /// <summary>
        /// Identifies different faces.
        /// </summary>
        public int Id;

        public LabeledBox() { }

        public LabeledBox(Rectangle2d rect)
        {
            Box = rect;
            Name = "";
        }

        public override string ToString()
        {
            return String.Format("<Labeled Box {0} {1}>", Id, Box.ToString());
        }


        public void Draw(Graphics gfx, Pen pen)
        {
            Rectangle2d rect = this.Box;
            gfx.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public override void Draw(Graphics gfx)
        {
            Pen penBox = new Pen(Color.Cyan, 1.0f);
            Draw(gfx, penBox);
        }
    }



    /// <summary>
    /// A colored box.
    /// </summary>
    [Serializable]
    public class LabeledColorBox : LabeledBox
    {
        public Color Color = Color.Cyan;

        public LabeledColorBox() { } // For xml serialization

        public LabeledColorBox(Rectangle2d rect, Color c)
            : base(rect)
        {
            Color = c;
        }

        public override void Draw(Graphics gfx)
        {
            Draw(gfx, new Pen(Color, 1.0f));
        }
    }


    /// <summary>
    /// A point on an object which has been identified in an image.
    /// </summary>
    [Serializable]
    public class ObjectPoint
    {
        public String Name;
        public Vector2d Location;
    }



    /// <summary>
    /// An object in an image whose bounding box has been found.
    /// </summary>
    [Serializable]
    public class LabeledFace : LabeledBox
    {
        [XmlArrayItem(typeof(ObjectPoint), ElementName = "Point")]
        public ArrayList PointList;

        public LabeledFace() { }


        public override void Draw(Graphics gfx)
        {
            Pen penBox = new Pen(Color.Yellow, 1.0f);
            Draw(gfx, penBox);
            throw new System.NotImplementedException();
        }
    }

}