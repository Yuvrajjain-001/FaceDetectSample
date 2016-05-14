using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;
using DetectionManagedLib;

namespace sampleFaceDetectManaged
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public bool CallBackDummy()
        {
            return false;
        }

                // Return a list of rectangles with detected faces
        private List<Rectangle> FaceDetection(string imageFileName)
        {
            string classifierData = @"%inetroot%\private\research\private\CollaborativeLibs_01\LibFaceDetect\FaceDetect\Classifier\classifier.txt";
            float detectionThreshold = 0.0F;

            FaceDetector detector = new FaceDetector(
                    classifierData,
                    true,
                    detectionThreshold);

            // Run Detection            
            DetectionResult detectionResult = detector.DetectObject(imageFileName);
            List<ScoredRect> scoredResultList = detectionResult.GetMergedRectList(0.0F);

            if (scoredResultList.Count < 0)
            {
                return null;
            }

            List<Rectangle> faceRects = new List<Rectangle>();

            foreach (ScoredRect scoredRect in scoredResultList)
            {
                Rectangle rect = new Rectangle();

                rect.X = scoredRect.X;
                rect.Y = scoredRect.Y;
                rect.Width = scoredRect.Width;
                rect.Height = scoredRect.Height;

                faceRects.Add(rect);
            }

            return faceRects;
        }


        private void DisplayImage(String fileName)
        {
            if (false == File.Exists(fileName))
            {
                return;
            }

            Bitmap photoBitmap = new Bitmap(fileName);
            Rectangle rect = new Rectangle(0, 0, photoBitmap.Width, photoBitmap.Height);
            BitmapData photoData = photoBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            //int bytePerPixel = 3;
            int totBytes = photoData.Height * photoData.Stride;
            byte[] dataPixs = new byte[totBytes];
            System.Runtime.InteropServices.Marshal.Copy(photoData.Scan0, dataPixs, 0, totBytes);
            photoBitmap.UnlockBits(photoData);

            pictureBoxFace.Image = (Image)photoBitmap.GetThumbnailImage(pictureBoxFace.Width, 
                pictureBoxFace.Height,
                new Image.GetThumbnailImageAbort(CallBackDummy),
                IntPtr.Zero);

            List<Rectangle> faces = FaceDetection(fileName);
        }

        private void buttonDetect_MouseClick(object sender, MouseEventArgs e)
        {
            DisplayImage(textBoxFile.Text);           
        }


    }
}