using System;
using System.Diagnostics;

namespace Dpu.ImageProcessing
{
    /// <summary>
    /// Integer valued image.  (Not clearly necessary,  but handy.)
    /// </summary>
	public class DiscreteImage
	{
		public DiscreteImage(int width, int height)
		{
			this.width = width;
			this.height = height;
			pixels = new int[height, width];
		}

		public DiscreteImage(int width, int height, int val)
		{
			this.width = width;
			this.height = height;
			pixels = new int[height, width];

			for(int r = 0; r < height; r++)
				for(int c = 0; c < width; c++)
					pixels[r, c] = val;
		}

		public int GetPixel(int c, int r)
		{
			return pixels[r, c];
		}

		public void SetPixel(int c, int r, int val)
		{
			pixels[r, c] = val;
		}

		public void Dump()
		{
			for(int r = 0; r < height; r++)
			{
				for(int c = 0; c < width; c++)
				{
					Debug.Write(String.Format("{0,3}", pixels[r, c]));
				}
				Debug.WriteLine("");
			}
		}

		public int Width { get { return width; } }
		public int Height { get { return height; } }

		int width, height; // TODO: how to get dimensions directly from pixels
		int[,] pixels;
	}
}
