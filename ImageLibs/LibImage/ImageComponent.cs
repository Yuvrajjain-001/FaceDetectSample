using System;
using System.Collections;
using System.Drawing;
//using System.Windows.Ink.Analysis.MathLibrary;
using System.Windows.Ink.Analysis.MathLibrary;

namespace Dpu.ImageProcessing
{
	using PixelType = System.Single;

	/// <summary>
	/// An ImageComponent is a subset of pixels of an image.
	/// Often we will use connected components.
	/// </summary>
	public class ImageComponent
	{
		public ImageComponent(Image img, DiscreteImage iimg, int componentId, Rectangle boundingBox)
		{
			this.img = img;
			this.iimg = iimg;
			this.componentId = componentId;
			this.boundingBox = boundingBox;
			this.centroid = ComputeCentroid();
		}

		public Image img;
		public DiscreteImage iimg;
		public int componentId;
		public Rectangle boundingBox; // the bounding box of the image component
		public Vector2d centroid;

		/// <summary>
		/// 
		/// </summary>
		/// <returns>The array of ImageComponents found in the image.</returns>
		public static ArrayList FindConnectedComponents(Image img, PixelType threshold, bool diagIsConnected)
		{
			DiscreteImage iimg = new DiscreteImage(img.Width, img.Height, -1);
			ArrayList components = new ArrayList();
			int componentId = 0;

			for(int r = 0; r < img.Height; r++)
			{
				for(int c = 0; c < img.Width; c++)
				{
					if(UnExplored(img, iimg, c, r, threshold))
					{
						componentId++;
						int minr = img.Height, maxr = 0;
						int minc = img.Width, maxc = 0;
						FloodFill(img, iimg, c, r, componentId, ref minc, ref minr, ref maxc, ref maxr,
							threshold, diagIsConnected);

						Rectangle boundingBox = new Rectangle(minc, minr, maxc-minc+1, maxr-minr+1);
						components.Add(new ImageComponent(img, iimg, componentId, boundingBox));
					}
				}
			}

			return components;
		}

		/// <returns>Whether (c, r) contains a black pixel that has not been assigned a component yet.</returns>
		static bool UnExplored(Image img, DiscreteImage iimg, int c, int r, PixelType threshold)
		{
			if(r < 0 || c < 0 || r >= img.Height || c >= img.Width) return false;
			return img.GetPixel(c, r) > threshold && iimg.GetPixel(c, r) == -1;
		}


        /// <returns>Whether (c, r) contains a black pixel that has not been assigned a component yet.</returns>
        static bool UnExploredColor(ImageStack istack, Color color, DiscreteImage iimg, int c, int r)
        {
            if (r < 0 || c < 0 || r >= istack.Height || c >= istack.Width) 
                return false;
            else 
                return
                (
                    (istack.GetColor(c, r).Equals(color))
                    && iimg.GetPixel(c, r) == -1
                 );
        }

        /// <summary>
        /// Fill up iimg with componentId starting at (c, r).
        /// Implementing own stack to prevent stack overflow.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="iimg"></param>
        /// <param name="c">Flood from this col</param>
        /// <param name="r">Flood from this row</param>
        /// <param name="componentId">id of this component</param>
        /// <param name="minc">resulting bounding box</param>
        /// <param name="minr">resulting bounding box</param>
        /// <param name="maxc">resulting bounding box</param>
        /// <param name="maxr">resulting bounding box</param>
        /// <param name="threshold">Maximum allowable threshold</param>
        /// <param name="diagIsConnected">Is 8 connected, or 4.</param>
        static void FloodFill(Image img, DiscreteImage iimg,
            int c, int r, int componentId,
            ref int minc, ref int minr, ref int maxc, ref int maxr,
            PixelType threshold, bool diagIsConnected
        )
        {
            // Funky avoidance of the call stack.  Instead we maintain our own stack.
            int nc = img.Width;
            int[] stack = new int[img.Height * img.Width];
            int nstack = 0;

            // Stack stores the next location to examine.  The row and column are encoded in a single int (neat).
            stack[nstack++] = r * nc + c;

            // Until we have fully explored the current CC.
            while (nstack > 0)
            {
                int x = stack[--nstack];
                r = x / nc;
                c = x % nc;

                iimg.SetPixel(c, r, componentId);

                // Update bounds
                minr = Math.Min(minr, r);
                minc = Math.Min(minc, c);
                maxr = Math.Max(maxr, r);
                maxc = Math.Max(maxc, c);

                // Flood W, E, N, S
                if (UnExplored(img, iimg, c - 1, r, threshold)) stack[nstack++] = r * nc + (c - 1);
                if (UnExplored(img, iimg, c + 1, r, threshold)) stack[nstack++] = r * nc + (c + 1);
                if (UnExplored(img, iimg, c, r - 1, threshold)) stack[nstack++] = (r - 1) * nc + c;
                if (UnExplored(img, iimg, c, r + 1, threshold)) stack[nstack++] = (r + 1) * nc + c;

                // Flood NW, NE, SW, SE
                if (diagIsConnected)
                {
                    if (UnExplored(img, iimg, c - 1, r - 1, threshold)) stack[nstack++] = (r - 1) * nc + (c - 1);
                    if (UnExplored(img, iimg, c + 1, r - 1, threshold)) stack[nstack++] = (r - 1) * nc + (c + 1);
                    if (UnExplored(img, iimg, c - 1, r + 1, threshold)) stack[nstack++] = (r + 1) * nc + (c - 1);
                    if (UnExplored(img, iimg, c + 1, r + 1, threshold)) stack[nstack++] = (r + 1) * nc + (c + 1);
                }
            }
        }

