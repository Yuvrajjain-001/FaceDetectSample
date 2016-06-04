using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Windows;
using System.IO;
using DetectionManagedLib;

namespace Microsoft.LiveLabs
{
    /// <summary>
    // This sample illustrates use of the face detection library and 
    // optionally the Eye detector
    // The key face detection code is in method listBoxFiles_SelectedIndexChanged
    /// </summary>
    public partial class FaceDetectSample : Form
    {

        private EyeDetect eyeDetect;            // Eye Detector classifier
        private FaceDetector faceDetector;      // Face Detector classifier
        private List<ScoredRect> faceDetectRects; // Detected faces in current photo
        private List<RectangleF> leftEyeRects;  // Detected Left eyees in each detected face
        private List<RectangleF> rightEyeRects; //  Detected Right eyees in each detected face
        private List<RectangleF> noseRects;  // Detected Nose in each detected face
        private List<RectangleF> leftMouthRects;  // Detected Left Mouth in each detected face
        private List<RectangleF> rightMouthRects; //  Detected Right Mouth in each detected face

        private float eyeMark;                  // Size of the drawn eye marker
        private Graphics pictureBoxGraphics;    // Where photo is displayed
        private float imageScale;               // Scaling factor to fit image in pictureBox
        private Image photoImage;               // Displayed photo
        private Rectangle photoRect;            // Photo size displayed
        private Pen facePen = new Pen(Color.Black, 2);
        private Pen eyePen = new Pen(Color.Red, 2);

        private string faceDetectorData = "classifier.txt";
        private string sentimentNetRegKey = @"Software\SentimentDemo";
        private string sentimentNetRegVal = "SentimentNet";
        private string sentimentNet = @"g:\projects\Photos\sentiment\1\net_30.bin";
        private string basePath = "";

        public FaceDetectSample()
        {
            InitializeComponent();
            pictureBoxGraphics = pictureBox1.CreateGraphics();
            eyeMark = 0.06F;
            RegistryKey key = Registry.CurrentUser.OpenSubKey(this.sentimentNetRegKey);
            if (null != key)
            {
                this.sentimentNet = (string)key.GetValue(this.sentimentNetRegVal, this.sentimentNet);
            }

            try
            {
                faceDetector = new FaceDetector(faceDetectorData, true, 0.0F);
            }
            catch (Exception)
            {
                try
                {
                    // When built in  sample directory the following might find the classifier data file
                    faceDetector = new FaceDetector(Path.Combine(@"..\..\..\release", faceDetectorData), true, 0.0F);

                }
                catch (Exception)
                {
                    throw new Exception("Failed to initialize faceDetector Perhaps data file " + faceDetectorData + " is unavailable");
                }
            }
            eyeDetect = new EyeDetect();
            leftEyeRects = new List<RectangleF>();
            rightEyeRects = new List<RectangleF>();
            noseRects = new List<RectangleF>();
            leftMouthRects = new List<RectangleF>();
            rightMouthRects = new List<RectangleF>();
        }

        /// <summary>
        /// Load the photo and run face detection. The face detector returns a collection of rects demarcating
        /// detected faces
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedImage();
        }

        private void UpdateSelectedImage()
        {
            if (listBoxFiles.SelectedIndex >= 0 )
            {
                string imageFile = Path.Combine(basePath, listBoxFiles.SelectedItem as string);
                DetectFile(imageFile);
                pictureBox1.Invalidate();
            }
        }

