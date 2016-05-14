using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DetectionManagedLib;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using Microsoft.LiveLabs;

namespace FaceSortUI
{
    /// <summary>
    /// Object to deal with a whole phot that may contain faces
    /// Enables face detection and creation of face objects found
    /// </summary>
    public class Photo : System.Windows.Controls.Canvas, IDisplayableElement
    {
        private int _ID;
        private string _pathName;
        private List<Rectangle> _outlineRectList;
        int _faceDisplayWidth;

        private string _fileName;
        private Border _border;
        private Image _childImage;
        private BitmapSource  _bitmap;
        private BackgroundCanvas _mainCanvas;
        private List<Rect> _faceRectList;
        private List<Point> _leftEyeList;
        private List<Point> _rightEyeList;
        private List<Face> _faceList;
        private Rect _targetRect;
        private Rect _originalRect;
        private int _defaultDPI;

        static private int VerticalMargin = 4;
        static private Brush DefaultRectBrush = Brushes.Blue;

        static private FaceDetector _detector = null;

        #region publicMethods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ID">Unique ID</param>
        public Photo(int ID)
        {
            _ID = ID;
        }

        /// <summary>
        /// Returnds reference to  myparent whicj is always teh main Canvas
        /// inherited from IDisplayableElement
        /// </summary>
        public IDisplayableElement MyParent
        {
            get
            {
                return _mainCanvas;
            }
        }
        /// <summary>
        /// Returns my unique ID (inherited form IDisplayableElement
        /// Inherited from IDisplayableElement
        /// </summary>
        public int MyID
        {
            get
            {
                return _ID;
            }
        }

        /// <summary>
        /// Initialize a photo - run faceDetection
        /// </summary>
        /// <param name="mainCanvas">Main canvas reference</param>
        /// <param name="filename">Full path name to image file</param>
        public int InitializeWithFaceDetection(BackgroundCanvas mainCanvas, string filename)
        {
            if (null == _detector)
            {
                _detector = new FaceDetector(
                                        mainCanvas.OptionDialog.FaceDetectorDataPath,
                                        true,
                                        mainCanvas.OptionDialog.FaceDetectorThreshold);
            }
            _detector.SetTargetDimension(mainCanvas.OptionDialog.FaceDetectTargetWidth,
                                        mainCanvas.OptionDialog.FaceDetectTargetHeight);

            DetectionResult detectionResult = _detector.DetectObject(filename);
            List<ScoredRect> scoredResultList = detectionResult.GetMergedRectList(0.0F);

            if (scoredResultList.Count < 0)
            {
                return 0;
            }

            List<Rect> faceRects = new List<Rect>();

            foreach (ScoredRect scoredRect in scoredResultList)
            {
                Rect rect = new Rect();

                rect.X = scoredRect.X;
                rect.Y = scoredRect.Y;
                rect.Width = scoredRect.Width;
                rect.Height = scoredRect.Height;

                faceRects.Add(rect);
            }

            _targetRect = new Rect();
            _faceDisplayWidth = mainCanvas.OptionDialog.FaceDisplayWidth;
            _defaultDPI = mainCanvas.OptionDialog.DefaultDPI;
            return InitializeInternal(mainCanvas, filename, faceRects, mainCanvas.OptionDialog.BorderWidth, null);

        }

