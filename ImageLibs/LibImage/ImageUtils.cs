using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
//using System.Windows.Ink.Analysis.MathLibrary;
using System.Windows.Ink.Analysis.MathLibrary;

using PixelType = System.Single;

namespace Dpu.ImageProcessing
{
	/// <summary>
	/// Summary description for ImageUtils.
	/// </summary>
	public class ImageUtils
	{
        public static PixelType blackPixel = 1;
        public static PixelType whitePixel = 0;
        public static PixelType defaultThreshold = (blackPixel + whitePixel) / 2;
		// XXX: move?
//#if INTERNAL_DPU
        public static System.Drawing.Rectangle RectFrom2d(Rectangle2d r)
		{
			return new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
		}

		public static Rectangle2d RectTo2d(System.Drawing.Rectangle r)
		{
			return new Rectangle2d(r.Left, r.Right, r.Top, r.Bottom);
		}

		public static float RectArea(Rectangle2d r)
		{
			return r.Size.Height * r.Size.Width;
		}

		// Return the smallest rectangle centered at v which contains r
		public static Rectangle2d CenterContainingRect(Rectangle2d r, Vector2d v)
		{
			float xdist = Math.Max(Math.Abs(v.X - r.Left), Math.Abs(v.X - r.Right));
			float ydist = Math.Max(Math.Abs(v.Y - r.Top), Math.Abs(v.Y - r.Bottom));
			return Rectangle2d.FromXYWH(v.X - xdist, v.Y - ydist, xdist*2, ydist*2);
		}

		public static Vector2d RectCentroid(Rectangle2d r)
		{
			return new Vector2d((r.Left+r.Right)/2, (r.Top+r.Bottom)/2);
		}

		// The Contains() in Rectangle2d is broken.
		// It doesn't give the same results when the rectangle is flipped.
		public static bool Rect2dContains(Rectangle2d r1, Rectangle2d r2)
		{
			return r1.Top <= r2.Top && r1.Bottom >= r2.Bottom && r1.Left <= r2.Left && r1.Right >= r2.Right;
		}

		public static PixelType BitToPixel(bool b)
		{
			return b ? whitePixel : blackPixel;
		}

		public static bool PixelToBit(PixelType p)
		{
			if(p == whitePixel) return true;
			if(p == blackPixel) return false;
			throw new ArgumentException("Only black/white supported");
		}

		public static Color PixelTypeToColor(PixelType p)
		{
			if(p < 0) p = 0;
			if(p > 1) p = 1;
			/*if(p < 0 || p > 1)
				throw new ArgumentException("Invalid pixel value");*/
			int x = (int)(p*255);
			return Color.FromArgb(x, x, x);
		}

		public static PixelType ColorToPixelType(Color c)
		{
			return 1-c.R/255f;
		}

		/// <summary>
		/// Does the same thing as BitmapToImage(), but is faster because it accesses the
		/// bitmap directly instead of calling the slow GetPixel().  The disadvantage is that
		/// it only works for black and white bitmaps.  It can, however, easily be extended
		/// to work for bitmaps with other number of colors.
		/// </summary>
		public static Image BitmapToImageFast(Bitmap bmp)
		{
			if(bmp.PixelFormat != PixelFormat.Format1bppIndexed)
				throw new NotSupportedException("Only 1 pixel/bit format supported");
			
			BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
				ImageLockMode.ReadOnly, bmp.PixelFormat);

			if(bd.Stride < 0)
				throw new NotSupportedException("Positive stride supported only");

			Image img = new Image(bmp.Width, bmp.Height);
			unsafe 
			{
				byte *p = (byte *)bd.Scan0.ToPointer();
				for(int r = 0; r < bmp.Height; r++)
				{
					for(int c = 0; c < bmp.Width; c++)
					{
						bool bit = ((p[c/8] >> (7-(c%8))) & 1) != 0;
						img.SetPixel(c, r, BitToPixel(bit));
					}
					p += bd.Stride;
				}
			}

			bmp.UnlockBits(bd);
			return img;
		}

        /// <summary>

