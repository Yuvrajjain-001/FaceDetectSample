using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

using Dpu.Utility;

using PixelType = System.Single;

namespace Dpu.ImageProcessing
{
    /// <summary>
    /// Collection of images.  Currently by convention they must all be the same size.  The "size" of the 
    /// stack is the size of the included images.  The DEPTH is the number of images.
    /// </summary>
    public class ImageStack : IList
    {
        public ArrayList Images = new ArrayList();

        public int Depth
        {
            get { return Images.Count; }
        }

        public int Width
        {
            get { return GetImage(0).Width; }
        }

        public int Height
        {
            get { return GetImage(0).Height; }
        }

        public Image GetImage( int imageNumber )
        {
            return (Image)Images[imageNumber];
        }

        public Image Red
        {
            get { return (Image)Images[0]; }
        }

        public Image Green
        {
            get { return (Image)Images[1]; }
        }

        public Image Blue
        {
            get { return (Image)Images[2]; }
        }

        public Color GetColor( int col, int row )
        {
            Debug.Assert(Depth == 3, "Cannot get the color of an image stack that does not have 3 channels.");
            return Color.FromArgb(
                (int)Red.GetPixel(col, row),
                (int)Green.GetPixel(col, row),
                (int)Blue.GetPixel(col, row)
                );
        }

        public ImageStack( )
        {
        }

        public ImageStack( Bitmap bitmap )
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
                );

            Images.Add(new Image(data, Image.ColorPlane.red));
            Images.Add(new Image(data, Image.ColorPlane.green));
            Images.Add(new Image(data, Image.ColorPlane.blue));

            bitmap.UnlockBits(data);
        }

        public ImageStack( Size imageSize, int imageNumber )
        {
            for (int n = 0; n < imageNumber; ++n)
                Images.Add(new Image(imageSize));
        }
        /// <summary>
        /// Produces an image stack of the same size as the argument.  Not a copy,  but a dummy copy.
        /// </summary>
        public ImageStack DummyCopy( )
        {
            return new ImageStack(GetImage(0).GetSize, Depth);
        }

        public unsafe void ComputeGrey( Image res )
        {
            if (res.Width != this.Width || res.Height != this.Height)
            {
                throw new Exception("Size does not match.");
            }
            if (Depth != 3)
            {
                throw new Exception("THIS is not color.");
            }

            Image red = this.Red;
            Image blue = this.Blue;
            Image green = this.Green;

            fixed (PixelType* ptred = red.Pixels)
            {
                red.SetPixelData(ptred);

                fixed (PixelType* ptblue = blue.Pixels)
                {
                    blue.SetPixelData(ptblue);

                    fixed (PixelType* ptgreen = green.Pixels)
                    {
                        green.SetPixelData(ptgreen);

                        fixed (PixelType* ptres = res.Pixels)
                        {
                            res.SetPixelData(ptres);

                            for (int nrow = 0; nrow < res.Height; ++nrow)
                            {
                                PixelType* prow = res.Row_Start(nrow);
                                PixelType* prowEnd = res.Row_End(nrow);

                                PixelType* prowRed = red.Row_Start(nrow);
                                PixelType* prowGreen = green.Row_Start(nrow);
                                PixelType* prowBlue = blue.Row_Start(nrow);

                                while (prow < prowEnd)
                                {
                                    *prow = (PixelType)(*prowRed + *prowBlue + *prowGreen) / 3.0f;
                                    prow += res.Step_Col();
                                    prowRed += red.Step_Col();
                                    prowBlue += blue.Step_Col();
                                    prowGreen += green.Step_Col();
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Use a precomputed list of images
        /// </summary>
        public ImageStack( IList images )
        {
            Images.AddRange(images);
        }

        public Bitmap ConvertToBitmap( )
        {
            Bitmap bitmap = new Bitmap(this.Red.Width, this.Red.Height);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb
                );

            Image.ToBitmapData(data, Red, Green, Blue);

            bitmap.UnlockBits(data);

            return bitmap;
        }

        public static void ReduceSize( ImageStack from, ImageStack to )
        {
            for (int n = 0; n < from.Depth; ++n)
            {
                Algorithms.ReduceSize(from.GetImage(n), to.GetImage(n));
            }
        }

        public static void Convolve( ImageStack from, Image kernel, ImageStack to )
        {
            for (int n = 0; n < from.Depth; ++n)
            {
                Image.Convolve(from.GetImage(n), kernel, to.GetImage(n));
            }
        }

        public static void ConvolveReflecting( ImageStack from, int colSkip, int rowSkip, Image kernel, ImageStack to )
        {
            for (int n = 0; n < from.Depth; ++n)
            {
                Image.ConvolutionReflecting(from.GetImage(n), colSkip, rowSkip, kernel, to.GetImage(n));
            }
        }

        public static ImageStack GetSubStack( ImageStack from, Rectangle rect )
        {
            ImageStack toStack = new ImageStack(new Size(rect.Width, rect.Height), from.Depth);
            GetSubStack(from, rect, toStack);
            return toStack;
        }

        public static void GetSubStack( ImageStack from, Rectangle rect, ImageStack to )
        {
            ImageUtils.GetSubImage(from.Red, rect, to.Red);
            ImageUtils.GetSubImage(from.Blue, rect, to.Blue);
            ImageUtils.GetSubImage(from.Green, rect, to.Green);
        }

        #region IList Members

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }


        public int Count
        {
            get
            {
                return Width * Height;
            }
        }

        /// <summary>
        /// Kind of inefficient set and get based on vectors of pixel values.
        /// </summary>
        object System.Collections.IList.this[int index]
        {
            get
            {
                PixelType[] res = new PixelType[Depth];
                for (int n = 0; n < Depth; ++n)
                {
                    res[n] = GetImage(n).GetPixel(index % Width, index / Width);
                }
                return res;
            }
            set
            {
                for (int n = 0; n < Depth; ++n)
                {
                    PixelType[] vector = (PixelType[])value;
                    GetImage(n).SetPixel(index % Width, index / Width, vector[n]);
                }
            }
        }

        public void RemoveAt( int index )
        {
            Debug.Assert(true, "Not implemented.");
        }

        public void Insert( int index, object value )
        {
            Debug.Assert(true, "Not implemented.");
        }

        public void Remove( object value )
        {
            Debug.Assert(true, "Not implemented.");
        }

        public bool Contains( object value )
        {
            Debug.Assert(true, "Not implemented.");
            return false;
        }

        public void Clear( )
        {
            for (int n = 0; n < Depth; ++n)
                this.GetImage(n).Clear();
        }

        public int IndexOf( object value )
        {
            Debug.Assert(true, "Not implemented.");
            return 0;
        }

        /// <summary>
        /// Add an image to the stack.
        /// </summary>
        public int Add( object value )
        {
            Image image = (Image)value;

            if (Depth > 0)
                Debug.Assert(image.Width == Width && image.Height == Height);

            Images.Add(image);
            return Depth;
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public void CopyTo( Array array, int index )
        {
            Debug.Assert(true, "Not implemented.");
        }

        public object SyncRoot
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator( )
        {
            //FUTURE-2005/06/14-DPU -- Add ImageStack.GetEnumerator implementation
            return null;
        }

        #endregion
    }
}