        private void DetectFile(string file)
        {
            photoImage = Image.FromFile(file);
            imageScale = Math.Min((float)pictureBox1.Size.Width / photoImage.Size.Width,
                                (float)pictureBox1.Size.Height / photoImage.Size.Height);

            photoRect = new Rectangle(0, 0, (int)(imageScale * photoImage.Size.Width), (int)(imageScale * photoImage.Size.Height));
            pictureBoxGraphics.Clear(Color.White);

            DateTime start = DateTime.Now;
            faceDetector.SetTargetDimension(640, 480);

            // Run face detection. There are a few ways of doing this. They should
            // all yield the same result. It all depends on the form of your image data
            // Try the different overloads by uncommenting below

            // Method 1 Directly from a System.Drawing.Imaging object. 
            // Note only underlying data formats that have 1 byte per colour plane are supported
            Bitmap bitmap = new Bitmap(photoImage);
            BitmapData bitmapdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            DetectionResult detectionResult = faceDetector.DetectObject(bitmapdata);
            bitmap.UnlockBits(bitmapdata);

            // Method 2 - Use the image name works for jpg and some other common formats. Supported
            // formats are not as good as what is supported by the windows decoders
            //DetectionResult detectionResult = faceDetector.DetectObject(imageFile);

            // Method 3. Directly from a byte array. This code is included for illustartion only. It is not a suggested way of
            // actually doing this
            //Bitmap bitmap = new Bitmap(photoImage);
            //BitmapData bitmapdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            //                                    System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            //int byteCount = bitmapdata.Height * bitmapdata.Stride;
            //byte [] bytes = new byte[byteCount];
            //System.Runtime.InteropServices.Marshal.Copy(bitmapdata.Scan0, bytes, 0, byteCount);
            //DetectionResult detectionResult = faceDetector.DetectObject(bitmapdata.Width,
            //                                                            bitmapdata.Height,
            //                                                            bitmapdata.Stride,
            //                                                            3,                  // 3 Bytes per Pixel
            //                                                            bytes);
            //bitmap.UnlockBits(bitmapdata);



            faceDetectRects = detectionResult.GetMergedRectList((float)numericUpDownFaceDetectThreshold.Value);
            TimeSpan detectTime = new TimeSpan(DateTime.Now.Ticks - start.Ticks);
            textBoxFaceDetectTime.Text = detectTime.Milliseconds.ToString();

            leftEyeRects.Clear();
            rightEyeRects.Clear();
            noseRects.Clear();
            leftMouthRects.Clear();
            rightMouthRects.Clear();

            RunEyeDetection();
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
            Bitmap photoBitMap = (Bitmap)photoImage;
            Rectangle rect = new Rectangle(0, 0, photoBitMap.Width, photoBitMap.Height);
            BitmapData data = photoBitMap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int bytes = data.Stride* photoBitMap.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);

            DateTime start = DateTime.Now;
            float[] avgScore = new float[2];
            float sentimentCount = 0;
            foreach (ScoredRect r in faceDetectRects)
            {
                Rectangle faceRect = new Rectangle(r.X, r.Y, r.Width, r.Height);

                // This is fairly inefficient as the the face must first be extracted and scaled before eye detecion is run
                
                EyeDetectResult eyeResult = eyeDetect.Detect(rgbValues, photoBitMap.Width, photoBitMap.Height, data.Stride, faceRect);
                float eyeRectLen = eyeMark * faceRect.Width;
                float eyeRectLen2 = eyeRectLen / 2.0F;

                // Save the rects that will be displayed
                leftEyeRects.Add(new RectangleF((float)eyeResult.LeftEye.X - eyeRectLen2,
                                                (float)eyeResult.LeftEye.Y - eyeRectLen2,
                                                eyeRectLen, eyeRectLen));
                rightEyeRects.Add(new RectangleF((float)eyeResult.RightEye.X - eyeRectLen2,
                                                    (float)eyeResult.RightEye.Y - eyeRectLen2,
                                                    eyeRectLen, eyeRectLen));

                if (eyeResult is FaceFeatureResult)
                {
                    FaceFeatureResult faceResult = eyeResult as FaceFeatureResult;
                    noseRects.Add(new RectangleF((float)faceResult.Nose.X - eyeRectLen2,
                                                    (float)faceResult.Nose.Y - eyeRectLen2,
                                                    eyeRectLen, eyeRectLen));
                    leftMouthRects.Add(new RectangleF((float)faceResult.LeftMouth.X - eyeRectLen2,
                                                    (float)faceResult.LeftMouth.Y - eyeRectLen2,
                                                    eyeRectLen, eyeRectLen));
                    rightMouthRects.Add(new RectangleF((float)faceResult.RightMouth.X - eyeRectLen2,
                                                        (float)faceResult.RightMouth.Y - eyeRectLen2,
                                                        eyeRectLen, eyeRectLen));

                    List<float> res = RunSentimentAnalysis(r, faceResult, rgbValues, photoBitMap, data);
                    if (res != null)
                    {
                        avgScore[0] += res[0];
                        avgScore[1] += res[1];
                        ++sentimentCount;
                    }
                }

            }
            TimeSpan detectTime = new TimeSpan(DateTime.Now.Ticks - start.Ticks);
            textBoxEyeDetect.Text = detectTime.Milliseconds.ToString();