        /// Fast conversion for grayscale images

        /// </summary>

        public static Image BitmapToImageGrayscale(Bitmap bmp)
        {

            if(bmp.PixelFormat != PixelFormat.Format24bppRgb)
                throw new NotSupportedException("Only 24 bit/pixel format supported");
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);
            if(bd.Stride < 0)
                throw new NotSupportedException("Positive stride supported only");
            Image img = new Image(bmp.Width, bmp.Height);
            unsafe 
            {
                byte *p = (byte *)bd.Scan0.ToPointer();
                int offset = bd.Stride - bmp.Width*3;
                for(int r = 0; r < bmp.Height; r++)
                {
                    for(int c = 0; c < bmp.Width; c++)
                    {
                        byte val = p[0]; //ignore G and B channel
                        PixelType pix = ((PixelType)val)/255f;
                        img.SetPixel(c, r, pix);
                        p += 3;
                    }
                    p += offset;
                }
            }
            bmp.UnlockBits(bd);

            return img;
        }

		public static Rectangle GetBoundingRectangle(Image img)
		{
			int minr = img.Height, maxr = 0;
			int minc = img.Width, maxc = 0;
			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					if(img.GetPixel(c, r) != whitePixel)
					{
						minr = Math.Min(minr, r);
						minc = Math.Min(minc, c);
						maxr = Math.Max(maxr, r);
						maxc = Math.Max(maxc, c);
					}
				}
			}
			return new Rectangle(minc, minr, maxc-minc+1, maxr-minr+1);
		}

		public static Image ScreenImage(Image img, ImageScreener screener)
		{
			Image newImg = new Image(img.Width, img.Height);
			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					PixelType val = img.GetPixel(c, r);
					if(!screener.Include(c, r, val)) val = whitePixel;
					newImg.SetPixel(c, r, val);
				}
			}
			return newImg;
		}

        /// <summary>
        /// Return a new image based on the rectangle provided whose contents are img.
        /// </summary>
        public static Image GetSubImage(Image img, Rectangle rect)
        {
            Image subImg = new Image(rect.Width, rect.Height);
            GetSubImage(img, rect, subImg);
            return subImg;
        }

        /// <summary>
        /// Return a new image based on the rectangle provided whose contents are img.
        /// </summary>
        public static void GetSubImage(Image img, Rectangle rect, Image toImg)
        {
            
            if(rect.Width != toImg.Width || rect.Height != toImg.Height)
                throw new ArgumentException("Rectangle must match the size of the toImg");

            if(rect.Left < 0 || rect.Top < 0 || rect.Right > img.Width || rect.Bottom > img.Height)
                throw new ArgumentException("Rectangle must fit in bounds of image");

            for(int r = 0; r < rect.Height; r++)
            {
                for(int c = 0; c < rect.Width; c++)
                {
                    toImg.SetPixel(c, r, img.GetPixel(rect.Left + c, rect.Top + r));
                }
            }
        }

		/// <summary>
		/// Return a new image based on the rectangle provided whose contents are iimg.
		/// </summary>
		public static DiscreteImage GetSubDiscreteImage(DiscreteImage discreteImg, Rectangle rect)
		{
			DiscreteImage subDiscreteImg = new DiscreteImage(rect.Width, rect.Height);
			if(rect.Left < 0 || rect.Top < 0 || rect.Right > discreteImg.Width || rect.Bottom > discreteImg.Height)
				throw new ArgumentException("Rectangle must fit in bounds of image");

			for(int r = 0; r < rect.Height; r++)
			{
				for(int c = 0; c < rect.Width; c++)
				{
					subDiscreteImg.SetPixel(c, r, discreteImg.GetPixel(rect.Left + c, rect.Top + r));
				}
			}
			return subDiscreteImg;
		}

		/// <summary>
		/// Faster version of ImageToBitmap().  Only writes ref black and white.
		/// </summary>
		public static Bitmap ImageToBitmapFast(Image img)
		{
			Bitmap bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format1bppIndexed);

			BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
				ImageLockMode.WriteOnly, bmp.PixelFormat);

			unsafe 
			{
				fixed(PixelType *ptimg = img.Pixels) 
				{
					img.SetPixelData(ptimg);

					byte *p = (byte *)bd.Scan0.ToPointer();
					for(int r = 0; r < bmp.Height; r++)
					{
						for(int c = 0; c < bmp.Width; c++)
						{
							PixelType pixel = img.GetPixelFast(c, r);
							if(c % 8 == 0) p[c/8] = 0;
							if(PixelToBit(pixel)) p[c/8] |= (byte)(1 << (7-(c%8)));
						}
						p += bd.Stride;
					}
				}
			}

			bmp.UnlockBits(bd);
			return bmp;
		}

		public static Image BitmapToImage(Bitmap bmp)
		{
			Image img = new Image(bmp.Width, bmp.Height);
			
			for(int r = 0; r < bmp.Height; r++)
				for(int c = 0; c < bmp.Width; c++)
					img.SetPixel(c, r, ColorToPixelType(bmp.GetPixel(c, r)));

			return img;
		}
		
        public static Image BitmapToImage(Bitmap bmp, Rectangle rect, int page)
		{
			Image img = new Image(rect.Width, rect.Height);
			bmp.SelectActiveFrame(new FrameDimension(bmp.FrameDimensionsList[0]), page);

			for(int r = 0; r < rect.Height; r++)
				for(int c = 0; c < rect.Width; c++)
					img.SetPixel(c, r, ColorToPixelType(bmp.GetPixel(c + rect.Left, r + rect.Top)));

			return img;
		}

		public static Bitmap ImageToBitmap(Image img)
		{
			Bitmap bmp = new Bitmap(img.Width, img.Height);

			for(int r = 0; r < img.Height; r++)
				for(int c = 0; c < img.Width; c++)
					bmp.SetPixel(c, r, PixelTypeToColor(img.GetPixel(c, r)));

			return bmp;
		}

        /// <summary>
        /// Free of the annoyance that the file is locked while the bitmap is open.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public unsafe static Bitmap ReadBitmapFile(string filename)
        {
            if (System.IO.File.Exists(filename)) 
            {
                Bitmap bitmap = new Bitmap(filename);
                Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData input = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData output = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                UInt32* from = (UInt32*) input.Scan0.ToPointer();
                UInt32* to   = (UInt32*) output.Scan0.ToPointer();

                for(int n = 0; n < bitmap.Height * bitmap.Width; ++n) 
                {
                    *(to++) = *(from++);
                }
                bitmap.UnlockBits(input);
                result.UnlockBits(output);
                bitmap.Dispose();

                return result;
            }
            else 
                return null;
        }

		public static void MakeMonochrome(Image img, PixelType threshold)
		{
			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					PixelType v = img.GetPixel(c, r);
					v = v < threshold ? whitePixel : blackPixel;
					img.SetPixel(c, r, v);
				}
			}
		}

		public static Bitmap ImageToBitmapStretch(Image img)
		{
			Bitmap bmp = new Bitmap(img.Width, img.Height);
			PixelType pixMin = img.GetPixel(0, 0);
			PixelType pixMax = img.GetPixel(0, 0);
			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					PixelType pix = img.GetPixel(c,r);
					if (pix < pixMin)
						pixMin = pix;
					if (pix > pixMax)
						pixMax = pix;
				}
			}


			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					if ((pixMax - pixMin) > 0) 
					{
						bmp.SetPixel(c, r, PixelTypeToColor((img.GetPixel(c,r) - pixMin) / (pixMax - pixMin)));
					}
					else
					{
						bmp.SetPixel(c, r, Color.Black);
					}
				}
			}
			return bmp;
		}

		public static void DumpImageAsAscii(Image img, string file)
		{
			StreamWriter sw = new StreamWriter(file);
			DumpImageAsAscii(img, sw);
			sw.Close();
		}

		public static void DumpImageAsAscii(Image img, TextWriter writer)
		{
			string chars = ".-+*#";
			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					PixelType val = img.GetPixel(c, r);
					char ch = '?';
					if(val >= 0 && val <= 1)
						ch = chars[(int)(val*(chars.Length-1))];
					else
						Console.WriteLine(val);
					//throw new ArgumentException("Image values must be in [0,1]; we have " + val);
					writer.Write(ch);
				}
				writer.WriteLine("");
			}
		}
		public static Image RescaleImage(Image img, int newWidth, int newHeight)
		{
			return RescaleImageWithBorder(img, null, img.BoundingRect, newWidth, newHeight, 0, 0);
		}
		/// <summary>
		/// This is a big hairy do-it-all function.
		/// This function creates a new image of size (newWidth, newHeight) based off img with the following properties.
		/// The internal image is the image inside of the new image within the borders specified by *borderFrac.
		/// img[rect] should fit with the same perspective inside the internal image.
		/// </summary>
		/// <param name="border">the thickness of the border is borderFrac of the image dimensions</param>
		/// <returns></returns>
		public static Image RescaleImageWithBorder(Image img, ComponentIdScreener screener, Rectangle rect, int newWidth, int newHeight,
			double wborderFrac, double hborderFrac)
		{
			Image newImg = new Image(newWidth, newHeight);

			// Compute the border and the stuff remaining
			int wborder = (int)(wborderFrac * newWidth);
			int hborder = (int)(hborderFrac * newHeight);
			int maxInternalWidth = newWidth - 2*wborder; // max width of image excluding border
			int maxInternalHeight = newHeight - 2*hborder; // max height of image excluding border

			Debug.Assert(maxInternalHeight > 0 && maxInternalWidth > 0);

			// Compute dimensions and offset of the internal image
			int internalWidth = maxInternalWidth, internalHeight = maxInternalHeight;
			if(internalWidth * rect.Height > internalHeight * rect.Width)
				internalWidth = Math.Max(1, internalHeight * rect.Width / rect.Height);
			else
				internalHeight = Math.Max(1, internalWidth * rect.Height / rect.Width);
			int internalWOffset = wborder + (maxInternalWidth - internalWidth) / 2;
			int internalHOffset = hborder + (maxInternalHeight - internalHeight) / 2;

			// relevantRect is the part of the img that actually should get copied over
			int relevantMinC = Math.Max(0, rect.Left - internalWOffset*rect.Width/internalWidth - 1);
			int relevantMinR = Math.Max(0, rect.Top - internalHOffset*rect.Height/internalHeight - 1);
			int relevantMaxC = Math.Min(img.Width-1, rect.Right + internalWOffset*rect.Width/internalWidth + 1);
			int relevantMaxR = Math.Min(img.Height-1, rect.Bottom + internalHOffset*rect.Height/internalHeight + 1);
			Rectangle relevantRect = new Rectangle(relevantMinC, relevantMinR, relevantMaxC-relevantMinC+1, relevantMaxR-relevantMinR+1);
	
			// Reduce img only to what's relevant
			img = GetSubImage(img, relevantRect);
			if(screener != null)
				screener.iimg = GetSubDiscreteImage(screener.iimg, relevantRect);
			rect.Offset(-relevantMinC, -relevantMinR);

			// Apply screener here, after extracting the relevant part of img,
			// and before scaling, so that we can use iimg effectively
			if(screener != null)
				img = ScreenImage(img, screener);

			// Downsample so that the rectangle would fit inside the internal image
			// The size of the rectangle halves; when it doesn't exactly half, it errs on the side of
			// including too much
			Debug.Assert(internalHeight > 0 && internalWidth > 0);
			while(rect.Height > internalHeight || rect.Width > internalWidth)
			{
				Image halfImg = new Image((img.Width+1)/2, (img.Height+1)/2);
				for(int r = 0; r < halfImg.Height; r++)
				{
					for(int c = 0; c < halfImg.Width; c++)
					{
						PixelType val = img.GetPixel(2*c, 2*r);
						int n = 1;
						if(2*c+1 < img.Width)
						{
							val += img.GetPixel(2*c+1, 2*r);
							n++;
						}
						if(2*r+1 < img.Height)
						{
							val += img.GetPixel(2*c, 2*r+1);
							n++;
						}
						if(2*c+1 < img.Width && 2*r+1 < img.Height)
						{
							val += img.GetPixel(2*c+1, 2*r+1);
							n++;
						}
						halfImg.SetPixel(c, r, val/n);
					}
				}
				img = halfImg;
				rect = new Rectangle(rect.Left/2, rect.Top/2, (rect.Width+1)/2, (rect.Height+1)/2);
			}
			
			// Copy the image over using bilinear interpolation
			for(int r = 0; r < newHeight; r++)
			{
				for(int c = 0; c < newWidth; c++)
				{
					float imgC = rect.Left + (float)(c - internalWOffset) * rect.Width / internalWidth;
					float imgR = rect.Top + (float)(r - internalHOffset) * rect.Height / internalHeight;

					PixelType val = whitePixel;
					if(imgR >= 0 && imgC >= 0 && imgR <= img.Height-1 && imgC <= img.Width-1)
						val = (PixelType)Image.Bilinear(img, imgC, imgR);
					Debug.Assert(val >= 0 && val <= 1);
					newImg.SetPixel(c, r, val);
				}
			}

			return newImg;
		}

		// The centriod is the weighted (by value of the pixel) of the coordinate positions.
		public Vector2d ComputeCentroid(Image img)
		{
			float sumr = 0;
			float sumc = 0;
			float sum = 0;
			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					PixelType val = img.GetPixel(c, r);
					sumr += val*r;
					sumc += val*c;
					sum += val;
				}
			}
			return new Vector2d((float)sumc / sum, (float)sumr / sum);
		}

		public static float Distance(Vector2d a, Vector2d b)
		{
			return (float)Math.Sqrt((a.X-b.X)*(a.X-b.X) + (a.Y-b.Y)*(a.Y-b.Y));
		}

