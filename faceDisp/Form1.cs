using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Windows;
using System.IO;
using ShoNS.Array;
using FaceSortUI;

namespace FaceDisp
{
    public partial class Form1 : Form
    {

        private List<FaceData> _faceData;
        private Pen _pen;
        private double _eyeMark2;
        private double _eyeMark;
        private GenPositionData.TransformSample _transform;
        private FaceData.FaceDataTypeEnum _fileType;

        private System.Windows.Point normNose = new System.Windows.Point(0.50, 0.60);
        private System.Windows.Point normLeftMouth = new System.Windows.Point(0.33, 0.76);
        private System.Windows.Point normRightMouth = new System.Windows.Point(0.66, 0.76);


        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
            _pen = new Pen(Color.Red, 2);
            _eyeMark = 0.06;
            _eyeMark2 = _eyeMark / 2.0;
            _transform = new GenPositionData.TransformSample();
        }

        private void PopulateListBox(string suiteFile)
        {
            listBoxFiles.Items.Clear();
            FaceDataFile suiteReader = null;

            try
            {
                suiteReader = new FaceDataFile(suiteFile, _fileType);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }

            if (null == suiteReader)
            {
                return;
            }

            FaceData item = null;
            _faceData = new List<FaceData>();

            while ((item = suiteReader.GetNext()) != null)
            {
                listBoxFiles.Items.Add(Path.GetFileName(item.Filename));
                SetTrueEyes(ref item, item.TrueLeftEye, item.TrueRightEye);
                SetRecoEyes(ref item, item.RecoLeftEye, item.RecoRightEye);

                _faceData.Add(item);
            }

            suiteReader.Dispose();
        }

        private FaceData ReadEyeDetectFile(string filename, string line, int iLine)
        {

            FaceData item = new FaceData(filename, line, FaceData.FaceDataTypeEnum.EyeDetect);
            SetTrueEyes(ref item, item.TrueLeftEye, item.TrueRightEye);
            SetRecoEyes(ref item, item.RecoLeftEye, item.RecoRightEye);

            return item;
        }

        private FaceData ReadFaceRecoFile(string filename, string line, int iLine)
        {
            FaceData item = new FaceData(filename, line, FaceData.FaceDataTypeEnum.FaceReco);

            item.NormNose.X = normNose.X * item.faceRect.Width;
            item.NormNose.Y = normNose.Y * item.faceRect.Height;
            item.NormLeftMouth.X = normLeftMouth.X * item.faceRect.Width;
            item.NormLeftMouth.Y = normLeftMouth.Y * item.faceRect.Height;
            item.NormRightMouth.X = normRightMouth.X * item.faceRect.Width;
            item.NormRightMouth.Y = normRightMouth.Y * item.faceRect.Height;


            double defaultRectSize = 10.0 / 41.0;
            double smallRectSize = 8.0 / 41.0;

            SetFeatureRectFromPoint(item.TrueLeftEye, ref item.TrueLeftRect, item.faceRect, defaultRectSize);
            SetFeatureRectFromPoint(item.TrueRightEye, ref item.TrueRightRect, item.faceRect, defaultRectSize);

            SetFeatureRectFromPointNorm(item.RecoLeftEye, ref item.RecoLeftRect, item.faceRect, defaultRectSize);
            SetFeatureRectFromPointNorm(item.RecoRightEye, ref item.RecoRightRect, item.faceRect, defaultRectSize);

            Rect destRect = new Rect(0, 0, item.faceRect.Width, item.faceRect.Height);
            item.AffineMat = GetFaceAffine(item.ConvertDrawingToRect(item.faceRect), item.TrueLeftEye, item.TrueRightEye, destRect, 
                item.RecoLeftEye, item.RecoRightEye);

            item.Nose = TransformPoint(item.AffineMat, destRect, item.NormNose, item.ConvertDrawingToRect(item.faceRect));
            SetFeatureRectFromPoint(item.Nose, ref item.NoseRect, item.faceRect, defaultRectSize);
            SetFeatureRectFromPointNorm(item.NormNose, ref item.NormNoseRect, item.faceRect, defaultRectSize);

            item.LeftMouth = TransformPoint(item.AffineMat, destRect, item.NormLeftMouth, item.ConvertDrawingToRect(item.faceRect));
            SetFeatureRectFromPoint(item.LeftMouth, ref item.LeftMouthRect, item.faceRect, smallRectSize);
            SetFeatureRectFromPointNorm(item.NormLeftMouth, ref item.NormLeftMouthRect, item.faceRect, smallRectSize);

            item.RightMouth = TransformPoint(item.AffineMat, destRect, item.NormRightMouth, item.ConvertDrawingToRect(item.faceRect));
            SetFeatureRectFromPoint(item.RightMouth, ref item.RightMouthRect, item.faceRect, smallRectSize);
            SetFeatureRectFromPointNorm(item.NormRightMouth, ref item.NormRightMouthRect, item.faceRect, smallRectSize);

            item.MouthRect = item.LeftMouthRect;
            item.MouthRect.Width = item.RightMouthRect.Right - item.LeftMouthRect.X;
            item.MouthRect.Height = item.RightMouthRect.Bottom - item.LeftMouthRect.Y;

            item.NormMouthRect = item.NormLeftMouthRect;
            item.NormMouthRect.Width = item.NormRightMouthRect.Right - item.NormLeftMouthRect.X;
            item.NormMouthRect.Height = item.NormRightMouthRect.Bottom - item.NormLeftMouthRect.Y;

            return item;
        }
        private void SetTrueEyes(ref FaceData item, System.Windows.Point leftEye, System.Windows.Point rightEye)
        {

            item.TrueLeftRect.X = (int)((leftEye.X - _eyeMark2) * item.faceRect.Width);
            item.TrueLeftRect.Width = (int)(_eyeMark * item.faceRect.Width);
            item.TrueLeftRect.Y = (int)((leftEye.Y - _eyeMark2) * item.faceRect.Width);
            item.TrueLeftRect.Height = (int)(_eyeMark * item.faceRect.Width);

            item.TrueRightRect.X = (int)((rightEye.X - _eyeMark2) * item.faceRect.Width);
            item.TrueRightRect.Width = (int)(_eyeMark * item.faceRect.Width);
            item.TrueRightRect.Y = (int)((rightEye.Y - _eyeMark2) * item.faceRect.Width);
            item.TrueRightRect.Height = (int)(_eyeMark * item.faceRect.Width);
        }

