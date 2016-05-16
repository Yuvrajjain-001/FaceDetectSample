using System;
using System.Collections;
using System.Diagnostics;
//using System.Windows.Ink.Analysis.MathLibrary;
using System.Windows.Ink.Analysis.MathLibrary;

namespace Dpu.ImageProcessing
{
	using PixelType = System.Single;

    /// <summary>
    /// An implementation of this interface should provide (efficient) computation
    /// of rectangular sums.
    /// </summary>
    public interface IIntegralImage
    {
        /// <summary>
        /// The bounding box of the integral image
        /// </summary>
        Rectangle2d Bounds { get; }
        /// <summary>
        /// The number of pixels that are black in rect
        /// </summary>
        PixelType ComputeRectSum(Rectangle2d rect); // Return the number of black pixels in rect

        /// <summary>
        /// Render a rectangular region of the integral image out to an image
        /// </summary>
        Image RenderImage(Rectangle2d boundingBox);

        /// <summary>
        /// Add an integral image to the given image
        /// </summary>
        void Add(IIntegralImage img);
    }

    /// <summary>
    /// An image that knows where it is in some global reference frame
    /// </summary>
	[Serializable]
	public class ImageConstituent
	{
		public ImageConstituent(Image img, Vector2d offset)
		{
			this.Img = img;
			this.Offset = offset;
		}

		public Image Img;
		public Vector2d Offset;
	}

    /// <summary>
    /// A collection of ImageConstituent integral images that composite
    /// together.
    /// </summary>
	[Serializable]
	public class CompositeIntegralImage : IIntegralImage
	{
		// List of ImageConstituents
		// Remember that the actual size of the image represented is actually one pixel
		// (in both dimensions) smaller than the size of img
		public ArrayList Constituents = new ArrayList();

        protected Rectangle2d _bounds;
        public Rectangle2d Bounds { get { return _bounds; } }

		public CompositeIntegralImage()
		{
		}

		/// <summary>
		/// Make an integral image from the subimage of img defined by rect.
		/// Put this image at offset.
		/// </summary>
		public CompositeIntegralImage(Image img, Rectangle2d rect, Vector2d offset)
		{
			Add(img, rect, offset, null);
		}

		// Create a single GlobalIntImage from the image component
		public CompositeIntegralImage(ImageComponent ic, Vector2d offset)
		{
			ImageScreener screener = new ComponentIdScreener(ic.iimg, ic.componentId, true);
			Add(ic.img, ImageUtils.RectTo2d(ic.boundingBox), offset, screener);
		}

		/// <summary>
		/// The img is assumed to be at offset.
		/// Extract the rect portion of the img and compute the integral image.
		/// </summary>
		public void Add(Image img, Rectangle2d rect, Vector2d offset, ImageScreener screener)
		{
			Image intImg = new Image((int)rect.Width+1, (int)rect.Height+1);

			int offc = (int)rect.Left;
			int offr = (int)rect.Top;

            if(screener != null)
            {
                for(int r = 0; r < intImg.Height-1; r++)
                {
                    for(int c = 0; c < intImg.Width-1; c++)
                    {
                        // Get the pixel at (c, r), but exclude it if it's screened out
                        PixelType v = img.GetPixel(c+offc, r+offr);
                        if(!screener.Include(c+offc, r+offr, v))
                        {
                            v = 0; //ImageUtils.whitePixel;
                        }

                        v += intImg.GetPixel(c+1, r) + intImg.GetPixel(c, r+1) - intImg.GetPixel(c, r);
                        intImg.SetPixel(c+1, r+1, v);
                    }
                }
            }
            else
            {
                //FIXME_MMS: test optimized version
                //Image subImg = ImageUtils.GetSubImage(image, new Rectangle2d(offset, rect.Size));
                //intImg = Image.IntegralImage(subImg, res);

                for(int r = 0; r < intImg.Height-1; r++)
                {
                    for(int c = 0; c < intImg.Width-1; c++)
                    {
                        // Get the pixel at (c, r), but exclude it if it's screened out
                        PixelType v = img.GetPixel(c+offc, r+offr);
                        v += intImg.GetPixel(c+1, r) + intImg.GetPixel(c, r+1) - intImg.GetPixel(c, r);
                        intImg.SetPixel(c+1, r+1, v);
                    }
                }
            }

			AddImageConstituent(new ImageConstituent(intImg, rect.TopLeft + offset)); //FIXME_MMS: re-examine this
		}

