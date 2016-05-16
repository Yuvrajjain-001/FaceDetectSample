using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Image = Dpu.ImageProcessing.Image;

namespace Dpu.ImageProcessing
{
    public class ImageIO
    {
        /// <summary>
        /// Read an image into a Dpu Image
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        unsafe static public Image readDpuImage(string FileName)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(FileName);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            //BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            //Dpu.ImageProcessing.Image dpuIm = new Dpu.ImageProcessing.Image(bmpData, Dpu.ImageProcessing.Image.ColorPlane.red);

            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            Dpu.ImageProcessing.Image dpuIm = new Dpu.ImageProcessing.Image(bmpData.Width, bmpData.Height);

            int i, j;
            float r, g, b;
            for (i = 0; i < bmpData.Height; i++)
            {
                Byte* row = (Byte*)(bmpData.Scan0) + i * bmpData.Stride;

                for (j = 0; j < bmpData.Width; j++)
                {
                    r = (float)row[0];
                    g = (float)row[1];
                    b = (float)row[2];

                    dpuIm.SetPixel(j, i, 0.3f * r + 0.59f * g + 0.11f * b);
                    row = row + 3;
                }
            }

            bmp.UnlockBits(bmpData);
            bmp.Dispose();

            return dpuIm;
        }
        /// <summary>
        /// Write a dpu image
        /// </summary>
        /// <param name="dpuIm"></param>
        /// <param name="FileName"></param>
        static public void writeDpuImage(Dpu.ImageProcessing.Image dpuIm, string FileName)
        {
            System.Drawing.Bitmap bmp = Dpu.ImageProcessing.Image.ToBitmap(dpuIm, dpuIm, dpuIm);
            bmp.Save(FileName);
            bmp.Dispose();
        }
    }
}
