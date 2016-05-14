using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using System.IO;

using DetectionManagedLib;


namespace Microsoft.LiveLabs
{
    public class Detect
    {

        private EyeDetect eyeDetect;            // Eye Detector classifier
        private FaceDetector faceDetector;      // Face Detector classifier
        private List<ScoredRect> faceDetectRects; // Detected faces in current photo
        private List<EyeDetectResult> eyeDetectResults = new List<EyeDetectResult>();
        private List<RectangleF> leftEyeRects;  // Detected Left eyees in each detected face
        private List<RectangleF> rightEyeRects; //  Detected Right eyees in each detected face

        private float eyeMark;                  // Size of the drawn eye marker
        private float imageScale;               // Scaling factor to fit image in pictureBox
        private Image photoImage;               // Displayed photo
        private Rectangle photoRect;            // Photo size displayed
        private Pen facePen = new Pen(Color.Black, 2);
        private Pen eyePen = new Pen(Color.Red, 2);

        private float detectionThreshold = 0;

        private string faceDetectorData = "classifier.txt";

        public static string FindParamFile(string file)
        {
            if (File.Exists(file))
                return file;

            string dir = Path.GetDirectoryName(file);
            string baseName = Path.GetFileName(file);
            string releasePath = Path.Combine(Path.Combine(dir, "release"), baseName);
            
            if (File.Exists(releasePath))
                return releasePath;

            string parent = Path.GetDirectoryName(dir);

            if (parent == null)
            {
                throw new Exception("could not find file.");
            }

            return FindParamFile(Path.Combine(parent, baseName));
        }

        public Detect()
        {
            try
            {
                string dataPath = FindParamFile(Path.GetFullPath(faceDetectorData));
                faceDetector = new FaceDetector(dataPath, true, 0.0F);
            }
            catch (Exception)
            {
                    throw new Exception("Failed to initialize faceDetector Perhaps data file " + faceDetectorData + " is unavailable");
            }
            eyeDetect = new EyeDetect();
            leftEyeRects = new List<RectangleF>();
            rightEyeRects = new List<RectangleF>();
        }

        public void SetFaceFeatureFile(string file)
        {
            if (false == eyeDetect.SetAlgorithm(EyeDetect.AlgorithmEnum.NN, file))
            {
                throw new Exception("Could not load detector file " + file);
            }

        }      

        public void DetectFile(string file)
        {

            photoImage = Image.FromFile(file);
            imageScale = 1;

            photoRect = new Rectangle(0, 0, (int)(imageScale * photoImage.Size.Width), (int)(imageScale * photoImage.Size.Height));

            DateTime start = DateTime.Now;
            faceDetector.SetTargetDimension(640, 480);

            // Run face detection
            DetectionResult detectionResult = faceDetector.DetectObject(file);
            faceDetectRects = detectionResult.GetMergedRectList(detectionThreshold);
            TimeSpan detectTime = new TimeSpan(DateTime.Now.Ticks - start.Ticks);

            leftEyeRects.Clear();
            rightEyeRects.Clear();

            RunEyeDetection();
        }

        public void PrintResults()
        {
            for (int f = 0; f < faceDetectRects.Count; f++)
            {
                ScoredRect r = faceDetectRects[f];
                EyeDetectResult e = eyeDetectResults[f];
                Console.Write("Detect {0} {1} {2} {3}  ", r.X, r.Y, r.Width, r.Height);
                Console.Write("Eye {0} {1} {2} {3}  ", e.LeftEye.X, e.LeftEye.Y, e.RightEye.X, e.RightEye.Y);
                if (e is FaceFeatureResult)
                {
                    FaceFeatureResult faceResult = e as FaceFeatureResult;
                    Console.Write("Nose {0} {1}  ", faceResult.Nose.X, faceResult.Nose.Y);
                    Console.Write("Mouth {0} {1} {2} {3}  ", faceResult.LeftMouth.X, faceResult.LeftMouth.Y, faceResult.RightMouth.X, faceResult.RightMouth.Y);

                }

                
                Console.WriteLine();
            }
        }
        /// <summary>
        /// Detect eyes in each detected face. Note the eye detector runs only on the face detected
        /// portion  of a photo, so face detection must be run first. 
        /// In this method the whole photo is passed to the eye detector togetehr with a face rect
        /// The eye detector extracts the face, scales it and converts to gryscale before runningthe detector
        /// If your calling code has already extracted and converted the input photo then
        /// it is much more efficient to call the eye Detect method that accepts this data
        /// </summary>
        private void RunEyeDetection()
        {
            eyeDetectResults.Clear();

            Bitmap photoBitMap = (Bitmap)photoImage;
            Rectangle rect = new Rectangle(0, 0, photoBitMap.Width, photoBitMap.Height);
            BitmapData data = photoBitMap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int bytes = data.Stride * photoBitMap.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);