        public void Add(IIntegralImage iimg)
        {
            Add((CompositeIntegralImage)iimg);
        }

        /// <summary>
        /// Add the contents of the given integral image to this.
        /// </summary>
		public void Add(CompositeIntegralImage iimg)
		{
            Add(iimg, new Vector2d(0, 0));
		}

        /// <summary>
        /// Add the contents of the given integral image, offset
        /// by the given amount.
        /// </summary>
		public void Add(CompositeIntegralImage iimg, Vector2d offset)
		{
			foreach(ImageConstituent constituent in iimg.Constituents)
			{
				// Add an additional offset.
				Vector2d v = new Vector2d(constituent.Offset.X+offset.X, constituent.Offset.Y+offset.Y);
				AddImageConstituent(new ImageConstituent(constituent.Img, v));
			}
		}

        /// <summary>
        /// Return number of pixels that are black in rect
        /// </summary>
		public PixelType ComputeRectSum(Rectangle2d rect)
		{
			PixelType sum = 0;

			// Sum up each of the integral image's imgFrags
            foreach(ImageConstituent imgFrag in Constituents)
            {
                sum += ComputeRectSum(imgFrag, rect);
            }

			return sum; //FIXME_MMS / ImageUtils.RectArea(rect);
		}

		// Return absolute sum
		private static PixelType ComputeRectSum(ImageConstituent intImg, Rectangle2d rect)
		{
			// Bounding box of this global image (relative coordinates)
			Rectangle2d bbox = new Rectangle2d(0, intImg.Img.Width-1, 0, intImg.Img.Height-1);

			// Clip the query rectangle
			rect.Offset(-intImg.Offset.X, -intImg.Offset.Y);
			Rectangle2d clippedRect = Rectangle2d.Intersect(rect, bbox);

            if (rect.IsEmpty)
            {
                return 0.0f;
            }
            else 
            {
                PixelType top    = clippedRect.Top;
                PixelType bottom = clippedRect.Bottom;
                PixelType right  = clippedRect.Right;
                PixelType left   = clippedRect.Left;

                // Coordinates of integer box which lies inside the rectangle
                int  intTop    = (int) Math.Ceiling(top);
                int  intBottom = (int) Math.Floor(bottom);
                int  intLeft   = (int) Math.Ceiling(left);
                int  intRight  = (int) Math.Floor(right);

                //Console.WriteLine("RRR " + rect + " " + clippedRect + " " + intImg.img.BoundingRect);

                PixelType sum = ComputeRectSum(intImg.Img, intLeft, intTop, intRight, intBottom);

                // Top slice
                if(intTop > 0)
                    sum += (intTop - top) * ComputeRectSum(intImg.Img, intLeft, intTop-1, intRight, intTop);

                // Bottom slice
                if(intBottom < intImg.Img.Height-1)
                    sum += (bottom - intBottom) * ComputeRectSum(intImg.Img, intLeft, intBottom, intRight, intBottom+1);

                // Left slice
                if(intLeft > 0)
                    sum += (intLeft - left) * ComputeRectSum(intImg.Img, intLeft-1, intTop, intLeft, intBottom);

                // Right slice
                if(intRight < intImg.Img.Width-1)
                    sum += (right - intRight) * ComputeRectSum(intImg.Img, intRight, intTop, intRight+1, intBottom);

                // TODO: Leave out corners for now.
				// let delta1 = intTop-top, delta2 = bottom-intBottom, delta3 = intLeft -left, 
				// delta4 = right-intRight
				// The missing term is:
				// delta1*delta3*GetPixelOfIntImg(intLeft-1, intTop-1)+
				// delta1*delta4*GetPixelOfIntImg(intTop-1, intRight)+
				// delta4*delta2*GetPixelOfIntImg(intBottom, intRight)+
				// delta2*delta3*GetPixelOfIntImg(intLeft-1, intBottom;
                return sum;
            }
		}

