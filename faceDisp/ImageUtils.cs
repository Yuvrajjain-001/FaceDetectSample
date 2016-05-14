using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using GenPositionData;
using Dpu.ImageProcessing;

namespace FaceDisp
{
    /// <summary>
    /// Wrapper class between image processing library routines
    /// and the faceSortUI. The idea is to not intangle too deeply other libraries
    /// with the main body of faceSortUI
    /// </summary>
    public class ImageUtils
    {

        /// <summary>
        /// Extract a normalized face from an input image. Normalization is done
        /// by ensuring that the eye positions in the original image
        /// map to the specified position in the face image. The input
        /// inage is assumed to be 1 byte per channel. Steps involved
        /// 1. Construct an affine mapping from Destination to original eye location
        /// 2. Fill in the destination image using the mapping
        /// </summary>
        /// <param name="origImage">Input image as a byte array</param>
        /// <param name="origRect">Size of the original image</param>
        /// <param name="origLeftEye">Left eye location in source image</param>
        /// <param name="origRightEye">Right eye location in source</param>
        /// <param name="bytePerPix"># bytes per Pixel in original image.Since we assume 1 byte per channel this is same as # channels Face is constructed with same</param>
        /// <param name="faceRect">Desired face size</param>
        /// <param name="faceLeftEye">Desired left eye location in face</param>
        /// <param name="faceRightEye">Desired right eye loc in face</param>
        /// <returns>Byte array of extracted face</returns>
        static public byte[] ExtractNormalizeFace(byte[] origImage, Rect origRect, int bytePerPix, int origStride,
            Rect mapFrom, Rect faceRect, int faceStride, TransformSample txfm)
        {
            // Sanity check eye location
            double[,] affineMat = new double[2, 3];
            double scaleX = mapFrom.Width / faceRect.Width;
            double scaleY = mapFrom.Height / faceRect.Height;
            double theta = txfm.ThetaRad;

            affineMat[0, 0] = scaleX * Math.Cos(theta);
            affineMat[0, 1] = -scaleY * Math.Sin(theta);
            affineMat[0, 2] = txfm.Xpix;
            affineMat[1, 0] = scaleX * Math.Sin(theta);
            affineMat[1, 1] = scaleY * Math.Cos(theta);
            affineMat[1, 2] = txfm.Ypix;

            return DoAffine(origImage, origRect, bytePerPix, origStride, mapFrom, faceRect, faceStride, affineMat);
        }

        static public byte[]  DoAffine(byte[] origImage, Rect origRect, int bytePerPix, int origStride,
            Rect mapFrom, Rect faceRect, int faceStride, double[,] affineMat)
        {
            // Use the Image library routines. These operate 1 channel at a time
            Image[] outImage = new Image[bytePerPix];
            double srcCentreX = mapFrom.X + mapFrom.Width / 2.0;
            double srcCentreY = mapFrom.Y + mapFrom.Height / 2.0;

            for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
            {
                Image inChannel = new Image(origImage, iChannel, (int)origRect.Width, (int)origRect.Height, bytePerPix, origStride);
                outImage[iChannel] = new Image((int)faceRect.Width, (int)faceRect.Height);

                //Image.AffineTransPad(inChannel, outImage[iChannel], affineMat);
                Image.AffineTransPadProvideCenter(inChannel, srcCentreX, srcCentreY, outImage[iChannel], affineMat);
            }

            byte[] faceBuffer = new byte[faceStride * (int)faceRect.Height];

            for (int row = 0; row < faceRect.Height; ++row)
            {
                int rowOffSet = row * faceStride;

                for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
                {
                    int iPix = row * (int)faceRect.Width;
                    int col = rowOffSet + iChannel;

                    for (int iCol = 0; iCol < faceRect.Width; ++iCol, col += bytePerPix)
                    {
                        faceBuffer[col] = (byte)Math.Min(Byte.MaxValue, outImage[iChannel].Pixels[iPix++]);
                    }
                }
            }

            return faceBuffer;
        }

    }
}