        public int InitializeWithEyeList(BackgroundCanvas mainCanvas, string filename, List<Point> leftEyeList, List<Point> rightEyeList)
        {
            if (null == _detector)
            {
                _detector = new FaceDetector(
                                        mainCanvas.OptionDialog.FaceDetectorDataPath,
                                        true,
                                        mainCanvas.OptionDialog.FaceDetectorThreshold);
            }
            // VIOLA: Removed face detection...  since we have the eye are are relying on them


            List<Rect> faceRects = new List<Rect>();

            for(int i = 0; i < leftEyeList.Count; ++i)
            {
                Rect rect = new Rect();
                

                Vector leye = new Vector(leftEyeList[i].X, leftEyeList[i].Y);
                Vector reye = new Vector(rightEyeList[i].X, rightEyeList[i].Y);

                Vector delta = reye - leye;

                double eyeWidth = delta.Length;

                Vector center = leye + 0.5 * delta;

                rect.X = center.X - 1.5 * eyeWidth;
                rect.Y = center.Y - 1.0 * eyeWidth;
                rect.Width = 3.0 * eyeWidth;
                rect.Height = 3.0 * eyeWidth;

                faceRects.Add(rect);
            }

            _targetRect = new Rect();
            _faceDisplayWidth = mainCanvas.OptionDialog.FaceDisplayWidth;
            _defaultDPI = mainCanvas.OptionDialog.DefaultDPI;
            _leftEyeList = leftEyeList;
            _rightEyeList = rightEyeList;

            return InitializeInternal(mainCanvas, filename, faceRects, mainCanvas.OptionDialog.BorderWidth, null);

        }
        /// <summary>
        /// Initialize a photo by displaying detected faces
        /// </summary>
        /// <param name="mainCanvas">Main canvas reference</param>
        /// <param name="filename">Full path name to image file</param>
        /// <param name="faceRectList">Detected location of faces</param>
        /// <param name="borderWidth">Border width to use around faces</param>
        /// <param name="faceIdList">Id's to use for each detected face </param>
        /// <returns>Number of faces displayed</returns>
        private int InitializeInternal(BackgroundCanvas mainCanvas, string filename, List<Rect> faceRectList,
            int borderWidth, List<int> faceIdList)
        {
            _faceRectList = faceRectList;
            _mainCanvas = mainCanvas;
            _pathName = filename;
            _fileName = System.IO.Path.GetFileName(_pathName);

            Dpu.ImageProcessing.Image[] dataPixs = CreateMainBitMap(filename);

            _border = new Border();

            RenderTransform = new ScaleTransform(1.0, 1.0); ;

            _childImage = new Image();
            _childImage.Source = _bitmap;
            _border.Child = _childImage;
            _border.BorderThickness = new Thickness(borderWidth);
            _border.BorderBrush = Brushes.Black;
            _border.Background = Brushes.White;

            Children.Add(_border);

            double xScale = _bitmap.PixelWidth / _originalRect.Width;
            double yScale =  _bitmap.PixelHeight / _originalRect.Height;
            bool doEyeDetect = false;
            EyeDetect eyeDetect = null;
            Rect displayFaceRect = new Rect(0, 0, _faceDisplayWidth, _faceDisplayWidth);
            Rect eyeDetectFaceRect = displayFaceRect;

            if (_leftEyeList == null)
            {
                doEyeDetect = true;
                eyeDetect = new EyeDetect();
                eyeDetect.SetAlgorithm(_mainCanvas.OptionDialog.EyeDetectAlgo, _mainCanvas.OptionDialog.EyeDetectPathName);
                if (_mainCanvas.OptionDialog.EyeDetectAlgo == EyeDetect.AlgorithmEnum.NN)
                {
                    eyeDetectFaceRect = new Rect(0, 0, 41, 41);
                }
            }

            _outlineRectList = new List<Rectangle>();
            _faceList = new List<Face>();

            for (int iRect = 0; iRect < faceRectList.Count ; ++iRect)
            {
                Rectangle outlineRect = new Rectangle();

                Rect rect = faceRectList[iRect];
                outlineRect.Width = rect.Width * xScale;
                outlineRect.Height = rect.Height * yScale;
                SetLeft(outlineRect, rect.X * xScale + _border.BorderThickness.Left);
                SetTop(outlineRect, rect.Y * yScale + _border.BorderThickness.Top);
                outlineRect.Stroke = Brushes.Blue;
                outlineRect.Visibility = Visibility.Visible;
                outlineRect.Opacity = 1.0;

                Children.Add(outlineRect);

                Image faceImage = new Image();
                Point targetLeft = new Point(0, 0);
                Point targetRight = new Point(0, _faceDisplayWidth);

                BitmapSource newbitMap = SelectAndNormalizeBitmap(dataPixs,
                                new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y),
                                new Point(0, 0), new Point(_faceDisplayWidth, 0), displayFaceRect);

                int eyeId = FindEyes(_leftEyeList, _rightEyeList, rect);
                Point leftEye = new Point(0, 0);
                Point rightEye = new Point(0, 0);

                if (eyeId >= 0)
                {
                    leftEye = _leftEyeList[eyeId];
                    rightEye = _rightEyeList[eyeId];
                }
                else if (doEyeDetect == true)
                {
                    byte[] faceEyeDetect;
                    int faceDetectPixCount = (int)(eyeDetectFaceRect.Width *eyeDetectFaceRect.Height);

                    faceEyeDetect = SelectAndNormalizePatch(dataPixs,
                           new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y),
                           new Point(0, 0), new Point(eyeDetectFaceRect.Width, 0), eyeDetectFaceRect);


                    faceEyeDetect = ConvertToGreyScale(faceEyeDetect, faceEyeDetect.Length / faceDetectPixCount);

                    EyeDetectResult eyeResult = eyeDetect.Detect(faceEyeDetect, 
                        (int)eyeDetectFaceRect.Width, (int)eyeDetectFaceRect.Height);
                    leftEye.X = eyeResult.LeftEye.X * rect.Width / eyeDetectFaceRect.Width + rect.X;
                    leftEye.Y = eyeResult.LeftEye.Y * rect.Height / eyeDetectFaceRect.Height + rect.Y;
                    rightEye.X = eyeResult.RightEye.X * rect.Width / eyeDetectFaceRect.Width + rect.X;
                    rightEye.Y = eyeResult.RightEye.Y * rect.Height / eyeDetectFaceRect.Height + rect.Y;
                }
                // else This is case where eye list is supplied an dthis face was not found - do not add this face