            DateTime start = DateTime.Now;
            foreach (ScoredRect r in faceDetectRects)
            {
                Rectangle faceRect = new Rectangle(r.X, r.Y, r.Width, r.Height);

                // This is fairly inefficient as the the face must first be extracted and scaled before eye detecion is run
                EyeDetectResult eyeResult = eyeDetect.Detect(rgbValues, photoBitMap.Width, photoBitMap.Height, data.Stride, faceRect);
                eyeDetectResults.Add(eyeResult);

                float eyeRectLen = eyeMark * faceRect.Width;
                float eyeRectLen2 = eyeRectLen / 2.0F;

                // Save the rects that will be displayed

                leftEyeRects.Add(new RectangleF((float)eyeResult.LeftEye.X - eyeRectLen2,
                                                (float)eyeResult.LeftEye.Y - eyeRectLen2,
                                                eyeRectLen, eyeRectLen));
                rightEyeRects.Add(new RectangleF((float)eyeResult.RightEye.X - eyeRectLen2,
                                                    (float)eyeResult.RightEye.Y - eyeRectLen2,
                                                    eyeRectLen, eyeRectLen));

            
            }
            TimeSpan detectTime = new TimeSpan(DateTime.Now.Ticks - start.Ticks);

            photoBitMap.UnlockBits(data);
        }

        public List<int> FindFacePoints(string image)
        {
            DetectFile(image);

            if (this.faceDetectRects.Count != 1)
            {
                string error = String.Format("Found {0} faces (not exactly 1!) in image {1}", this.faceDetectRects.Count, image);
                throw new System.Exception(error);
            }

            EyeDetectResult eyeResult = this.eyeDetectResults[0];
            double lx = eyeResult.LeftEye.X;
            double ly = eyeResult.LeftEye.Y;
            double rx = eyeResult.RightEye.X;
            double ry = eyeResult.RightEye.Y;

            if (eyeResult is FaceFeatureResult)
            {
                FaceFeatureResult faceResult = eyeResult as FaceFeatureResult;
                return FaceAnchorPoints(lx, ly, rx, ry,
                    (faceResult.LeftMouth.X + faceResult.RightMouth.X) / 2, (faceResult.LeftMouth.Y + faceResult.RightMouth.Y) / 2);
            }
            else 
            {
                return FaceAnchorPoints(lx, ly, rx, ry);
            }
        }

        public static List<int> FaceAnchorPoints(double lx, double ly, double rx, double ry)
        {
            // Default Mouth is one eye width down and half way across
            double mx = lx + (0.5f * (rx - lx)) - (ry - ly);
            double my = ly + (0.5f * (ry - ly)) + (rx - lx);

            return FaceAnchorPoints(lx, ly, rx, ry, mx, my);
        }

        public static List<int> FaceAnchorPoints(double lx, double ly, double rx, double ry, double mx, double my)
        {
            List<int> res = new List<int>();

            res.Add((int)Math.Round(lx));
            res.Add((int)Math.Round(ly));
            res.Add((int)Math.Round(rx));
            res.Add((int)Math.Round(ry));
            res.Add((int)Math.Round(mx));
            res.Add((int)Math.Round(my));

            return res;
        }


        public string ImagePlusPoints(string image)
        {
            return BlendImageArgument(image, FindFacePoints(image));
        }

        public static string BlendImageArgument(string image, List<int> points)
        {
            string res = image;
            foreach (int p in points)
            {
                res += String.Format(" {0}", p);
            }
            return res;
        }

        public static List<T> MakeList<T>(params T[] args)
        {
            List<T> res = new List<T>();
            foreach (T val in args)
            {
                res.Add(val);
            }
            return res;
        }

        public static T[] MakeArray<T>(params T[] args)
        {
            return args;
        }

