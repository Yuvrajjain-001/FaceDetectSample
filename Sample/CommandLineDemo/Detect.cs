using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;

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

        private string basePath = "";

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


    }
}