                BitmapSource normBitMap = SelectAndNormalizeBitmap(dataPixs,
                        leftEye, rightEye,
                    _mainCanvas.OptionDialog.NormalizeLeftEyeLocation, _mainCanvas.OptionDialog.NormalizeRightEyeLocation,
                    displayFaceRect);


                if (null != newbitMap && null != normBitMap)
                {
                    int faceID;
                    if (null != faceIdList && iRect < faceIdList.Count)
                    {
                        faceID = faceIdList[iRect];
                    }
                    else
                    {
                        faceID = mainCanvas.CreateNewObjectID();
                    }
                    Face face = new Face(mainCanvas, filename, faceID, normBitMap, newbitMap, iRect, this);
                    if (null != normBitMap)
                    {
                        leftEye.X = (leftEye.X-rect.X) * _faceDisplayWidth / rect.Width;
                        leftEye.Y = (leftEye.Y - rect.Y) * _faceDisplayWidth / rect.Height;
                        rightEye.X = (rightEye.X - rect.X) * _faceDisplayWidth / rect.Width;
                        rightEye.Y = (rightEye.Y - rect.Y) * _faceDisplayWidth / rect.Height;

                        face.LeftEye = leftEye;
                        face.RightEye = rightEye;
                    }
                    mainCanvas.AddFace(face);
                    _faceList.Add(face);
                    _outlineRectList.Add(outlineRect);
                }
                else
                {
                    //Face is is not used remove the rect
                    _faceList.Add(null);
                }

            }

