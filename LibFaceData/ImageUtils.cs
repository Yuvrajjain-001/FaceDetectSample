using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Dpu.ImageProcessing;
using ShoNS.Array;

namespace LibFaceData
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
        /// <returns>Images array representing the colour plane of extracted face</returns>
        static public Image[] ExtractNormalizeFace(Image[] origImage, Rect origRect, Point origLeftEye, Point origRightEye, int bytePerPix,
                    Rect faceRect, Point faceLeftEye, Point faceRightEye)
        {
            // Sanity check eye location
            if (false == origRect.Contains(origLeftEye) || false == origRect.Contains(origRightEye))
            {
                return null;
            }

            // Step 1 - Construct the affine transformation
            // Find mapping between orig and desired EyePosAsMatrix locations + a 
            // fake point located at right angles to the vector joing the two eyes
            //INumArray <float> origMat = ArrFactory.FloatArray(3,2);
            //INumArray <float> faceMat = ArrFactory.FloatArray(3,3);
            FloatArray origMat = new FloatArray(3, 2);
            FloatArray faceMat = new FloatArray(3, 3);

            EyePosAsMatrix(origRect, origLeftEye, origRightEye, ref origMat);
            EyePosAsMatrix(faceRect, faceLeftEye, faceRightEye, ref faceMat);
            faceMat[0,2] = 1.0F;
            faceMat[1, 2] = 1.0F;
            faceMat[2, 2] = 1.0F;
            SVDFloat svd = new SVDFloat(faceMat);
            //INumArray <float> sss = svd.Solve(origMat);
            //INumArray <float> mmm = (INumArray <float>)sss.Transpose();
            FloatArray sss = svd.Solve(origMat);
            FloatArray mmm = sss.Transpose();
            //double[,] affineMat = new DoubleArray(mmm).ToArray();
            DoubleArray da = DoubleArray.From(mmm);
            double[,] affineMat = ConvertToDoubleArray(da);

            return TransformImage(origImage, origRect, faceRect, affineMat, bytePerPix);

        }

        static public double [,] ConvertToDoubleArray(DoubleArray da)
        {
            double[,] ret = new double[da.size0, da.size1];
            for (int i = 0; i < da.size0; ++i)
            {
                for (int j = 0; j < da.size1; ++j)
                {
                    ret[i, j] = da[i, j];
                }
            }

            return ret;
        }
        /// <summary>
        /// Scale a full image to fit into a destination rect
        /// </summary>
        /// <param name="origImage">Input image as a byte array</param>
        /// <param name="origRect">Size of the original image</param>
        /// <param name="destRect">Size of destination image (sets the scaling parameters</param>
        /// <param name="bytePerPix"># bytes per Pixel in original image.Since we assume 1 byte per channel this is same as # channels Face is constructed with same</param>
        /// <returns>Images array representing the colour plane of scaled image</returns>
        static public Image[] ScaleImage(Image[] origImage, Rect origRect, Rect destRect, int bytePerPix)
        {
            if (origImage[0].Width != origRect.Width || origImage[0].Height != origRect.Height)
            {
                return null;
            }

            double scaleX = origRect.Width / destRect.Width;
            double scaleY = origRect.Height / destRect.Height;
            double[,] affineMat = new double[2, 3];

            affineMat[0, 0] = origRect.Width / destRect.Width;
            affineMat[1, 1] = origRect.Height / destRect.Height;
            affineMat[0, 2] = origRect.X;
            affineMat[1, 2] = origRect.Y;

            return TransformImage(origImage, origRect, destRect, affineMat, bytePerPix);

        }
        /// <summary>
        /// Perform an affine transform of an image buffer
        /// </summary>
        /// <param name="origImage">Input image buffer</param>
        /// <param name="affine">Transformation matrix</param>
        /// <param name="bytePerPix"># bytes per Pixel in original image.Since we assume 1 byte per channel this is same as # channels Face is constructed with same</param>
        /// <returns>Images array representing the colour plane</returns>
        static Image[] TransformImage(Image[] srcImage, Rect srcRect, Rect destRect, double[,] affineMat, int bytePerPix)
        {
            // Use the Image library routines. These operate 1 channel at a time
            Image [] outImage = new Image[bytePerPix];
            for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
            {
                outImage[iChannel] = new Image((int)destRect.Width, (int)destRect.Height);
                Image.AffineTransPad(srcImage[iChannel], outImage[iChannel], affineMat);
            }

            return outImage;
        }

        /// <summary>
        /// Convert array of Image colour plane representations int
        /// a single byte array
        /// </summary>
        /// <param name="srcImage"></param>
        /// <returns>Single Byte array  representation of image</returns>
        static public byte[] ConvertImageArrayToByteArray(Image[] srcImage)
        {
            int facePix = srcImage[0].Width * srcImage[0].Height;
            int bytePerPix = srcImage.Length;
            byte[] faceBuffer = new byte[facePix * bytePerPix];

            for (int iPix = 0; iPix < facePix; ++iPix)
            {
                int iOff = iPix * bytePerPix;
                for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
                {
                    faceBuffer[iOff + iChannel] = (byte)Math.Min(Byte.MaxValue, srcImage[iChannel].Pixels[iPix]);
                }

            }

            return faceBuffer;

        }
        /// <summary>
        /// Create an array of Images from a bsingle byte array of an image
        /// </summary>
        /// <param name="srcPixs">Source input buffer</param>
        /// <param name="srcRect">dimensions of input image</param>
        /// <param name="bytePerPix"># bytes per Pixel in original image.Since we assume 1 byte per channel this is same as # channels Face is constructed with same</param>
        /// <returns>Array of images constructed</returns>
        static public Image[] ConvertByteArrayToImageArray(byte[] srcPixs, Rect srcRect, int bytePerPix)
        {
            Image[] retImages = new Image[bytePerPix];

            for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
            {
                retImages[iChannel] = new Image(srcPixs, iChannel, (int)srcRect.Width, (int)srcRect.Height, bytePerPix);
            }

            return retImages;
        }
        /// <summary>
        /// Fill in a 2x2 matrix representation of eye positions with
        /// respect centre of the image. Beside the two eye positions  trick is used
        /// to create a third point which is a right angles to teh vector left -> right eye
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="leftEye">Absolute position of left eye</param>
        /// <param name="rightEye">Absolute pos of right eye</param>
        /// <param name="mat">Filled in 2x2 matrix</param>
        //static public void EyePosAsMatrix(Rect imageRect, Point leftEye, Point rightEye, ref INumArray<float> mat)
        static public void EyePosAsMatrix(Rect imageRect, Point leftEye, Point rightEye, ref FloatArray mat)
        {
            double cx = imageRect.Width / 2.0;
            double cy = imageRect.Height / 2.0;

            mat[0, 0] = (float)(leftEye.X - cx);
            mat[0, 1] = (float)(leftEye.Y - cy);

            mat[1, 0] = (float)(rightEye.X - cx);
            mat[1, 1] = (float)(rightEye.Y - cy);

            float dx = mat[1, 0] - mat[0, 0];
            float dy = mat[1, 1] - mat[0, 1];

            // Trick.  You need to specify 3 points in correspondance to
            // determine a GLR matrix.  This is a *fake* point which is lies in a RIGHT TRIAGLE
            // with the left and right eye.  You do the same thing with both the source and
            // target points.
            mat[2, 0] = mat[0, 0] - dy;
            mat[2, 1] = mat[0, 1] + dx;
        }
    }
}