            if (sentimentCount > 0)
            {
                avgScore[0] /= sentimentCount;
                avgScore[1] /= sentimentCount;
                this.happyScore.Text = string.Format("{0:F3}", avgScore[0]);
                this.unhappyScore.Text = string.Format("{0:F3}", avgScore[1]);
            }

            photoBitMap.UnlockBits(data);
        }

        private List<float> RunSentimentAnalysis(ScoredRect r, FaceFeatureResult faceResult, byte[] rgbValues, Bitmap bitmap, BitmapData bitmapdata)
        {
            double eyeFrac = 0.6;       // Width between eyes as frac of extraction
            double eyeToMouthFrac = 0.6; // Eye to mouth fraction of extracted height
            System.Drawing.Rectangle faceRect = new System.Drawing.Rectangle(r.X, r.Y, r.Width, r.Height);


            int width = Convert.ToInt32((faceResult.RightEye.X - faceResult.LeftEye.X) / eyeFrac);
            int height = Convert.ToInt32(((faceResult.LeftMouth.Y + faceResult.RightMouth.Y) - (faceResult.RightEye.Y + faceResult.LeftEye.Y)) / eyeToMouthFrac / 2);
            int leftX = Convert.ToInt32(faceResult.LeftEye.X - ((1.0 - eyeFrac) / 2.0 * width));
            int leftY = Convert.ToInt32((faceResult.LeftEye.Y + faceResult.RightEye.Y) / 2.0 -
                                        ((1 - eyeToMouthFrac) / 2.0 * height));
            if (leftX < 0 || leftY < 0)
            {
                MessageBox.Show("Eye Detection outside face Rect", "Error");
                return null;
            }

            System.Drawing.Rectangle scaleRect = new System.Drawing.Rectangle(leftX, leftY, width, height);
            int bytePerPixel = rgbValues.Length / (bitmap.Width * bitmap.Height);
            byte[] scaledData = eyeDetect.ScaleImage(rgbValues, bitmap.Width, bitmap.Height, bitmapdata.Stride, scaleRect, bytePerPixel);
            //this.SaveSentimentResult(scaledData, targets, line, i);
            Microsoft.Search.nnRunTimeManaged nn = new Search.nnRunTimeManaged(this.sentimentNet);

            int[] intScaled = new int[scaledData.Length];
            for (int i = 0; i < scaledData.Length; ++i)
            {
                intScaled[i] = Convert.ToInt32(scaledData[i]);
            }

            List<float> result = nn.Classify<int>(intScaled);
            return result;
        }

        private void PopulateListBox(string suiteFile)
        {
            listBoxFiles.Items.Clear();
            using (StreamReader sr = new StreamReader(suiteFile))
            {
                string file;
                string suitePath = Path.GetDirectoryName(suiteFile);

                while ((file = sr.ReadLine()) != null)
                {
                    if (false == Path.IsPathRooted(file))
                    {
                        file = Path.Combine(suitePath, file);
                    }
                    listBoxFiles.Items.Add(file);
                }
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (null == faceDetectRects)
            {
                return;
            }

            Graphics gfx = e.Graphics;

            gfx.DrawImage(photoImage, photoRect);
            DrawResults(gfx);
           
        }

        public void DrawToFile(string filename)
        {
            Image res = (Image) photoImage.Clone();
            Graphics gfx = Graphics.FromImage(res);

            DrawResults(gfx);

            res.Save(filename);
        }

        public void SaveAllFiles()
        {
            foreach(string file in listBoxFiles.Items)
            {
                string imageFile = Path.Combine(basePath, file);
                string baseName = Path.GetFileNameWithoutExtension(file) + "_res";
                string ext = Path.GetExtension(file);
                
                string resFile = Path.Combine(basePath, baseName + ext);

                DetectFile(imageFile);
                imageScale = 1;
                DrawToFile(resFile);
            }
        }

        private void DrawResults(Graphics gfx)
        {
            if (true == this.checkBoxEyeDetect.Checked)
            {

                foreach (ScoredRect r in faceDetectRects)
                {
                    gfx.DrawRectangle(facePen, r.X * imageScale, r.Y * imageScale, r.Width * imageScale, r.Height * imageScale);
                }
                foreach (RectangleF r in leftEyeRects)
                {
                    RectangleF re = new RectangleF(r.X * imageScale, r.Y * imageScale, r.Width * imageScale, r.Height * imageScale);
                    gfx.DrawEllipse(eyePen, re);
                }
                foreach (RectangleF r in rightEyeRects)
                {
                    RectangleF re = new RectangleF(r.X * imageScale, r.Y * imageScale, r.Width * imageScale, r.Height * imageScale);
                    gfx.DrawEllipse(eyePen, re);
                }
                foreach (RectangleF r in noseRects)
                {
                    RectangleF re = new RectangleF(r.X * imageScale, r.Y * imageScale, r.Width * imageScale, r.Height * imageScale);
                    gfx.DrawEllipse(eyePen, re);
                }
                foreach (RectangleF r in leftMouthRects)
                {
                    RectangleF re = new RectangleF(r.X * imageScale, r.Y * imageScale, r.Width * imageScale, r.Height * imageScale);
                    gfx.DrawEllipse(eyePen, re);
                }
                foreach (RectangleF r in rightMouthRects)
                {
                    RectangleF re = new RectangleF(r.X * imageScale, r.Y * imageScale, r.Width * imageScale, r.Height * imageScale);
                    gfx.DrawEllipse(eyePen, re);
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (DialogResult.OK == dialog.ShowDialog())
            {
                basePath = Path.GetDirectoryName(dialog.FileName);
                PopulateListBox(dialog.FileName);
            }
        }

        private void checkBoxEyeDetect_CheckStateChanged(object sender, EventArgs e)
        {
            UpdateSelectedImage();
        }
        private void SaveAllButton_Click(object sender, EventArgs e)
        {
            SaveAllFiles();
        }

        private void detectorFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            if (DialogResult.OK == dialog.ShowDialog())
            {
                if (false == eyeDetect.SetAlgorithm(EyeDetect.AlgorithmEnum.NN, dialog.FileName))
                {
                    MessageBox.Show("Could not load detector file " + Path.GetFileName(dialog.FileName));
                }
            }
        }

        private void SentimentNet_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            if (DialogResult.OK == dialog.ShowDialog())
            {
                this.sentimentNet = dialog.FileName;
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                                @"Software\SentimentDemo",
                                RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key.SetValue("SentimentNet", dialog.FileName);
                }
            }
        }

        private void OpenJpg_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
            if (DialogResult.OK == dialog.ShowDialog())
            {
                DetectFile(dialog.FileName);
                pictureBox1.Invalidate();
            }

        }
    }
}