            MakeInvisible();
            return _outlineRectList.Count;
        }

        private int FindEyes(List<Point> leftEyeList, List<Point> rightEyeList, Rect rect)
        {
            int eyeId = -1;

            if (null != leftEyeList && null != rightEyeList)
            {
                for (int id = 0; id < Math.Min(leftEyeList.Count, rightEyeList.Count); ++id)
                {
                    if (rect.Contains(leftEyeList[id]) && rect.Contains(rightEyeList[id]))
                    {
                        eyeId = id;
                        break;
                    }
                }
            }

            return eyeId;
        }
        /// <summary>
        /// Constructor for deserialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        /// <param name="mainCanvas">The top level canavs</param>
        /// <param name="id">Unique Id for desrerialization</param>
        public Photo(SerializationInfo info, StreamingContext context, BackgroundCanvas mainCanvas, int id)
        {
            _ID = info.GetInt32("PhotoID"+id.ToString());
            _pathName = info.GetString("PhotoPathName" + MyID.ToString());
            int faceRectCount = info.GetInt32("PhotoFaceCount" + MyID.ToString());
            _faceDisplayWidth = info.GetInt32("PhotoFaceDisplayWidth" + MyID.ToString());
            int borderWidth = info.GetInt32("PhotoBorderWidth" + MyID.ToString());
            _defaultDPI = info.GetInt32("PhotoDefaultDPI" + MyID.ToString());

            List<Rect> faceRects = new List<Rect>();
            List<Point> leftEyeList = new List<Point>();
            List<Point> rightEyeList = new List<Point>();
            List<int> faceIdList = new List<int>();

            for(int iRect = 0 ; iRect < faceRectCount ; ++iRect)
            {
                Rect rect = new Rect();

                int faceId = info.GetInt32("PhotoFaceID" + MyID.ToString() + "_" + iRect.ToString());

                if (faceId >= 0)
                {
                    faceIdList.Add(faceId);
                rect.X = info.GetDouble("PhotoRectX" + MyID.ToString() + "_" + iRect.ToString());
                rect.Y = info.GetDouble("PhotoRectY" + MyID.ToString() + "_" + iRect.ToString());
                rect.Width = info.GetDouble("PhotoRectW" + MyID.ToString() + "_" + iRect.ToString());
                rect.Height = info.GetDouble("PhotoRectH" + MyID.ToString() + "_" + iRect.ToString());
                faceRects.Add(rect);
                }

                try
                {
                    Point leftEye = new Point();
                    Point rightEye = new Point();
                    leftEye.X = info.GetDouble("PhotoLeftEyeX" + MyID.ToString() + "_" + iRect.ToString());
                    leftEye.Y = info.GetDouble("PhotoLeftEyeY" + MyID.ToString() + "_" + iRect.ToString());
                    rightEye.X = info.GetDouble("PhotoRightEyeX" + MyID.ToString() + "_" + iRect.ToString());
                    rightEye.Y = info.GetDouble("PhotoRightEyeY" + MyID.ToString() + "_" + iRect.ToString());

                    leftEyeList.Add(leftEye);
                    rightEyeList.Add(rightEye);
                }
                catch (SerializationException)
                {
                    // Silently eat it
                }
            }

            // Note leftEye and RightEye counts must match so only need to check one
            if (leftEyeList.Count > 0)
            {
                _leftEyeList = leftEyeList;
                _rightEyeList = rightEyeList;
            }
            else
            {
                _leftEyeList = null;
                _rightEyeList = null;
            }

            _targetRect = new Rect();
            InitializeInternal(mainCanvas, _pathName, faceRects, borderWidth, faceIdList);

            int iFace = 0;
            foreach (Face face in FaceList)
            {
                face.Deserialize(info, context, MyID, iFace);
                ++iFace;
            }
        }
        /// <summary>
        /// Serialization 
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization Context</param>
        /// <param name="id">Unique id to identify this object</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context, int id)
        {
            info.AddValue("PhotoID" + id.ToString(), MyID);
            info.AddValue("PhotoPathName" + MyID.ToString(), _pathName);
            info.AddValue("PhotoFaceCount"+MyID.ToString(), _faceRectList.Count);
            info.AddValue("PhotoFaceDisplayWidth" + MyID.ToString(), _faceDisplayWidth);
            info.AddValue("PhotoBorderWidth" + MyID.ToString(), _mainCanvas.OptionDialog.BorderWidth);
            info.AddValue("PhotoDefaultDPI" + MyID.ToString(), _defaultDPI);

            int iFace = 0;
            foreach(Rect rect in _faceRectList)
            {
                info.AddValue("PhotoRectX" + MyID.ToString() + "_" + iFace.ToString(), rect.X);
                info.AddValue("PhotoRectY" + MyID.ToString() + "_" + iFace.ToString(), rect.Y);
                info.AddValue("PhotoRectW" + MyID.ToString() + "_" + iFace.ToString(), rect.Width);
                info.AddValue("PhotoRectH" + MyID.ToString() + "_" + iFace.ToString(), rect.Height);

                int faceId = -1;
                if (null != _faceList[iFace])
                {
                    faceId = _faceList[iFace].MyID;
                }
                info.AddValue("PhotoFaceID" + MyID.ToString() + "_" + iFace.ToString(), faceId);

                ++iFace;
            }

            iFace = 0;
            if (null != _leftEyeList)
            {
                foreach (Point eye in _leftEyeList)
                {
                    info.AddValue("PhotoLeftEyeX" + MyID.ToString() + "_" + iFace.ToString(), eye.X);
                    info.AddValue("PhotoLeftEyeY" + MyID.ToString() + "_" + iFace.ToString(), eye.Y);
                    ++iFace;
                }
            }

            iFace = 0;
            if (null != _rightEyeList)
            {
                foreach (Point eye in _rightEyeList)
                {
                    info.AddValue("PhotoRightEyeX" + MyID.ToString() + "_" + iFace.ToString(), eye.X);
                    info.AddValue("PhotoRightEyeY" + MyID.ToString() + "_" + iFace.ToString(), eye.Y);
                    ++iFace;
                }
            }

            iFace = 0;
            foreach(Face face in FaceList)
            {
                face.SyncParentID();
                face.GetObjectData(info, context, -1, MyID, iFace);
                ++iFace;
            }
        }

        /// <summary>
        /// Clear out all my internal lists
        /// </summary>
        public void Clear()
        {
            _faceList.Clear();
            _outlineRectList.Clear();
            _faceRectList.Clear();
        }

        /// <summary>
        /// Make the photo visible in the context of a face
        /// </summary>
        /// <param name="face">Face to use as context</param>
        public void MakeVisible(Face face)
        {
            if (_mainCanvas.IsPhotoVisible == true)
            {
                ScaleTransform start = new ScaleTransform();
                start.ScaleX = 0;
                start.ScaleY = 0;

                ScaleTransform scaleTransform = (ScaleTransform)RenderTransform;

                ScaleTransform end = new ScaleTransform();
                end.ScaleX = (_mainCanvas.OptionDialog.PhotoWidth * _mainCanvas.DisplayWidth) / _bitmap.PixelWidth;
                end.ScaleY = (_mainCanvas.OptionDialog.PhotoWidth * _mainCanvas.DisplayHeight) / _bitmap.PixelHeight;
                end.ScaleX = Math.Min(end.ScaleX, end.ScaleY);
                end.ScaleY = end.ScaleX;

                scaleTransform.ScaleX = end.ScaleX;
                scaleTransform.ScaleY = end.ScaleY;

                ChangeRectColor(face.PhotoIdx, Brushes.Red, end.ScaleX);

                double leftPlacement = GetLeft(face);
                double topPlacement = GetTop(face);
                double displayWidth = _childImage.ActualWidth * scaleTransform.ScaleX;
                double displayHeight = _childImage.ActualHeight * scaleTransform.ScaleY;

                if (leftPlacement + displayWidth > _mainCanvas.DisplayWidth)
                {
                    leftPlacement = Math.Max(0, _mainCanvas.DisplayWidth - displayWidth);
                }

                if (topPlacement > displayHeight + VerticalMargin)
                {
                    topPlacement = Math.Max(0, topPlacement - displayHeight - VerticalMargin);
                }
                else
                {
                    topPlacement += face.ActualHeight + VerticalMargin;
                }

                _targetRect.X = leftPlacement;
                _targetRect.Y = topPlacement;
                _mainCanvas.MoveToFrontDisplayOrder(this);
                Visibility = Visibility.Visible;

                SetLeft(this, (int)_targetRect.X);
                SetTop(this, (int)_targetRect.Y);


                _mainCanvas.Animate(scaleTransform, start, end, -1.0, AnimateCompleteHandler);
            }

        }
        /// <summary>
        /// Hid the photo
        /// </summary>
        public void MakeInvisible()
        {
            Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Get list of active faces discarding any that are not active
        /// </summary>
        public List<Face> FaceList
        {
            get
            {
                List<Face> retList = new List<Face>();
                if (null != _faceList)
                {
                    foreach (Face face in _faceList)
                    {
                        if (null != face)
                        {
                            retList.Add(face);
                        }
                    }
                }
                return retList;
            }
        }

        #region IDisplayableElementImplementation
        /// <summary>
        /// Return my position relative to the main canvas
        /// </summary>
        /// <returns>Location </returns>
        public Point GetPositionRelativeToCanvas()
        {
            return _mainCanvas.GetPositionRelativeToCanvas(this);
        }

        /// <summary>
        /// Ensure that parentID is synced to current parent 
        /// Should sync before serializing
        /// </summary>
        public void SyncParentID()
        {
        }
        /// <summary>
        /// Rebuild my parent hierachy, typically following deserialization
        /// </summary>
        /// <param name="backgroundCanvas"></param>
        public void RebuildTree(BackgroundCanvas backgroundCanvas)
        {
            _mainCanvas = backgroundCanvas;
        }

        /// <summary>
        /// Sync with options dialog
        ///  Currently Nothing
        /// </summary>
        public void SyncDisplayOptions()
        {
        }
        #endregion IDisplayableElementImplementation

        #endregion publicmethods

        #region privatemethods

        private void AnimateCompleteHandler(Object sender, EventArgs e)
        {
            SetLeft(this, (int)_targetRect.X);
            SetTop(this, (int)_targetRect.Y);
            Width = _targetRect.Width;
            Height = _targetRect.Height;
        }

        private void ChangeRectColor(int id, Brush brush, double thicknessScale)
        {
            for (int i = 0 ; i < _outlineRectList.Count ; ++i)
            {
                if (i == id)
                {
                    _outlineRectList[i].Stroke = brush;
                    _outlineRectList[i].StrokeThickness = _mainCanvas.OptionDialog.EmphasisLineThickness / thicknessScale;
                }
                else
                {
                    _outlineRectList[i].Stroke = DefaultRectBrush;
                    _outlineRectList[i].StrokeThickness = _mainCanvas.OptionDialog.DefaultLineThickness / thicknessScale;

                }
            }
        }

        private byte[] ConvertToGreyScale(byte[] facePix, int cColorPlane)
        {
            byte[] grey = new byte[facePix.Length / cColorPlane];

            int iGrey = 0;

            for (int i = 0; i < facePix.Length; )
            {
                int val = 0;
                for (int j = 0; j < cColorPlane; ++j)
                {
                    val += facePix[i];
                    ++i;
                }
                grey[iGrey++] = (byte)(val / cColorPlane);
            }

            return grey;
        }

        
        private BitmapSource CutOutRect(BitmapImage originalImage, Int32Rect rect, int targetWidth)
        {
            if (rect.Width <= 0)
            {
                return null;
            }

            double scale = (double)targetWidth / (double)rect.Width;
            int inputWidth = (int)(scale * originalImage.PixelWidth);

            BitmapImage input = new BitmapImage();
            input.BeginInit();
            input.UriSource = originalImage.UriSource;
            input.DecodePixelWidth = inputWidth;
            input.EndInit();

            rect.Width = (int)Math.Round(rect.Width * scale);
            System.Diagnostics.Debug.Assert(rect.Width == targetWidth);
            rect.Height = rect.Width;
            rect.X = (int)(rect.X * scale);
            rect.Y = (int)(rect.Y * scale);

            int numPix = (int)(rect.Height * rect.Width);
            PixelFormat pixFormat = input.Format;
            int bytePerPixel = pixFormat.BitsPerPixel / 8;
            int cTotByte = numPix * bytePerPixel;
            int stride = rect.Width * bytePerPixel;
            byte[] allPixs = new byte[stride * rect.Height];


            input.CopyPixels(rect, allPixs, stride, 0);

            BitmapSource ret = (BitmapSource)BitmapImage.Create((int)rect.Width, (int)rect.Height,
                _defaultDPI,
                _defaultDPI, 
                input.Format, null, allPixs, stride);


            return ret;
        }

        private byte[] SelectAndNormalizePatch(Dpu.ImageProcessing.Image[] dataPixs, Point sourceLeft, Point sourceRight,
                Point targetLeft, Point targetRight,
                Rect faceRect)
        {
            Dpu.ImageProcessing.Image[] retPixs;

            if (sourceLeft.X == sourceRight.X && sourceLeft.Y == sourceRight.Y)
            {
                return null;
            }

            int bytePerPixel = _bitmap.Format.BitsPerPixel / 8;
            int stride = (int)_targetRect.Width * bytePerPixel;
            Rect sourceRect = new Rect(0, 0, (int)_originalRect.Width, (int)_originalRect.Height);

            retPixs = ImageUtils.ExtractNormalizeFace(dataPixs, sourceRect,
                                            sourceLeft, sourceRight,
                                            bytePerPixel, faceRect,
                                            targetLeft, targetRight);

            return ImageUtils.ConvertImageArrayToByteArray(retPixs);
        }

        private BitmapSource SelectAndNormalizeBitmap(Dpu.ImageProcessing.Image[] dataPixs, Point sourceLeft, Point sourceRight, 
                        Point targetLeft, Point targetRight, 
                        Rect faceRect)
        {
            byte[] facePixs = SelectAndNormalizePatch(dataPixs, sourceLeft, sourceRight, targetLeft, targetRight, faceRect);
            if (null == facePixs)
            {
                return null;
            }

            int bytePerPixel = _bitmap.Format.BitsPerPixel / 8;
            int stride = (int)(faceRect.Width * bytePerPixel);
            BitmapSource ret = (BitmapSource)BitmapImage.Create((int)faceRect.Width, (int)faceRect.Height,
                _defaultDPI,
                _defaultDPI,
                _bitmap.Format, null, facePixs, stride);


            return ret;

        }
        /// <summary>
        /// Cretae the member variable _bitmap for the photo to eet
        /// DPI and format requirements
        /// </summary>
        /// <param name="filename">Path to photo</param>
        /// <returns>Array of Images representing the iameg</returns>
        private Dpu.ImageProcessing.Image[] CreateMainBitMap(string filename)
        {
            Uri uri = new Uri("file:" + filename);
            if (false == System.IO.File.Exists(uri.LocalPath))
            {
                throw new Exception("Cannot find photo file " + filename);
            }

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = uri;
            bitmapImage.EndInit();

            if (PixelFormats.Rgb24 != bitmapImage.Format)
            {
                _bitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Rgb24, null, 0.0) as BitmapSource;
            }
            else
            {
                _bitmap = bitmapImage as BitmapSource;
            }

            if (null == _bitmap)
            {
                throw new Exception("Bitmap is null");
            }

            int bytePerPixel = _bitmap.Format.BitsPerPixel / 8;
            int stride = _bitmap.PixelWidth * bytePerPixel;
            _originalRect = new Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);
            byte[] dataPixs = new byte[stride * _bitmap.PixelHeight];
            _bitmap.CopyPixels(dataPixs, stride, 0);

            Dpu.ImageProcessing.Image [] dataImages = ImageUtils.ConvertByteArrayToImageArray(dataPixs, _originalRect, bytePerPixel);

            _targetRect = new Rect();
            _targetRect.Width = (_mainCanvas.OptionDialog.PhotoWidth * _mainCanvas.DisplayWidth);
            _targetRect.Height = (_mainCanvas.OptionDialog.PhotoWidth * _mainCanvas.DisplayHeight);

            byte[] displayPix;

            //Downscale large images
            if (_bitmap.PixelWidth > _targetRect.Width && _bitmap.PixelHeight > _targetRect.Height)
            {
                if (_bitmap.PixelWidth / _targetRect.Width > _bitmap.PixelHeight / _targetRect.Height)
                {
                    _targetRect.Height = _bitmap.PixelHeight * _targetRect.Width / _bitmap.PixelWidth;
                }
                else
                {
                    _targetRect.Width = _bitmap.PixelWidth * _targetRect.Height / _bitmap.PixelHeight;
                }

                Rect sourceRect = new Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);

                Dpu.ImageProcessing.Image[]  displayImages = ImageUtils.ScaleImage(dataImages, sourceRect, _targetRect, bytePerPixel);

                displayPix = ImageUtils.ConvertImageArrayToByteArray(displayImages);

                stride = (int)_targetRect.Width * bytePerPixel;
                _bitmap = (BitmapSource)BitmapImage.Create((int)_targetRect.Width, (int)_targetRect.Height,
                    _defaultDPI,
                    _defaultDPI,
                    _bitmap.Format, null, displayPix, stride);
            }
            else
            {
                displayPix = dataPixs;
            }

            if (_bitmap.DpiX != _defaultDPI ||
                _bitmap.DpiY != _defaultDPI)
            {
                _bitmap = BitmapImage.Create(_bitmap.PixelWidth, _bitmap.PixelHeight,
                    _defaultDPI, _defaultDPI,
                    _bitmap.Format, null,
                    displayPix, stride);
            }


            return dataImages;
        }


        #endregion
    }

}