        private void SetFeatureRectFromPoint(System.Windows.Point pt, ref System.Drawing.Rectangle rect, System.Drawing.Rectangle faceRect, double rectSize)
        {
            rect.Width = (int)Math.Round(faceRect.Width * rectSize);
            rect.Height = (int)Math.Round(faceRect.Height * rectSize);

            rect.X = (int)Math.Round(pt.X  -faceRect.X - rect.Width / 2.0);
            rect.Y = (int)Math.Round(pt.Y - faceRect.Y - rect.Height / 2.0);
        }

        private void SetFeatureRectFromPointNorm(System.Windows.Point pt, ref System.Drawing.Rectangle rect, System.Drawing.Rectangle faceRect, double rectSize)
        {
            rect.Width = (int)Math.Round(faceRect.Width * rectSize);
            rect.Height = (int)Math.Round(faceRect.Height * rectSize);

            rect.X = (int)Math.Round(pt.X  - rect.Width / 2.0);
            rect.Y = (int)Math.Round(pt.Y  - rect.Height / 2.0);
        }

        private void SetRecoEyes(ref FaceData item, System.Windows.Point leftEye, System.Windows.Point rightEye)
        {

            item.RecoLeftRect.X = (int)((leftEye.X - _eyeMark2) * item.faceRect.Width);
            item.RecoLeftRect.Width = (int)(_eyeMark * item.faceRect.Width);
            item.RecoLeftRect.Y = (int)((leftEye.Y - _eyeMark2) * item.faceRect.Width);
            item.RecoLeftRect.Height = (int)(_eyeMark * item.faceRect.Width);

            item.RecoRightRect.X = (int)((rightEye.X - _eyeMark2) * item.faceRect.Width);
            item.RecoRightRect.Width = (int)(_eyeMark * item.faceRect.Width);
            item.RecoRightRect.Y = (int)((rightEye.Y - _eyeMark2) * item.faceRect.Width);
            item.RecoRightRect.Height = (int)(_eyeMark * item.faceRect.Width);

        }
        private void listBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex >= 0 && listBoxFiles.SelectedIndex < _faceData.Count)
            {
                FaceData item = _faceData[listBoxFiles.SelectedIndex];
                Bitmap bitmap1 = null;
                Bitmap bitmap2 = null;

                if (false == item.DoTransform)
                {
                    item.Transform.Reset();
                    bitmap1 = CreateFaceTxfm(item, item.Transform);

                    if (_fileType == FaceData.FaceDataTypeEnum.FaceReco)
                    {
                        bitmap2 = CreateFaceTxfm(item, null);
                    }
                    else
                    {
                        bitmap2 = bitmap1;
                    }
                }
                else
                {
                    bitmap1 = CreateFaceTxfm(item, item.Transform);
                    bitmap2 = bitmap1;
                }

                pictureBox1.Image = (Image)bitmap1;
                pictureBox2.Image = (Image)bitmap2;

                pictureBox2.Left = pictureBox1.Right + 20;
            }
        }


        /// <summary>
        /// Create a face by rotating the input face theta degrees
        /// </summary>
        /// <param name="face">FaceS to rotate</param>
        /// <param name="theta">Rotation in degrees</param>
        /// <returns></returns>
        private Bitmap CreateFaceTxfm(FaceData face, GenPositionData.TransformSample trans)
        {
            if (null == face )
            {
                return null;
            }


            Bitmap photoBitmap = new Bitmap(face.Filename);
            Rectangle rect = new Rectangle(0, 0, photoBitmap.Width, photoBitmap.Height);
            BitmapData photoData = photoBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int bytePerPixel = 3;
            int totBytes = photoData.Height * photoData.Stride;
            byte [] dataPixs = new byte [totBytes];
            System.Runtime.InteropServices.Marshal.Copy(photoData.Scan0, dataPixs, 0, totBytes);

            System.Windows.Rect destRect = face.FaceWindowsRect;
            destRect.X = 0.0;
            destRect.Y = 0.0;
            System.Windows.Rect srcRect = new System.Windows.Rect(0, 0, photoBitmap.Width, photoBitmap.Height);

            Bitmap faceBitmap = new Bitmap(face.faceRect.Width, face.faceRect.Height, photoData.PixelFormat);

            rect.Width = face.faceRect.Width;
            rect.Height = face.faceRect.Height;
            BitmapData faceData = faceBitmap.LockBits(rect, ImageLockMode.ReadWrite, photoData.PixelFormat);

            byte[] facePixs;

            if (null == trans)
            {
                facePixs = ImageUtils.DoAffine(dataPixs, srcRect, bytePerPixel, photoData.Stride,
                    destRect, destRect, faceData.Stride, face.AffineMat);
            }
            else
            {
                facePixs = ImageUtils.ExtractNormalizeFace(dataPixs, srcRect, bytePerPixel, photoData.Stride,
                    face.FaceWindowsRect, destRect, faceData.Stride, trans);
            }
            photoBitmap.UnlockBits(photoData);


            System.Runtime.InteropServices.Marshal.Copy(facePixs, 0, faceData.Scan0, facePixs.Length);
            faceBitmap.UnlockBits(faceData);

            return faceBitmap;

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (null != _faceData && listBoxFiles.SelectedIndex >= 0)
            {
                FaceData face = _faceData[listBoxFiles.SelectedIndex];

                if (_fileType == FaceData.FaceDataTypeEnum.EyeDetect)
                {
                    e.Graphics.DrawEllipse(_pen, face.TrueLeftRect);
                    e.Graphics.DrawEllipse(_pen, face.TrueRightRect);
                }
                else if (_fileType == FaceData.FaceDataTypeEnum.FaceReco)
                {
                    e.Graphics.DrawRectangle(_pen, face.TrueLeftRect);
                    e.Graphics.DrawRectangle(_pen, face.TrueRightRect);
                    e.Graphics.DrawRectangle(_pen, face.NoseRect);
                    //e.Graphics.DrawRectangle(_pen, face.LeftMouthRect);
                    //e.Graphics.DrawRectangle(_pen, face.RightMouthRect);
                    e.Graphics.DrawRectangle(_pen, face.MouthRect);
                }
            }
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            if (null != _faceData && listBoxFiles.SelectedIndex >= 0)
            {
                FaceData face = _faceData[listBoxFiles.SelectedIndex];
                if (_fileType == FaceData.FaceDataTypeEnum.EyeDetect)
                {
                e.Graphics.DrawEllipse(_pen, face.RecoLeftRect);
                e.Graphics.DrawEllipse(_pen, face.RecoRightRect);
                }
                else if (_fileType == FaceData.FaceDataTypeEnum.FaceReco)
                {
                    e.Graphics.DrawRectangle(_pen, face.RecoLeftRect);
                    e.Graphics.DrawRectangle(_pen, face.RecoRightRect);
                    e.Graphics.DrawRectangle(_pen, face.NormNoseRect);
                    e.Graphics.DrawRectangle(_pen, face.NormLeftMouthRect);
                    e.Graphics.DrawRectangle(_pen, face.NormRightMouthRect);
                    //e.Graphics.DrawRectangle(_pen, face.NormMouthRect);

                }
            }

        }

        private System.Windows.Point RotateEye(System.Windows.Point eye, GenPositionData.TransformSample trans)
        {
            double theta = trans.ThetaRad;
            System.Windows.Point newCentre = new System.Windows.Point();
            newCentre.X = (eye.X - 0.5) * Math.Cos(theta) - (0.5 - eye.Y) * Math.Sin(theta) + 0.5 - trans.X;
            newCentre.Y = 0.5 - ((0.5 - eye.Y) * Math.Cos(theta) + (eye.X - 0.5) * Math.Sin(theta)) - trans.Y;
            return newCentre;
        }

        private void RotateRecoEyes(FaceData item, GenPositionData.TransformSample trans)
        {

            SetRecoEyes(ref item, RotateEye(item.TrueLeftEye, trans), RotateEye(item.TrueRightEye, trans));
        }

        private void RotateRecoEyes(FaceData item)
        {

            SetRecoEyes(ref item, RotateEye(item.TrueLeftEye, item.Transform), RotateEye(item.TrueRightEye, item.Transform));
        }
        private void RotateTrueEyes(FaceData item)
        {

            SetTrueEyes(ref item, RotateEye(item.TrueLeftEye, item.Transform), RotateEye(item.TrueRightEye, item.Transform));
        }

        private void buttonRotate_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex >= 0 && listBoxFiles.SelectedIndex < _faceData.Count)
            {
                FaceData face = _faceData[listBoxFiles.SelectedIndex];

                _transform.Theta = (double)numericUpDownTheta.Value;
                _transform.X = (double)numericUpDownX.Value;
                _transform.Y = (double)numericUpDownY.Value;
                _transform.SetImageSize(face.FaceWindowsRect);
                pictureBox2.Image = CreateFaceTxfm(face, _transform);
                RotateRecoEyes(_faceData[listBoxFiles.SelectedIndex], _transform);
            }
        }

        private double[,] GetFaceAffine(Rect origRect, System.Windows.Point origLeftEye, System.Windows.Point origRightEye,
            Rect targetRect, System.Windows.Point targetLeftEye, System.Windows.Point targetRightEye)
        {

            // Step 1 - Construct the affine transformation
            // Find mapping between orig and desired EyePosAsMatrix locations + a 
            // fake point located at right angles to the vector joing the two eyes
            INumArray<float> origMat = ArrFactory.FloatArray(3, 2);
            INumArray<float> targetMat = ArrFactory.FloatArray(3, 3);
 
            FaceSortUI.ImageUtils.EyePosAsMatrix(origRect, origLeftEye, origRightEye, ref origMat);
            FaceSortUI.ImageUtils.EyePosAsMatrix(targetRect, targetLeftEye, targetRightEye, ref targetMat);
            targetMat[0, 2] = 1.0F;
            targetMat[1, 2] = 1.0F;
            targetMat[2, 2] = 1.0F;
            SVDFloat svd = new SVDFloat(targetMat);
            INumArray<float> sss = svd.Solve(origMat);
            INumArray<float> mmm = (INumArray<float>)sss.Transpose();
            double[,] affineMat = ArrFactory.DoubleArray(mmm).ToArray();

            return affineMat;

        }


        private System.Windows.Point TransformPoint(double[,] affine, Rect srcRect, System.Windows.Point src, Rect destRect)
        {
            double x = src.X - srcRect.X - srcRect.Width / 2.0;
            double y = src.Y - srcRect.Y - srcRect.Height / 2.0; ;

            System.Windows.Point ret = new System.Windows.Point();
            ret.X = affine[0, 0] * x + affine[0, 1] * y + affine[0, 2] + destRect.Width / 2.0;
            ret.Y = affine[1, 0] * x + affine[1, 1] * y + affine[1, 2] + destRect.Height / 2.0;
            return ret;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (DialogResult.OK == dialog.ShowDialog())
            {
                _fileType = FaceData.FaceDataTypeEnum.EyeDetect;
                PopulateListBox(dialog.FileName);
            }
        }

        private void OpenFaceRecoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (DialogResult.OK == dialog.ShowDialog())
            {
                _fileType = FaceData.FaceDataTypeEnum.FaceReco;
                PopulateListBox(dialog.FileName);
            }
        }

    }
}