#if PERCY_CODE


        public string Encode()
        {
            return EncodeRLE();
        }

        public static Image Decode(string s)
        {
            string t = DecodeRLE(s).Encode();
            Debug.Assert(s.Equals(t));
            return DecodeRLE(s);
        }


        public string EncodeRLE()
        {
            StringBuilder sb = new StringBuilder(NumRows * NumCols + 10);

            // Dimensions
            sb.Append(NumCols);
            sb.Append(' ');
            sb.Append(NumRows);
            sb.Append(' ');

            // data is number of 0s, number of 1s, number of 0s, etc.
            byte[] data = new byte[NumCols * NumRows + 1];
            int data_i = 0;

            int curr_x = 0;
            int run = 0;
            for (int i = 0; i < NumCols * NumRows; i++)
            {
                int x = Pixels[i] > defaultThreshold ? 1 : 0;
                if (curr_x == x)
                {
                    if (run == 255)
                    {
                        data[data_i++] = (byte)run;
                        data[data_i++] = 0;
                        run = 0;
                    }
                    run++;
                }
                else
                {
                    data[data_i++] = (byte)run;
                    run = 1;
                    curr_x = 1 - curr_x;
                }
            }
            if (run > 0) data[data_i++] = (byte)run;

            sb.Append(Convert.ToBase64String(data, 0, data_i));
            return sb.ToString();
        }

        public static Image DecodeRLE(string s)
        {
            string[] tokens = s.Split(' ');
            if (tokens.Length != 3) throw new ArgumentException("Wrong format");
            int ncols = Int32.Parse(tokens[0]);
            int nrows = Int32.Parse(tokens[1]);
            byte[] data = Convert.FromBase64String(tokens[2]);

            Image img = new Image(ncols, nrows);
            int i = 0;
            int curr_x = 0;
            for (int data_i = 0; data_i < data.Length; data_i++) // For each run
            {
                for (int run = 0; run < data[data_i]; run++) // Fill in the pixels
                    img.Pixels[i++] = curr_x;
                curr_x = 1 - curr_x;
            }

            return img;
        }
#endif // PERCY_CODE
	}
}