		/// <summary>
		/// Assuming the image is an integral image,  compute the sum inside the rectangle.
		/// Returned value is unnormalized.
		/// </summary>
		private static PixelType ComputeRectSum(Image intImg, int left, int top, int right, int bottom) 
		{
			if(!(left >= 0 && top >= 0 && right < intImg.Width && bottom < intImg.Height))
				throw new ArgumentException("Out of bounds");

			PixelType sum = 0;
			sum += intImg.GetPixel(left, top);
			sum += intImg.GetPixel(right, bottom);
			sum -= intImg.GetPixel(left, bottom);
			sum -= intImg.GetPixel(right, top);
			return sum;
		}

		private static PixelType GetPixelOfIntImg(Image intImg, int c, int r)
		{
			return ComputeRectSum(intImg, c, r, c+1, r+1);
		}

		public Image RenderImage(Rectangle2d rect)
		{
			/*if(!ImageUtils.Rect2dContains(rect, boundingBox))
				throw new ArgumentException(rect + " doesn't contain whole image " + boundingBox);*/

			// Create the final result image
			Image finalImg = new Image((int)rect.Width, (int)rect.Height);

			foreach(ImageConstituent constituent in Constituents) // For each integral image
			{
				Image intImg = constituent.Img;

				// Offset of imgFrag from the reference rect
				int offc = (int)(constituent.Offset.X - rect.Left);
				int offr = (int)(constituent.Offset.Y - rect.Top);

				for(int c = Math.Max(-offc, 0); c+offc < finalImg.Width && c < intImg.Width-1; c++)
				{
					for(int r = Math.Max(-offr, 0); r+offr < finalImg.Height && r < intImg.Height-1; r++)
					{
						PixelType v = finalImg.GetPixel(c+offc, r+offr) + GetPixelOfIntImg(intImg, c, r);
						finalImg.SetPixel(c+offc, r+offr, v);
					}
				}
			}
			return finalImg;
		}

		void AddImageConstituent(ImageConstituent constituent)
		{
			Vector2d size = new Vector2d(constituent.Img.Width-1, constituent.Img.Height-1);
			Rectangle2d bbox = new Rectangle2d(constituent.Offset, size+constituent.Offset);
    		_bounds = (_bounds.IsEmpty) ? bbox : Rectangle2d.Union(bbox, _bounds);
			Constituents.Add(constituent);
		}
	}

	/// <summary>
	/// A collection of non-overlapping solid black rectangles.
	/// </summary>
	[Serializable]
	public class SkeletonIntegralImage : IIntegralImage
	{
        protected Rectangle2d _bounds;
        public Rectangle2d Bounds { get { return _bounds; } }

		public SkeletonIntegralImage()
		{
			rects = new ArrayList();
		}

		public SkeletonIntegralImage(Rectangle2d rect)
		{
			rects = new ArrayList();
			Add(rect);
		}

		// Make an image with the skeletons of the pieces of iimg
		public SkeletonIntegralImage(CompositeIntegralImage iimg)
		{
			rects = new ArrayList();
			foreach(ImageConstituent constituent in iimg.Constituents)
			{
                System.Drawing.Rectangle intRect = constituent.Img.BoundingRect;
				Rectangle2d rect = new Rectangle2d(intRect.Left, intRect.Right-1, intRect.Top, intRect.Bottom-1);
				rect.Offset(constituent.Offset);
				Add(rect);
			}
		}

		public void Add(Rectangle2d rect)
		{
			rects.Add(rect);
			_bounds = (_bounds.IsEmpty) ? rect : Rectangle2d.Union(rect, _bounds);
		}

