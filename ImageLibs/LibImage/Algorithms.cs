// Algorithms.cs
//
// cscargs:  /unsafe /target:library /debug /r:c:/MSR/LiveCode/private/research/private/CollaborativeLibs_01/Sho/Bin/LibUtility.dll /r:c:/MSR/LiveCode/private/research/private/CollaborativeLibs_01/Sho/Bin/LibMath.dll /r:c:/MSR/LiveCode/private/research/private/CollaborativeLibs_01/Sho/Bin/LibImage.dll

using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.Collections;

using Dpu.Utility;

using PixelType = System.Single;

namespace Dpu.ImageProcessing
{
    public unsafe class Algorithms
    {
        /// <summary>
        /// Compute new image which is half the size of original ///////////////////////////////////
        /// </summary>
        /// <param name="fromImage"></param>
        /// <param name="toImage"></param>
        public static void         ReduceHalf(Image one, Image res)
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptres = res.Pixels)
                {
                    res.SetPixelData(ptres);

                    _ReduceHalfInternal(one, res);
                }
            }
        }

        private static void        _ReduceHalfInternal(Image one, Image res)
        {
            int ncol_new = res.Width;
            int nrow_new = res.Height;

            // This is the subset of indices which are easily computed
            int ncol_simp = one.Width / 2;
            int nrow_simp = one.Height / 2;

            /*
                cerr << "Half_Size " << s_tmp << " " << m_size << " new " << size_new << 
                " simp " << Size(ncol_simp, nrow_simp) << endl;
                */

            // Each pixel in the downsample is the sum of 4 in the orig
            for (int nrow = 0; nrow < nrow_simp; ++nrow)
            {
                PixelType* prow = res.Row_Start(nrow);
                PixelType* prow_end = prow + (res.Step_Col() * ncol_simp);
                PixelType* prow_orig = one.Row_Start(2 * nrow);
                while (prow < prow_end)
                {
                    PixelType sum = ((*prow_orig)
                        + (*(prow_orig + one.Step_Col()))
                        + (*(prow_orig + one.Step_Row()))
                        + (*(prow_orig + one.Step_Row() + one.Step_Col())));

                    *prow = sum / 4;
                    prow += res.Step_Col();
                    prow_orig += 2 * one.Step_Col();
                }
            }

            // THe boundaries are a little tricky 
            if (ncol_simp != ncol_new)
            {
                // for each row do the last column
                for (int nrow = 0; nrow < nrow_simp; ++nrow)
                {
                    PixelType* prow = res.Row_End(nrow) - res.Step_Col();
                    PixelType* prow_orig = one.Row_End(2 * nrow) - one.Step_Col();
                    // Sum is just current and one row down (we ran out of columns)
                    PixelType sum = ((*prow_orig) + (*(prow_orig - one.Step_Col()))
                        + (*(prow_orig + one.Step_Row())) + (*(prow_orig + one.Step_Row() - one.Step_Col())));
                    *prow = sum / 4;
                }
            }

            // THe boundaries are a little tricky
            if (nrow_simp != nrow_new)
            {
                // On the last row
                PixelType* prow = res.Row_Start(nrow_new - 1);
                PixelType* prow_end = res.Row_End(nrow_new - 1);
                PixelType* prow_orig = one.Row_Start(one.Height - 1);
                // Do each column
                while (prow < prow_end)
                {
                    PixelType sum = ((*prow_orig) + (*(prow_orig + one.Step_Col()))
                        + (*(prow_orig - one.Step_Row())) + (*((prow_orig - one.Step_Row()) + one.Step_Col())));
                    *prow = sum / 4;
                    prow += res.Step_Col();
                    prow_orig += 2 * one.Step_Col();
                }
            }

            // THe boundaries are a little tricky
            if ((nrow_simp != nrow_new) && (ncol_simp != ncol_new))
            {
                // Last pixel
                PixelType* prow = res.Row_End(nrow_simp) - res.Step_Col();
                PixelType* prow_orig = one.Row_End(one.Height - 1) - one.Step_Col();
                PixelType sum = ((*prow_orig) + (*(prow_orig - one.Step_Col()))
                    + (*(prow_orig - one.Step_Row())) + (*((prow_orig - one.Step_Row()) - one.Step_Col())));
                *prow = sum / 4;
            }
        }

        public static ArrayList    AllocatePyramidResult(IList referencePyr)
        {
            ArrayList res = new ArrayList();
            foreach(Image im in referencePyr)
            {
                res.Add(new Image(im.GetSize));
            }
            return res;
        }
        /// <summary>
        /// Allocate the images which will make up the pyramid
        /// </summary>
        public static ArrayList    AllocatePyramidResult(Image one, int minSize)
        {
            return AllocatePyramidResult(one.Width, one.Height, minSize);
        }
        /// <summary>
        /// Allocate the images which will make up the pyramid
        /// </summary>
        public static ArrayList    AllocatePyramidResult(int width, int height, int minSize)
        {
            ArrayList res = new ArrayList();

            res.Add(new Image(width, height));
            int newWidth = width;
            int newHeight = height;
            do
            {
                newWidth = (int)Math.Ceiling(newWidth / 2.0);
                newHeight = (int)Math.Ceiling(newHeight / 2.0);
                Image half = new Image(newWidth, newHeight);
                res.Add(half);
            }
            while (newWidth > minSize && newHeight > minSize);

            return res;
        }
        /// <summary>
        /// Compute the classical gaussian pyramid of quarter sized images.
        /// </summary>
        public static ArrayList    GaussianPyramid(Image one, int minSize)
        {
            ArrayList res = AllocatePyramidResult(one, minSize);
            ComputeGaussianPyramid(one, res);
            return res;
        }
        /// <summary>
        /// Compute the classical gaussian pyramid of quarter sized images.
        /// </summary>
        public static void         ComputeGaussianPyramid(Image one, IList res)
        {
            Image.Copy(one, (Image) res[0]);
            for (int i = 1; i < res.Count; ++i)
            {
                ReduceHalf((Image)res[i - 1], (Image)res[i]);
            }
        }
        /// <summary>
        /// Given two levels of a Gaussian pyramid, update the lower (hi-res) level to 
        /// contain only the residuals
        /// </summary>
        public static void         LaplaceLevel(Image one, double scale, Image half, Image res)
        {
            Debug.Assert(Image.Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* pthalf = half.Pixels)
                {
                    half.SetPixelData(pthalf);

                    fixed (PixelType* ptres = res.Pixels)
                    {
                        res.SetPixelData(ptres);

                        for (int nrow = 0; nrow < res.Height; ++nrow)
                        {
                            PixelType* prow = res.Row_Start(nrow);
                            PixelType* prow_end = res.Row_End(nrow);
                            PixelType* prow_one = one.Row_Start(nrow);
                            PixelType* prow_half_start = half.Row_Start(nrow / 2);
                            PixelType* prow_half = prow_half_start;
                            int res_col_step = res.Step_Col();
                            int one_col_step = one.Step_Col();
                            int half_col_step = half.Step_Col();
                            int offset = 0;
                            while (prow < prow_end)
                            {
                                // Predict that the high res is equal to the low res.
                                *prow = (PixelType)((*prow_one) + (scale * *prow_half));
                                prow += res_col_step;
                                prow_one += one_col_step;
                                offset += 1;
                                prow_half = prow_half_start + (offset / 2) * half_col_step;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Computes the Laplacian pyramid from the gaussian.  Note, can work *in place* and overwrite the gaussian.
        /// </summary>
        public static void         LaplacePyramidFromGaussian(IList gaussPyr, IList laplacePyr)
        {
            int i = 1;
            for (; i < gaussPyr.Count; ++i)
            {
                LaplaceLevel((Image)gaussPyr[i - 1], -1, (Image)gaussPyr[i], (Image)laplacePyr[i - 1]);
            }
            // Copy over the top
            Image.Copy((Image)gaussPyr[i - 1], (Image)laplacePyr[i - 1]);
        }
        /// <summary>
        /// Compute a laplacian pyramid from the image.
        /// </summary>
        public static ArrayList    LaplacePyramid(Image one, int minSize)
        {
            ArrayList res = AllocatePyramidResult(one, minSize);
            ComputeGaussianPyramid(one, res);
            LaplacePyramidFromGaussian(res, res);
            return res;
        }
        /// <summary>
        /// Reconstruct the gaussian pyramid from the laplacian pyramid. 
        /// </summary>
        public static void         ReconstructFromLaplacian(IList laplacePyr, IList gaussPyr)
        {
            int depth = laplacePyr.Count;
            // Copy in the lowest level
            Image.Copy(  (Image)laplacePyr[depth - 1], (Image)gaussPyr[depth - 1]);
            // predict each level from the last
            for (int i = depth - 1; i > 0; --i)
            {
                LaplaceLevel((Image)laplacePyr[i - 1], 1, (Image)gaussPyr[i], (Image)gaussPyr[i - 1]);
            }
        }
        /// <summary>
        /// Image derivative in the row direction.  An increasing ramp from top to bottom should yield positive values.
        /// </summary>
        public static void         RowDerivative(Image one, Image res)
        {
            Debug.Assert(Image.Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptres = res.Pixels)
                {
                    res.SetPixelData(ptres);

                    for (int nrow = 0; nrow < one.Height - 1; ++nrow) // can't compare to the row past the end
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        PixelType* prow_one_next = one.Row_Start(nrow + 1);
                        while (prow < prow_end)
                        {
                            *prow = *prow_one_next - *prow_one;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                            prow_one_next += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Image derivative in the col direction.  An increasing ramp from left to right should yield positive values.
        /// </summary>
        public static void         ColDerivative(Image one, Image res)
        {
            Debug.Assert(Image.Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptres = res.Pixels)
                {
                    res.SetPixelData(ptres);

                    for (int nrow = 0; nrow < one.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow) - res.Step_Col();  // stop before the end
                        PixelType* prow_one = one.Row_Start(nrow);
                        while (prow < prow_end)
                        {
                            *prow = *(prow_one + one.Step_Col()) - *prow_one;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Computes rectified pair from ONE.  if (one > 0) then pos = one else neg = -one
        /// </summary>
        public static void         RectifiedPair(Image one, Image pos, Image neg)
        {
            Debug.Assert(Image.Compatible(one, pos), "Images not compatible");
            Debug.Assert(Image.Compatible(one, neg), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptpos = pos.Pixels)
                {
                    pos.SetPixelData(ptpos);

                    fixed (PixelType* ptneg = neg.Pixels)
                    {
                        neg.SetPixelData(ptneg);

                        for (int nrow = 0; nrow < one.Height; ++nrow)
                        {
                            PixelType* prowOne = one.Row_Start(nrow);
                            PixelType* prowOneEnd = one.Row_End(nrow);
                            PixelType* prowPos = pos.Row_Start(nrow);
                            PixelType* prowNeg = neg.Row_Start(nrow);

                            while (prowOne < prowOneEnd)
                            {
                                PixelType val = *prowOne;
                                PixelType abs = Math.Abs(val);
                                // Clever trick (from Brown and Winder) that avoids a conditional.  
                                *prowPos = abs + val;  // 2X val if positive, else zero
                                *prowNeg = abs - val;  // -2X val if negative else zero
                                prowOne += one.Step_Col();
                                prowPos += pos.Step_Col();
                                prowNeg += neg.Step_Col();
                            }
                        }
                    }
                }
            }
        }

        public static int ExtractPatch(Image one, int c, int r, int w, int h, float[] res, int offset)
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                for (int j = r; j < r + h; ++j)
                {
                    for (int i = c; i < c + w; ++i)
                    {
                        PixelType val = one.GetPixelFast(i, j);
                        res[offset++] = val;
                    }
                }
                return offset;
            }
        }


        public static int ExtractPatchStep(Image one, int c, int r, int cStep, int rStep, int w, int h, float[] res, int offset)
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                for (int j = r; j < r + h; j += rStep)
                {
                    for (int i = c; i < c + w; i += cStep)
                    {
                        PixelType val = one.GetPixelFast(i, j);
                        res[offset++] = val;
                    }
                }
                return offset;
            }
        }

        public static int ExtractPatchStepKernel(Image one, Image kernel, int c, int r, int cStep, int rStep, int w, int h, float[] res, int offset)
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptkernel = kernel.Pixels)
                {
                    kernel.SetPixelData(ptkernel);



                    for (int j = r; j < r + h; j += rStep)
                    {
                        for (int i = c; i < c + w; i += cStep)
                        {
                            res[offset++] = (float) SpotExtractFast(one, kernel, c, r);
                        }
                    }
                    return offset;
                }
            }
        }
        


        public static Double SpotExtractFast(
            Image one, // Input image
            Image kernel,
            int col,
            int row
            )
        {
            int colOffset = kernel.Width / 2;
            int rowOffset = kernel.Height / 2;

            Double sum;

            int oneCol = col - colOffset;
            int oneRow = row - rowOffset;
            sum = 0;
            for (int kernelRow = 0; kernelRow < kernel.Height; ++kernelRow)
            {
                for (int kernelCol = 0; kernelCol < kernel.Width; ++kernelCol)
                {
                    sum += kernel.GetPixelFast(kernelCol, kernelRow)
                        * one.GetPixelReflect(oneCol + kernelCol, oneRow + kernelRow);
                }
            }
            return sum;
        }


        public static int          PixelCore(Image one, PixelType low, PixelType high, Image res)
        {
            int kept = 0;
            Debug.Assert(Image.Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptres = res.Pixels)
                {
                    res.SetPixelData(ptres);

                    for (int nrow = 0; nrow < one.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while (prow < prow_end)
                        {
                            if ((*prow_one < low) || (*prow_one > high))
                            {
                                *prow = *prow_one;
                                ++kept;
                            }
                            else
                                *prow = 0;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
            return kept;
        }

        public static object[]     MultiScaleHarrisImage(ImageStack rgb, int SiftLevels)
        {
            ArrayList boxList = new ArrayList();
            Hashtable imageHash = new Hashtable();
            int ksize = 7;
            Image blur = new Image(ksize, ksize);

            float variance = 1.5f * 1.5f;
            Image.Blur_Kernel(blur, 1.0f, variance, variance, true);
            Image original = new Image(rgb.Width, rgb.Height);

            float sum = 0;
            rgb.ComputeGrey(original);
            Image grey = original;

            for (int n = 0; n < SiftLevels; ++n)
            {
                Image blurred = new Image(grey.Width, grey.Height);
                Image harris = new Image(grey.Width, grey.Height);
                Image max = new Image(grey.Width, grey.Height);

                Image.ConvolutionReflecting(grey, 1, 1, blur, blurred);
                Image.FastHarris(blurred, harris);
                FindLocalMax(harris, 1.0f, 1.0f, harris, max);

                boxList.Add(new Dpu.Utility.Tuple(String.Format("Harris{0}", n), n, 4.0, max, original));


                imageHash[String.Format("Blur{0}", n)] = blurred;
                imageHash[String.Format("Harris{0}", n)] = harris;
                imageHash[String.Format("Max{0}", n)] = max;

                Image next = new Image(grey.Width / 2, grey.Height / 2);
                ReduceHalf(grey, next);
                grey = next;
            }
            Console.WriteLine("Found a total of {0} sift points", sum);

            return ListUtil.Create(boxList, imageHash);
        }

        public static double SiftFudge = 1.0;
        public static double SiftOffset = 0.0;
        /// <summary>
        /// Find the maximal points in image one.  Typically this is done by looking for pixels which are greater 
        /// than all neighbors.  In this case the image ONE, can be alternatively compared to image TWO.  This would find 
        /// those pixels in ONE that are greater than all neighboring pixels in TWO.
        /// </summary>
        /// <param name="one">The reference image.</param>
        /// <param name="polarity">Are we looking for local max (+1.0) or min (-1.0)</param>
        /// <param name="mult">A multiplier on the pixels of TWO.</param>
        /// <param name="two">Compare to image.</param>
        /// <param name="res">Result is an image in which local extrema are denoted by 1.0 (and 0.0 otherwise)</param>
        public static void         FindLocalMax(Image one, float polarity, float mult, Image two, Image res)
        {
            mult = mult * polarity;
            bool isSelf = (one == two);
            for (int r = 0; r < one.Height; ++r)
            {
                for (int c = 0; c < one.Width; ++c)
                {
                    if (r == 0 || r == one.Height - 1
                        || c == 0 || c == one.Width - 1)
                    {
                        res.SetPixelFast(c, r, 0.0f);
                    }
                    else
                    {
                        PixelType reference = polarity * one.GetPixelFast(c, r);
                        PixelType result = res.GetPixelFast(c, r);
                        bool isMax = Math.Abs(result - 1.0f) < 0.01f;
                        if (isMax)
                        {
                            for (int dr = r - 1; dr < r + 2 && isMax; ++dr)
                            {
                                for (int dc = c - 1; dc < c + 2 && isMax; ++dc)
                                {
                                    if (!(isSelf && r == dr && c == dc))
                                    {
                                        if (reference < SiftOffset)
                                        {
                                            res.SetPixelFast(c, r, 0.0f);
                                            isMax = false;
                                        }
                                        else if (reference < mult * two.GetPixelFast(dc, dr))
                                        {
                                            res.SetPixelFast(c, r, 0.0f);
                                            isMax = false;
                                        }
                                    }
                                }
                            }
                        }
                        if (isMax)
                        {
                            // Console.WriteLine("Max at {0} {1}", c, r);
                        }
                    }
                }
            }
        }

        public static void         ReduceSize(Image fromImage, Image toImage)
        {
            if (toImage.Width < fromImage.Width && toImage.Height < fromImage.Height)
            {
                ReduceSizeHelper(fromImage, toImage);
            }
            else
            {
                // Gee if the one axis is small and the other larger there is not much to be done.
                Image.BilinearResample(fromImage, toImage);
            }
        }
        /// <summary>
        /// resize to a given smaller scale.  To avoid aliasing and to increase speed,  done in factors of two;
        /// </summary>
        /// <param name="fromImage"></param>
        /// <param name="toImage"></param>
        private static void        ReduceSizeHelper(Image fromImage, Image toImage)
        {
            int colHalf = fromImage.Width / 2;
            int rowHalf = fromImage.Height / 2;
            if (colHalf < toImage.Width || rowHalf < toImage.Height)
            {
                // If the size_half is smaller than are final size,  then do not half size.  
                Image.BilinearResample(fromImage, toImage);
            }
            else
            {
                Image halfImage = new Image((int)colHalf, (int)rowHalf);
                ReduceHalf(fromImage, halfImage);
                ReduceSizeHelper(halfImage, toImage);
            }
        }
        /// <summary>
        /// Insert one into res at an offset.  Simply copy the pixels in.
        /// </summary>
        public static void         Insert(Image one, Image res, int colOffset, int rowOffset)
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one.SetPixelData(ptone);

                fixed (PixelType* ptres = res.Pixels)
                {
                    res.SetPixelData(ptres);

                    for (int nrow = 0; nrow < one.Height; ++nrow)
                    {
                        PixelType* prow_res = res.Row_Start(nrow + rowOffset) + colOffset * res.Step_Col();
                        PixelType* prow_one = one.Row_Start(nrow);
                        PixelType* prow_one_end = one.Row_End(nrow);
                        while (prow_one < prow_one_end)
                        {
                            *prow_res = *prow_one;
                            prow_res += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
    }
}
