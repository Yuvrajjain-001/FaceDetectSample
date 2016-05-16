using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.IO;


using System.Diagnostics;

using System.Windows.Ink.Analysis.MathLibrary;

using Dpu.Utility;


namespace Dpu.ImageProcessing
{
    using DpuImage = Dpu.ImageProcessing.Image;
    using DpuImageAlg = Dpu.ImageProcessing.Algorithms;
    using GdiImage = System.Drawing.Image;

    using PixelType = System.Single;


	public class ImageClassify
	{

        /// <summary>
        /// Produce a reference image for processing.  This image is scaled down to a reasonable size,  
        /// so that data handling is a little more manageable.
        /// </summary>
        /// <param name="example"></param>
        /// <returns></returns>
		/**/ 
        public static ImageStack ProduceReferenceImage (ImageStack image, int longSide, int shortSide)
		{
			Console.WriteLine("Read image of size {0} x {1}", image.Width, image.Height);

            int targetWidth  = longSide;
            int targetHeight = shortSide;
            
            if (image.Width > image.Height) 
            {
                targetWidth  = longSide;
                targetHeight = shortSide;
            }
            else
            {
                targetWidth  = shortSide;
                targetHeight = longSide;
            }
            
            int finalWidth;
            int finalHeight;

            float imageAspect = image.Width / (float)image.Height;
            float resAspect = targetWidth / (float)targetHeight;

            if (imageAspect < resAspect)
            {
                // If input is taller than result
                finalHeight = targetHeight;
                finalWidth = (int)Math.Floor(targetHeight * imageAspect);
            }
            else 
            {
                // If input is wider than result
                finalWidth  = targetWidth;
                finalHeight = (int)Math.Floor(targetWidth / imageAspect);
            }

			ImageStack scaledStack = new ImageStack(new Size(finalWidth, finalHeight), 3);
    		ImageStack.ReduceSize(image, scaledStack);

            return scaledStack;
		}
        /// <summary>
        /// Display a list of currently available image encoders.
        /// </summary>
		[Action]
		public static void ImageEncoderList ()
		{
			ImageCodecInfo[] infoList = ImageCodecInfo.GetImageEncoders();

			Console.Write("Encoders: ");

			for(int n = 0; n < infoList.Length; ++n) 
			{
				Console.Write(" " + infoList[n].FilenameExtension);
				Console.Write(" " + infoList[n].MimeType);
			}
			Console.WriteLine("");

			infoList = ImageCodecInfo.GetImageDecoders();

			Console.Write("Decoders: ");

			for(int n = 0; n < infoList.Length; ++n) 
			{
				Console.Write(" " + infoList[n].FilenameExtension);
				Console.Write(" " + infoList[n].MimeType);
			}
			Console.WriteLine("");
		}

        public static ImageStack BlurImage(ImageStack rgb, double BlurScale, double BlurWidth, bool BlurNormalize)
        {

            DpuImage blur = new DpuImage(9, 9);
            DpuImage.Blur_Kernel(blur, (float)BlurScale, (float)BlurWidth, (float)BlurWidth, BlurNormalize);

            ImageStack res = new ImageStack(new Size(rgb.Width - 8, rgb.Height - 8), 3);

            ImageStack.Convolve(rgb, blur, res);

            return res;
        }

        public static ImageStack HarrisImage(ImageStack rgb)
        {

            DpuImage grey = new DpuImage(rgb.Width, rgb.Height);
            DpuImage corner = new DpuImage(rgb.Width, rgb.Height);

            rgb.ComputeGrey(grey);

            DpuImage.FastHarris(grey, corner);

            DpuImage.PixelAdd(corner, 1.0f, corner);

            DpuImage.Log(corner, corner);

            DpuImage.PixelMultiply(corner, 10.0f, corner);

            ImageStack res = new ImageStack();

            res.Add(corner);
            res.Add(corner);
            res.Add(corner);

            return res;
        }

		public static Bitmap ImageBitmap (DpuImage image) 
		{
			DpuImage.Stretch(image, 0.0f, 255.0f, image);
			return DpuImage.ToBitmap(image, image, image);
		}

        public static unsafe object[] SiftImage(ImageStack rgb, int SiftLevels, double SiftOctaves, int BlurSteps)
        {
            ArrayList boxList = new ArrayList();
            Hashtable imageHash = new Hashtable();
            DpuImage grey = new DpuImage(rgb.Width, rgb.Height);
            DpuImage sift = new DpuImage(rgb.Width, rgb.Height);

            float sum = 0;
            rgb.ComputeGrey(grey);
            for (int n = 0; n < SiftLevels; ++n)
            {
                sum += SiftLevel(n, grey, sift, SiftOctaves, BlurSteps, boxList, imageHash);
                DpuImage next = new DpuImage(grey.Width / 2, grey.Height / 2);
                Dpu.ImageProcessing.Algorithms.ReduceHalf(grey, next);
                grey = next;
            }
            Console.WriteLine("Found a total of {0} sift points", sum);
            return ListUtil.Create(boxList, imageHash);
        }
        