        public static string FindBlendExe()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "blend.exe");
            if (File.Exists(path))
                return path;

            path = "c:/MSR/Research/FaceMerge/Bin/blend.exe";

            return path;
        }

 
        /// <summary>
        /// Produces a gallery of offspring.  The face positions have already been computed.  There is one element of the gallery for each mask.
        /// </summary>
        public List<string> ProduceGallery(string baseImage, List<int> basePoints, string srcImage, List<int> srcPoints,
            string[] maskImages, List<int> maskPoints, string resName, bool dontRun)
        {
            List<string> resultImages = new List<string>();
            ProcessStartInfo si = new ProcessStartInfo();

            string baseBlendArgs = " -baseEx " + BlendImageArgument(baseImage, basePoints);
            string srcBlendArgs = " -srcEx " + BlendImageArgument(srcImage, srcPoints);
            string imArguments = baseBlendArgs + srcBlendArgs;

            si.CreateNoWindow = false;
            si.RedirectStandardError = true;
            si.RedirectStandardOutput = true;
            si.UseShellExecute = false;
            si.WorkingDirectory = System.Environment.CurrentDirectory;

            si.FileName = FindBlendExe();
            int timeOut = 10000;
            string path = System.IO.Path.GetTempPath();

            for (int m = 0; m < maskImages.Length; ++m)
            {
                Process proc = new Process();
                string resFile = Path.Combine(path, String.Format("{0}_{1:D3}.jpg", resName, m));
                si.Arguments = imArguments + " -maskEx " + BlendImageArgument(maskImages[m], maskPoints) + " -out " + resFile;

                proc.StartInfo = si;
                System.Console.Error.WriteLine("{0} {1}", si.FileName, si.Arguments);
                if (!dontRun)
                {
                    proc.Start();
                    proc.WaitForExit(timeOut);
                    System.Console.Error.WriteLine("Done");

                    resultImages.Add(Path.Combine(si.WorkingDirectory, resFile));

                    if (!proc.HasExited)
                    {
                        string error;
                        error = String.Format("Command did not exit after {0} seconds.\n", timeOut / 1000);
                        error += String.Format("{0} {1}", si.FileName, si.Arguments);
                        throw new System.Exception(error);
                    }
                }
            }
            return resultImages;
        }

        public void FaceMergePair(string imageBase, string imageSrc, string maskImage, string result, bool dontRun)
        {

            List<int> basePoints = FindFacePoints(imageBase);
            List<int> srcPoints = FindFacePoints(imageSrc);
            List<int> maskPoints = MakeList<int>(400, 400, 500, 400, 450, 500);

            Blend(imageBase, basePoints, imageSrc, srcPoints, maskImage, maskPoints, result, dontRun);
        }

        public static void Blend(string baseIm, List<int> basePts, string srcIm, List<int> srcPts, string maskIm, List<int> maskPts, string outIm, bool dontRun)
        {
            ProcessStartInfo si = new ProcessStartInfo();

            si.CreateNoWindow = false;
            si.RedirectStandardError = true;
            si.RedirectStandardOutput = true;
            si.UseShellExecute = false;
            si.WorkingDirectory = System.Environment.CurrentDirectory;

            si.FileName = FindBlendExe();

            Process proc = new Process();
            string args = "";
            args += " -srcEx " + BlendImageArgument(srcIm, srcPts);
            args += " -baseEx " + BlendImageArgument(baseIm, basePts);
            args += " -maskEx " + BlendImageArgument(maskIm, maskPts);
            args += " -out " + outIm;
            si.Arguments = args;

            proc.StartInfo = si;
            System.Console.Error.WriteLine("{0} {1}", si.FileName, si.Arguments);
            if (!dontRun)
            {
                proc.Start();
                proc.WaitForExit();
            }
        }

        public static void CollectGallery(List<string> imageList, string gallery, int thumbSize)
        {
            List<Image> images = new List<Image>();

            foreach (string fileName in imageList)
            {
                using (Bitmap bmp = new Bitmap(fileName))
                {
                    // images.Add(bmp.GetThumbnailImage(thumbSize, thumbSize, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero));
                    images.Add(Detect.Thumbnail(bmp, thumbSize, thumbSize));
                }
            }

            using (Bitmap res = new Bitmap((thumbSize * images.Count / 2), thumbSize * 2))
            {
                Graphics gfx = Graphics.FromImage(res);

                int imNum = 0;
                int colOffset = 0;
                int rowOffset = 0;
                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < images.Count / 2; ++c)
                    {
                        gfx.DrawImage(images[imNum++], colOffset, rowOffset);
                        colOffset += thumbSize;
                    }
                    colOffset = 0;
                    rowOffset += thumbSize;
                }

                res.Save(gallery);
            }
        }

        public static Bitmap HalfSize(Bitmap orig)
        {
            Bitmap half = new Bitmap((int)Math.Ceiling(orig.Width / 2.0), (int)Math.Ceiling(orig.Height / 2.0));
            HalfSizeBitmap(orig, half);
            return half;
        }

        public static void HalfSizeBitmap(Bitmap orig, Bitmap half)
        {
            BitmapData origData = orig.LockBits(new Rectangle(0, 0, orig.Width, orig.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData halfData = half.LockBits(new Rectangle(0, 0, half.Width, half.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                HalfSizeBitmapData(origData, halfData);
            }
            finally
            {
                orig.UnlockBits(origData);
                half.UnlockBits(halfData);
            }
        }

        public unsafe static void HalfSizeBitmapData(BitmapData orig, BitmapData half)
        {
            System.Diagnostics.Debug.Assert(orig.PixelFormat == PixelFormat.Format32bppArgb);
            System.Diagnostics.Debug.Assert(half.PixelFormat == PixelFormat.Format32bppArgb);


            int origStride = orig.Stride;
            int halfStride = half.Stride;

            byte* origRowPointer = (byte*)orig.Scan0.ToPointer();
            byte* halfRowPointer = (byte*)half.Scan0.ToPointer();

            for (int nr = 0; nr < orig.Height / 2; ++nr)
            {
                byte* origPointer = origRowPointer;
                byte* halfPointer = halfRowPointer;

                for (int nc = 0; nc < orig.Width / 2; ++nc)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        UInt32 pixel = 0;

                        byte* origBase = origPointer + i;

                        pixel += *(origBase);
                        pixel += *(origBase + 4);
                        pixel += *(origBase + origStride);
                        pixel += *(origBase + origStride + 4);

                        *(halfPointer + i) = (byte)(pixel / 4);
                    }

                    origPointer += 8;
                    halfPointer += 4;
                }
                origRowPointer += 2 * origStride;
                halfRowPointer += halfStride;
            }

            if (orig.Height / 2 < half.Height)
            {
                // Fill in the bottom row
                origRowPointer = (byte*)orig.Scan0.ToPointer();
                halfRowPointer = (byte*)half.Scan0.ToPointer();

                byte* origPointer = origRowPointer + ((orig.Height - 2) * origStride); ;
                byte* halfPointer = halfRowPointer + ((half.Height - 1) * halfStride);

                for (int nc = 0; nc < orig.Width / 2; ++nc)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        UInt32 pixel = 0;

                        byte* origBase = origPointer + i;

                        pixel += *(origBase);
                        pixel += *(origBase + 4);

                        *(halfPointer + i) = (byte)(pixel / 2);
                    }

                    origPointer += 8;
                    halfPointer += 4;
                }
            }


            if (orig.Width / 2 < half.Width)
            {
                // Fill in the right col
                origRowPointer = (byte*)orig.Scan0.ToPointer();
                halfRowPointer = (byte*)half.Scan0.ToPointer();

                for (int nr = 0; nr < orig.Height / 2; ++nr)
                {
                    // Go to last column
                    byte* origPointer = origRowPointer + (orig.Stride - 4);
                    byte* halfPointer = halfRowPointer + (half.Stride - 4);

                    for (int i = 0; i < 4; i++)
                    {
                        UInt32 pixel = 0;

                        byte* origBase = origPointer + i;

                        pixel += *(origBase);
                        pixel += *(origBase + origStride);

                        *(halfPointer + i) = (byte)(pixel / 2);
                    }

                    origRowPointer += 2 * origStride;
                    halfRowPointer += halfStride;
                }
            }

            if ((orig.Height / 2 < half.Height) && (orig.Width / 2 < half.Width))
            {
                origRowPointer = (byte*)orig.Scan0.ToPointer();
                halfRowPointer = (byte*)half.Scan0.ToPointer();

                byte* origPointer = origRowPointer + ((orig.Height - 1) * origStride);
                byte* halfPointer = halfRowPointer + ((half.Height - 1) * halfStride);

                origPointer += (orig.Stride - 4);
                halfPointer += (half.Stride - 4);

                for (int i = 0; i < 4; i++)
                {
                    *(halfPointer + i) = *(origPointer + i);
                }
            }
        }

        public static Bitmap Thumbnail(Bitmap bmp, int width, int height)
        {
            if (bmp.Width / 2 <= width || bmp.Height/2 <= height)
            {
                return ThumbnailDirect(bmp, width, height);
            }
            else
            {
                using (Bitmap half = HalfSize(bmp))
                {
                    return Thumbnail(half, width, height);
                }
            }
        }

        public static Bitmap ThumbnailDirect(Bitmap bmp, int width, int height)
        {
            Bitmap res = new Bitmap(width, height);
            Graphics gfx = Graphics.FromImage(res);

            // Need to draw into the center of the image
            float fromWidth = bmp.Width;
            float fromHeight = bmp.Height;
            float fromAspect = fromWidth / ((float) fromHeight);
            float toAspect = width / ((float) height);

            if (toAspect < fromAspect)
            {
                float scale = width / fromWidth;
                float rWidth = width;
                float rHeight = fromHeight * scale;
                float deltaHeight = (height - rHeight) / 2.0f;

                RectangleF destRect = new RectangleF(0, deltaHeight, rWidth, rHeight);

                gfx.DrawImage(bmp, destRect);
            }
            else
            {
                float scale = height / fromHeight;
                float rWidth = fromWidth * scale;
                float rHeight = height;
                float deltaWidth = (width - rWidth) / 2.0f;

                RectangleF destRect = new RectangleF(deltaWidth, 0, rWidth, rHeight);

                gfx.DrawImage(bmp, destRect);

            }
            return res;
        }
    }
}