        /// <summary>
        /// Fill up iimg with componentId starting at (c, r).
        /// Implementing own stack to prevent stack overflow.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="iimg"></param>
        /// <param name="c">Flood from this col</param>
        /// <param name="r">Flood from this row</param>
        /// <param name="componentId">id of this component</param>
        /// <param name="minc">resulting bounding box</param>
        /// <param name="minr">resulting bounding box</param>
        /// <param name="maxc">resulting bounding box</param>
        /// <param name="maxr">resulting bounding box</param>
        /// <param name="threshold">Maximum allowable threshold</param>
        /// <param name="diagIsConnected">Is 8 connected, or 4.</param>
        static void FloodFillColor(
            ImageStack iStack,
            DiscreteImage iimg,
            int c, int r, int componentId,
            ref int minc, ref int minr, ref int maxc, ref int maxr,
            PixelType threshold, bool diagIsConnected
        )
        {
            Color color = iStack.GetColor(c, r);
            // Funky avoidance of the call stack.  Instead we maintain our own stack.
            int nc = iStack.Width;
            int[] stack = new int[iStack.Height * iStack.Width];
            int nstack = 0;

            // Stack stores the next location to examine.  The row and column are encoded in a single int (neat).
            stack[nstack++] = r * nc + c;

            // Until we have fully explored the current CC.
            while (nstack > 0)
            {
                int x = stack[--nstack];
                r = x / nc;
                c = x % nc;

                iimg.SetPixel(c, r, componentId);

                // Update bounds
                minr = Math.Min(minr, r);
                minc = Math.Min(minc, c);
                maxr = Math.Max(maxr, r);
                maxc = Math.Max(maxc, c);

                // Flood W, E, N, S
                if (UnExploredColor(iStack, color, iimg, c - 1, r)) stack[nstack++] = r * nc + (c - 1);
                if (UnExploredColor(iStack, color, iimg, c + 1, r)) stack[nstack++] = r * nc + (c + 1);
                if (UnExploredColor(iStack, color, iimg, c, r - 1)) stack[nstack++] = (r - 1) * nc + c;
                if (UnExploredColor(iStack, color, iimg, c, r + 1)) stack[nstack++] = (r + 1) * nc + c;

                // Flood NW, NE, SW, SE
                if (diagIsConnected)
                {
                    if (UnExploredColor(iStack, color, iimg, c - 1, r - 1)) stack[nstack++] = (r - 1) * nc + (c - 1);
                    if (UnExploredColor(iStack, color, iimg, c + 1, r - 1)) stack[nstack++] = (r - 1) * nc + (c + 1);
                    if (UnExploredColor(iStack, color, iimg, c - 1, r + 1)) stack[nstack++] = (r + 1) * nc + (c - 1);
                    if (UnExploredColor(iStack, color, iimg, c + 1, r + 1)) stack[nstack++] = (r + 1) * nc + (c + 1);
                }
            }
        }

		// The centriod is the weighted (by value of the pixel) of the coordinate positions.
		public Vector2d ComputeCentroid()
		{
			float sumr = 0;
			float sumc = 0;
			float sum = 0;
			for(int r = boundingBox.Top; r < boundingBox.Bottom; r++)
			{
				for(int c = boundingBox.Left; c < boundingBox.Right; c++)
				{
					if(iimg.GetPixel(c, r) != componentId) continue;
					PixelType val = img.GetPixel(c, r);
					sumr += val*r;
					sumc += val*c;
					sum += val;
				}
			}
			return new Vector2d((float)sumc / sum, (float)sumr / sum);
		}
	}
}