        public static unsafe void ComputePyramid(
            DpuImage grey, 
            double SiftOctaves, int BlurSteps, 
            ArrayList dogList, ArrayList gaussList
            )
        {
            int ksize = 7;
            DpuImage blur = new DpuImage(ksize, ksize);

            int isize = 11;
			DpuImage blurTrue = new DpuImage(isize, isize);
			DpuImage impulse = new DpuImage(isize,isize);
            DpuImage impulseRes = new DpuImage(isize,isize);

            DpuImage.Blur_Kernel(blur, 1.0f, 0.5f, 0.5f, true);

            DpuImage initial = new DpuImage(grey.Width, grey.Height);
            DpuImage.ConvolutionReflecting(grey, 1, 1, blur, initial);

            grey = initial;
            
            impulse.SetPixel(isize/2, isize/2, 1.0f);


            float scaleStep = (float) Math.Pow(SiftOctaves, 1.0 / (BlurSteps - 2.0));
            float currentVariance = 1.0f;

            for (int n = 0; n < BlurSteps; ++n) 
            {
                float targetScale = (float) (Math.Pow(scaleStep, n+1));
                float targetVariance = (float) targetScale * targetScale;

                float deltaVariance = targetVariance - currentVariance;
                float stdev = (float) Math.Sqrt(deltaVariance);
				
                Console.WriteLine("To achieve {0} Bluring by {1}", targetScale, stdev);
                DpuImage.Blur_Kernel(blur, 1.0f, deltaVariance, deltaVariance , true);
				
                float trueScale = targetVariance - 1.0f;

                DpuImage.Blur_Kernel(blurTrue, 1.0f, trueScale, trueScale, true);

                DpuImage.ConvolutionReflecting(impulse, 1, 1, blur, impulseRes);
                DpuImage.Copy(impulseRes, impulse);
			    
                Console.WriteLine("Blur = {0}", stdev);
                DpuImage.Print(blur, Console.Out);
                Console.WriteLine("BlurTrue scale = {0} sum = {1}", trueScale, DpuImage.PixelSum(blurTrue));
                DpuImage.Print(blurTrue, Console.Out);
                Console.WriteLine("Impulse (sum = {0})", DpuImage.PixelSum(impulse));
                DpuImage.Print(impulse, Console.Out);

                DpuImage next = new DpuImage(grey.Width, grey.Height);
                DpuImage dog = new DpuImage(grey.Width, grey.Height);
                DpuImage.ConvolutionReflecting(grey, 1, 1, blur, next);
                DpuImage.Subtract(grey, next, grey);
                dogList.Add(grey);
                Console.WriteLine("Dog [{0}, {1}]", DpuImage.PixelMin(grey), DpuImage.PixelMax(grey));
                // gaussList.Add(grey);
                grey = next;
				
                currentVariance += deltaVariance;
            }
        }