        public void Add(IIntegralImage iimg)
        {
            Add((SkeletonIntegralImage)iimg);
        }

		public void Add(SkeletonIntegralImage iimg)
		{
			foreach(Rectangle2d rect in iimg.rects)
				Add(rect);
		}

		public void Add(SkeletonIntegralImage iimg, Vector2d offset)
		{
			foreach(Rectangle2d rect in iimg.rects)
			{
				Rectangle2d newRect = rect;
				newRect.Offset(offset);
				Add(newRect);
			}
		}

		public PixelType ComputeRectSum(Rectangle2d rect)
		{
			PixelType v = 0;
			foreach(Rectangle2d r in rects)
			{
				v += ImageUtils.RectArea(Rectangle2d.Intersect(r, rect));
			}
			return v; // / ImageUtils.RectArea(rect);
		}

		public Image RenderImage(Rectangle2d destRect)
		{
			Image finalImg = new Image((int)destRect.Width, (int)destRect.Height);

			foreach(Rectangle2d rect in rects)
			{
				int offc = (int)(rect.Left - destRect.Left);
				int offr = (int)(rect.Top - destRect.Top);

				for(int c = Math.Max(-offc, 0); c+offc < finalImg.Width && c < rect.Width; c++)
				{
					for(int r = Math.Max(-offr, 0); r+offr < finalImg.Height && r < rect.Height; r++)
					{
						// This is valid if the rectangles are non-overlapping
						finalImg.SetPixel(c+offc, r+offr, ImageUtils.blackPixel); 
					}
				}
			}
			return finalImg;
		}

		public ArrayList Rects { get { return rects; } }

		ArrayList rects;
	}


#if false
	// Contains a set of integral images, each one is chopped, meaning that we consider only
	// a specified sub-rectangle of that image.
	public class ChoppedIntegralImage : IntegralImageBase
	{
		internal class ChoppedIntImg
		{
			public ChoppedIntImg(IntegralImageBase iimg, Rectangle2d rect)
			{
				this.iimg = iimg;
				this.rect = rect;
			}
			public IntegralImageBase iimg;
			public Rectangle2d rect;
		}

		ArrayList ciimgs;

		public ChoppedIntegralImage()
		{
			ciimgs = new ArrayList();
		}

		public void AddWithBottomChopped(IntegralImageBase iimg, float frac)
		{
			Rectangle2d rect = iimg.GetBoundingBox().
			ChoppedIntImg ciimg = new ChoppedIntImg(iimg, rect);
			ciimg.
			rects.Add(rect);
			if(boundingBox.IsEmpty)
				boundingBox = rect;
			else
				boundingBox = Rectangle2d.Union(rect, boundingBox);
		}

		public override void Add(IntegralImageBase iimg)
		{
			throw new NotSupportedException("Don't need this now");
		}

		public override void Add(IntegralImageBase iimg, Vector2d offset)
		{
			throw new NotSupportedException("Don't need this now");
		}

		public override PixelType ComputeRectSum(Rectangle2d rect)
		{
			PixelType v = 0;
			foreach(Rectangle2d r in rects)
			{
				v += ImageUtils.RectArea(Rectangle2d.Intersect(r, rect));
			}
			return v / ImageUtils.RectArea(rect);
		}

		public override Image RenderImage(Rectangle2d destRect)
		{
			Image final_img = new Image((int)destRect.Width, (int)destRect.Height);

			foreach(Rectangle2d rect in rects)
			{
				int offc = (int)(rect.Left - destRect.Left);
				int offr = (int)(rect.Top - destRect.Top);

				for(int c = Math.Max(-offc, 0); c+offc < final_img.NumCols && c < rect.Width; c++)
				{
					for(int r = Math.Max(-offr, 0); r+offr < final_img.Height && r < rect.Height; r++)
					{
						// This is valid if the rectangles are non-overlapping
						final_img.SetPixel(c+offc, r+offr, ImageUtils.blackPixel); 
					}
				}
			}
			return final_img;
		}

		ArrayList rects;
	}
#endif
}