        public static unsafe float SiftLevel (
            int level, DpuImage grey, DpuImage res,
            double SiftOctaves, int BlurSteps,
            ArrayList boxList, Hashtable imageHash
            )
		{

            float siftSum = 0;

            ArrayList dogList = new ArrayList();
            ArrayList gaussList = new ArrayList();

            float scaleStep = (float) Math.Pow(SiftOctaves, 1.0 / (BlurSteps - 2.0));
            ComputePyramid(grey, SiftOctaves, BlurSteps, dogList, gaussList);
            
			for(int i = 1; i < dogList.Count - 1; ++i) 
			{
				DpuImage cur    = ((DpuImage) dogList[i]);
				DpuImage before = ((DpuImage) dogList[i-1]);
				DpuImage after  = ((DpuImage) dogList[i+1]);
                DpuImage siftp = new DpuImage(grey.Width, grey.Height);
                DpuImage siftn = new DpuImage(grey.Width, grey.Height);

                DpuImage.Init(siftp, 1.0f);
                DpuImage.Init(siftn, 1.0f);

                fixed(PixelType *ptsiftp = siftp.Pixels) 
                {
                    siftp.SetPixelData(ptsiftp);

                    fixed(PixelType *ptsiftn = siftn.Pixels) 
                    {
                        siftn.SetPixelData(ptsiftn);
						
                        fixed(PixelType *ptcur = cur.Pixels) 
                        {
                            cur.SetPixelData(ptcur);

                            fixed(PixelType *ptbefore = before.Pixels) 
                            {
                                before.SetPixelData(ptbefore);
								
                                fixed(PixelType *ptafter = after.Pixels) 
                                {
                                    after.SetPixelData(ptafter);

                                    FindLocalSiftMax(cur, before, after, 1.0f, siftp);
                                    FindLocalSiftMax(cur, before, after, -1.0f, siftn);
                                }
                            }
                        }
                        double size = 4*Math.Pow(scaleStep,i-1);
						string name = String.Format("Sift{0}", level);
                        boxList.AddRange(CreateBoxes(name, level, size, siftp, res, Color.Blue));
                        boxList.AddRange(CreateBoxes(name, level, size, siftn, res, Color.Red));
					}
                }
                
                string imageName;
                imageName = String.Format("SiftP{0}{1}", level, i);
                float sump = DpuImage.PixelSum(siftp);
                Console.WriteLine("{0} = {1}", imageName, sump);
                DpuImage.Stretch(siftp, 0.0f, 255.0f, siftp);
                imageHash[imageName] = DpuImage.ToBitmap(siftp, siftp, siftp);

                imageName = String.Format("SiftN{0}{1}", level, i);
                float sumn = DpuImage.PixelSum(siftn);
                Console.WriteLine("{0} = {1}", imageName, sumn);
                DpuImage.Stretch(siftn, 0.0f, 255.0f, siftn);
                imageHash[imageName] = DpuImage.ToBitmap(siftn, siftn, siftn);

                siftSum = siftSum + sump + sumn;
            }

            Console.WriteLine("Found a total of {0} sift points", siftSum);


			for(int n = 0; n < BlurSteps; ++n) 
			{
				DpuImage   dog = (DpuImage) dogList[n];
				DpuImage.Stretch(dog, 0.0f, 255.0f, dog);

				string dogName = String.Format("Dog{0}{1}", level,n);
                imageHash[dogName] = DpuImage.ToBitmap(dog, dog, dog);
                // DpuImage   gauss = (DpuImage) gaussList[n];
                // DpuImage.Stretch(gauss, 0.0f, 255.0f, gauss);
                // string gaussName = String.Format("Gauss{0}{1}", level,n);
                // CurrentLabeledImage.GetView(gaussName).Bitmap = DpuImage.ToBitmap(gauss, gauss, gauss);
            }

            return siftSum;
		}



        public static ArrayList CreateBoxes(string name, int level, double size, DpuImage pos, DpuImage res, Color color) 
        {
            ArrayList boxList = new ArrayList();
            int scale = (int) Math.Pow(2, level);
            float boxSize = (float) (size * scale);
            float boxDelta = boxSize / 2;

            for(int r = 0; r < pos.Height; ++r) 
            {
				float row = ((r / (float) pos.Height) * res.Height) - boxDelta;
				for(int c = 0; c < pos.Width; ++c) 
                {            
                    bool isPos = Math.Abs(pos.GetPixelFast(c, r) - 1.0f) < 0.01f;
                    if (isPos) 
                    {
						float col = ((c / (float) pos.Width) * res.Width) - boxDelta;
                        boxList.Add(new LabeledColorBox(Rectangle2d.FromXYWH(col, row, boxSize, boxSize), color));
                    }
                }
            }
            return boxList;
        }
        /// <summary>
        /// A sift max (from Lowe) is a maximum both in the current image as well are the images that smaller and larger in scale.
        /// </summary>
        public static void FindLocalSiftMax(DpuImage curr, DpuImage before, DpuImage after, float polarity, DpuImage res) 
		{
			DpuImageAlg.FindLocalMax(curr, polarity,                  1.0f, curr,   res);
			DpuImageAlg.FindLocalMax(curr, polarity,     (float) SiftFudge, before, res);
			DpuImageAlg.FindLocalMax(curr, polarity, (float) (1/SiftFudge), after,  res);
		}

        public static double SiftFudge = 1.0;
        public static double SiftOffset = 0.0;

 
        [Action]
        public static DpuImage LocalMax(ImageStack rgb, double MaxThreshold)
        {

            DpuImage grey = new DpuImage(rgb.Width, rgb.Height);
            DpuImage dog = new DpuImage(rgb.Width, rgb.Height);
            DpuImage corner = new DpuImage(rgb.Width, rgb.Height);

            rgb.ComputeGrey(grey);

            DpuImage.Dog(grey, dog);

            DpuImage.Abs(dog, dog);

            DpuImage.LocalMax(dog, (float)MaxThreshold, corner);

            return corner;
        }
        [Action]
        public static ImageStack BlurImageReflecting(ImageStack rgb, double BlurScale, double BlurWidth, bool BlurNormalize)
        {
            DpuImage blur = new DpuImage(9, 9);
            DpuImage.Blur_Kernel(blur, (float)BlurScale, (float)BlurWidth, (float)BlurWidth, BlurNormalize);

            ImageStack res = new ImageStack(new Size(rgb.Width, rgb.Height), 3);

            ImageStack.ConvolveReflecting(rgb, 1, 1, blur, res);

            return res;
        } 
    }
}