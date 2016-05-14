using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization;

namespace FaceSortUI
{
    /// <summary>
    /// Helper class for displaying face distances in the debugView
    /// Describes the distance to an "other" face
    /// </summary>
    public class FaceDistance
    {
        private Face _face;
        private double _distance;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="face">Other face reference</param>
        /// <param name="value">Distance to the other face</param>
        public FaceDistance(Face face, double value)
        {
            _face = face;
            _distance = value;
        }

        /// <summary>
        /// Get "other" face
        /// </summary>
        public Face Face
        {
            get
            {
                return _face;
            }
        }

        /// <summary>
        /// Get a string identifying the "other" face
        /// </summary>
        public String FaceName
        {
            get
            {
                return _face.FileName;
            }
        }
        /// <summary>
        /// Get distance to "other" face
        /// </summary>
        public String Distance
        {
            get
            {
                return _distance.ToString("####0.0##");
            }
        }

    }
    /// <summary>
    /// Represent a single face
    /// </summary>
    [Serializable()]
    public class Face : System.Windows.Controls.Border, FaceSortUI.IDisplayableElement
    {

        /// <summary>
        /// Mouse selection mode of a face
        /// None - not selected
        /// ElementSelect - Free face selected
        /// GroupCreation - Selected as part of new group definition (Selecting on main canvas)
        /// Extend
        /// SortGroupSelect - Clicked on a SortSelection Group
        /// </summary>
        ///
        public enum SelectionStateEnum { None, ElementSelect, GroupCreationSelect, ExtendSelect, ExtendSelectDrag, SortGroupSelect };

        /// <summary>
        /// Describes how the face is created
        /// </summary>
        /// <param name="File">Created from cropped file</param>
        /// <param name="PhotoCutout">Detected in a photo</param>
        public enum CreationSourceEnum { File, PhotoCutout };

        /// <summary>
        /// Indicates which bitmap to display as the face
        /// Primary - normalized face
        /// Alternate1 - Unormalized face
        /// </summary>
        public enum DisplayBitmapEnum { Primary, Alternate1 };
        private int _ID;
        private string _pathName;
        private int _photoID;               // my index within a photo
        private int _parentID;
        private int _parentGroupID;
        private int _parentPhotoID;
        private CreationSourceEnum _creationSource;

        private string _fileName;
        private Canvas _canvas;
        private Image _childImage;
        private BitmapSource _currentBitmap;    // Currently displayed bitmap
        private BitmapSource _primaryBitmap;    // Primary (Ususally a normalized bitmap)
        private BitmapSource _alternateBitmap1; // ALternate 1 perhaps an unnormalized version of the face
        private Point _leftEye;                 // Eye location in unNormalized
        private Point _rightEye;                 // Eye location in unNormalized
        private System.Windows.Shapes.Ellipse _leftEyeEllipse;
        private System.Windows.Shapes.Ellipse _rightEyeEllipse;

        private SelectionStateEnum _selectState;
        private ScaleTransform _defaultScale;
        private Dictionary<Face, double> _distanceDictionary;
        private Dictionary<int, double> _deserializeDistance;
        
        private Photo _parentPhoto;               // The photo I belong to Can be null
        private BackgroundCanvas _mainCanvas;
        private Canvas _parent;             // My parent. I always have a parent
        private Group _parentGroup;        // Parent group null if not part of a group
        private Point _mouseDownOffset;     // Offset of mouse down releative to Top Left
        private Point _mouseDownMainOffset;     // Offset of mouse down releative to MainCanvas


        private Point _targetAnimationPos;

        #region publicMethods

        /// <summary>
        /// Create a face from an image file
        /// </summary>
        /// <param name="mainCanvas">Main canvas reference</param>
        /// <param name="filename">Full path name to image file</param>
        /// <param name="ID">Unique ID</param>
        public Face(BackgroundCanvas mainCanvas, string filename, int ID)
        {

            Uri uri = new Uri("file:" + filename);
            if (false == System.IO.File.Exists(uri.LocalPath))
            {
                throw new Exception("No image file " + filename);
            }
            _primaryBitmap = (BitmapSource)new BitmapImage(uri);
            _alternateBitmap1 = null;

            _creationSource = CreationSourceEnum.File;

            Initialize(mainCanvas, filename, ID);
        }

        /// <summary>
        /// Constructor for a face part of a whole photo
        /// </summary>
        /// <param name="mainCanvas">Main canvas reference</param>
        /// <param name="filename">Full path name to image file</param>
        /// <param name="ID">Unique ID</param>
        /// <param name="bitmap">Cutout bitmap representing face from original photo</param>
        /// <param name="photoId">Unique Id of the parent photo</param>
        /// <param name="photo">Reference to parent photo</param>
        public Face(BackgroundCanvas mainCanvas, string filename, int ID, BitmapSource normalizedBitmap, BitmapSource unNormalizedBitmap, int photoId, Photo photo)
        {
            _primaryBitmap = normalizedBitmap;
            _alternateBitmap1 = unNormalizedBitmap;
            _creationSource = CreationSourceEnum.PhotoCutout;
            Initialize(mainCanvas, filename, ID);
            _photoID = photoId;
            _parentPhoto = photo;
            _parentPhotoID = photo.MyID;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mainCanvas">Main canvas reference</param>
        /// <param name="filename">Full path name to image file</param>
        /// <param name="ID">Unique ID</param>
        /// <param name="bitmap">Bitmap to use for displaying face</param>
        public Face(BackgroundCanvas mainCanvas, string filename, int ID, BitmapSource bitmap)
        {
            _primaryBitmap = bitmap;
            _alternateBitmap1 = null;
            Initialize(mainCanvas, filename, ID);
        }

        private void Initialize(BackgroundCanvas mainCanvas, string filename, int ID)
        {
            _photoID = -1;
            _parentPhoto = null;
            _ID = ID;
            _currentBitmap = _primaryBitmap;
            if (null == _currentBitmap)
            {
                _currentBitmap = _alternateBitmap1;
            }

            _mainCanvas = mainCanvas;
            _parent = mainCanvas;
            _parentGroup = null;
            _childImage = new Image();
            _childImage.Source = _currentBitmap;

            _canvas = new Canvas();
            Child = _canvas;
            _canvas.Children.Add(_childImage);
            SetCurrentBitmap();

            RenderTransform = new ScaleTransform(1.0, 1.0);
            BorderThickness = new Thickness(_mainCanvas.OptionDialog.BorderWidth);
            BorderBrush = Brushes.Black;
            Background = Brushes.White;
            if (_parentGroupID <= 0)
            {
                _parentGroupID = BackgroundCanvas.NOPARENT;
            }
            _pathName = filename;
            _fileName = System.IO.Path.GetFileName(_pathName);
            _defaultScale = new ScaleTransform(1.0, 1.0);
            _targetAnimationPos = new Point();

            MouseEnter += OnMouseEnterHandler;
            MouseLeave += OnMouseLeaveHandler;
            MouseLeftButtonDown += MouseLeftButtonDownHandler;
            MouseLeftButtonUp += MouseLeftButtonUpHandler;
            MouseRightButtonDown += MouseRightButtonDownHandler;
            MouseMove += MouseMoveEventHandler;

            _selectState = SelectionStateEnum.None;

            _canvas.Width = MyWidth;
            _canvas.Height = MyHeight;


        }
        /// <summary>
        /// Deserialization constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        /// <param name="mainCanvas">Main canvas</param>
        /// <param name="id">Unique id identifying face</param>
        public Face(SerializationInfo info, StreamingContext context, BackgroundCanvas mainCanvas, int id)
        {
            _ID = info.GetInt32("FaceID" + id.ToString());
            Deserialize(info, context, MyID, 0);

            Uri uri = new Uri("file:" + _pathName);
            if (false == System.IO.File.Exists(uri.LocalPath))
            {
                throw new Exception("No image file " + _pathName);
            }
            _primaryBitmap = (BitmapSource)new BitmapImage(uri);

            Initialize(mainCanvas, _pathName, ID);
        }

        /// <summary>
        /// Deserialize the face
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        /// <param name="deserializeID">Unique Id for the source</param>
        /// <param name="deserializeOffSet">Unique offset. Used when more than 1 face comes from the same source photo</param>
        public void Deserialize(SerializationInfo info, StreamingContext context, int deserializeID, int deserializeOffSet)
        {
            _pathName = info.GetString("_FacePathName" + deserializeID.ToString() + "_" + deserializeOffSet.ToString());
            _photoID = info.GetInt32("_FacePhotoID" + deserializeID.ToString() + "_" + deserializeOffSet.ToString());
            if (null == _parent)
            {
                _parentID = info.GetInt32("_FaceParentID" + deserializeID.ToString() + "_" + deserializeOffSet.ToString());
            }
            else
            {
                _parentID = ((IDisplayableElement)_parent).MyID;
            }
            _parentGroupID = info.GetInt32("_FaceParentGroupID" + deserializeID.ToString() + "_" + deserializeOffSet.ToString());
            _parentPhotoID = info.GetInt32("_FaceParentPhotoID" + deserializeID.ToString() + "_" + deserializeOffSet.ToString());
            Canvas.SetLeft(this, info.GetDouble("FaceLeft" + deserializeID.ToString() + "_" + deserializeOffSet.ToString()));
            Canvas.SetTop(this, info.GetDouble("FaceTop" + deserializeID.ToString() + "_" + deserializeOffSet.ToString()));

        
            //Distances - need to temporarily desirialze into ID, value
            int distanceCount = info.GetInt32("DistanceCount" + MyID.ToString());
            _deserializeDistance = new Dictionary<int, double>();

            for (int iOther = 0 ; iOther < distanceCount ; ++iOther)
            {
                int otherID = info.GetInt32("DistanceID" + MyID.ToString() + "_" + iOther.ToString());
                double dist = info.GetDouble("Distance" + MyID.ToString() + "_" + iOther.ToString());
                _deserializeDistance.Add(otherID, dist);
            }
            

        }

        /// <summary>
        /// Serialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        /// <param name="id">Unique id for case when face is deserialized from a source file 
        /// in the range [0 numFaces]. VAlue is -1 when face is deserialized by facedetection in a photo</param>
        /// <param name="serializeID">Unique serialization ID</param>
        /// <param name="serializeOffset">Unique offset. Used when more than 1 face comes from the same source photo</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context, int id, int serializeID, int serializeOffset)
        {
            if (id >= 0)
            {
                info.AddValue("FaceID" + id.ToString(), MyID);
                serializeID = MyID;
            }

            info.AddValue("_FacePathName" + serializeID.ToString() + "_" + serializeOffset.ToString(), _pathName);
            info.AddValue("_FacePhotoID" + serializeID.ToString() + "_" + serializeOffset.ToString(), _photoID);
            // This would be the first guess serialization
            //info.AddValue("_FaceParentID" + serializeID.ToString() + "_" + serializeOffset.ToString(), _parentID);
            // However when face is deserialized it is by default made a child of the mainCanvas
            info.AddValue("_FaceParentID" + serializeID.ToString() + "_" + serializeOffset.ToString(), 0);
            info.AddValue("_FaceParentGroupID" + serializeID.ToString() + "_" + serializeOffset.ToString(), _parentGroupID);
            info.AddValue("_FaceParentPhotoID" + serializeID.ToString() + "_" + serializeOffset.ToString(), _parentPhotoID);
            info.AddValue("FaceLeft" + serializeID.ToString() + "_" + serializeOffset.ToString(), Canvas.GetLeft(this));
            info.AddValue("FaceTop" + serializeID.ToString() + "_" + serializeOffset.ToString(), Canvas.GetTop(this));

            // Distance Vectors
            info.AddValue("DistanceCount"+MyID.ToString(), DistanceVector.Count);
            int iOther = 0;
            foreach (KeyValuePair<Face, double> keyValue in DistanceVector)
            {
                Face otherFace = keyValue.Key;
                info.AddValue("DistanceID"+MyID.ToString() + "_" + iOther.ToString(), otherFace.MyID.ToString());
                info.AddValue("Distance"+MyID.ToString() + "_" + iOther.ToString(), keyValue.Value);
                ++iOther;
            }
            
        }

        /// <summary>
        /// Clear out my internal lists
        /// </summary>
        public void Clear()
        {
            _distanceDictionary.Clear();
        }

        /// <summary>
        /// Add me to a group
        /// /// </summary>
        /// <param name="parentGroup">My new parent</param>
        /// <param name="maxEntent">Optional extent of parent</param>
        public void AddToGroup(Canvas parentGroup, Rect maxEntent)
        {
            RemoveFromGroup(parentGroup);
            RenderTransform = _defaultScale;

            if (null != maxEntent)
            {
                double top = Canvas.GetTop(this);
                double left = Canvas.GetLeft(this);
                Canvas.SetTop(this, top - maxEntent.Top);
                Canvas.SetLeft(this, left - maxEntent.Left);
            }
        }

        /// <summary>
        /// Set which bitmap to display as representative
        /// of this face. 
        /// Note that if the requested bitmap does not exist we search for the 
        /// first non null bitmap
        /// </summary>
        /// <param name="displayBitMap">Bitmap Type to display</param>
        public void SetCurrentBitmap(DisplayBitmapEnum displayBitMap)
        {
            switch (displayBitMap)
            {
                case DisplayBitmapEnum.Primary:
                    _currentBitmap = _primaryBitmap;
                    break;

                case DisplayBitmapEnum.Alternate1:
                    _currentBitmap = _alternateBitmap1;
                    break;

                default:
                    _currentBitmap = _primaryBitmap;
                    break;
            }

            if (null == _currentBitmap)
            {
                if (null != _primaryBitmap)
                {
                    _currentBitmap = _primaryBitmap;
                }
                else if (null != _alternateBitmap1)
                {
                    _currentBitmap = _alternateBitmap1;
                }
            }
            if (null != _childImage)
            {
                _childImage.Source = _currentBitmap;
            }
        }

        /// <summary>
        /// Set which bitmap to display based on current optionsDisalog
        /// </summary>
        public void SetCurrentBitmap()
        {
            if (null != _mainCanvas && null != _mainCanvas.OptionDialog)
            {
                SetCurrentBitmap(_mainCanvas.OptionDialog.FaceDisplayBitmap);
            }
        }

        /// <summary>
        /// Unhook me from my parent. If my parent is a group
        /// hook me back up to the mainCanvas
        /// </summary>
        public void RemoveFromGroup(Canvas newParent)
        {
            if (null != _parent)
            {
                ((IFaceContainer)_parent).RemoveFace(this, false);
            }
            ((IFaceContainer)newParent).AddFace(this);

            if (newParent.GetType() == typeof(Group))
            {
                _parentGroup = (Group)newParent;
            }
            else
            {
                _parentGroup = null;
            }

            _parent = newParent;
        }

        /// <summary>
        /// Return the distance from me to another face. Distances are computed
        /// by the backend and stored by the UI
        /// </summary>
        /// <param name="otherFace">Other face</param>
        /// <returns>Distance</returns>
        public double GetDistanceToFace(Face otherFace)
        {
            if (DistanceVector.ContainsKey(otherFace))
            {
                return DistanceVector[otherFace];
            }
            return _mainCanvas.OptionDialog.DefaultMaxDistance;
        }

        /// <summary>
        /// How the face was created
        /// </summary>
        public CreationSourceEnum CreationSource
        {
            get
            {
                return _creationSource;
            }
        }

        /// <summary>
        /// Get/set the unormalized eyed location
        /// </summary>
        public Point LeftEye
        {
            get
            {
                return _leftEye;
            }
            set
            {
                _leftEye = value;
            }
        }

        /// <summary>
        /// Get/set the unormalized eyed location
        /// </summary>
        public Point RightEye
        {
            get
            {
                return _rightEye;
            }
            set
            {
                _rightEye = value;
            }
        }

        /// <summary>
        /// Set if the eys are visible
        /// </summary>
        public bool ShowEyes
        {
            set
            {
                if (true == value && LeftEye.X > 0 && LeftEye.Y > 0 && _currentBitmap != null)
                {
                    if (null == _leftEyeEllipse)
                    {
                        _leftEyeEllipse = new System.Windows.Shapes.Ellipse();
                        _leftEyeEllipse.Stroke = Brushes.Red;
                        _leftEyeEllipse.Width = _mainCanvas.OptionDialog.EyeDiameter * MyWidth;
                        _leftEyeEllipse.Height = _mainCanvas.OptionDialog.EyeDiameter * MyWidth;
                        _leftEyeEllipse.HorizontalAlignment = HorizontalAlignment.Center;
                        _leftEyeEllipse.VerticalAlignment = VerticalAlignment.Center;

                        _canvas.Children.Add(_leftEyeEllipse);
                    }
                    if (null == _rightEyeEllipse)
                    {
                        _rightEyeEllipse = new System.Windows.Shapes.Ellipse();
                        _rightEyeEllipse.Stroke = Brushes.Red;
                        _rightEyeEllipse.Width = _mainCanvas.OptionDialog.EyeDiameter * MyWidth;
                        _rightEyeEllipse.Height = _mainCanvas.OptionDialog.EyeDiameter * MyWidth;
                        _rightEyeEllipse.HorizontalAlignment = HorizontalAlignment.Center;
                        _rightEyeEllipse.VerticalAlignment = VerticalAlignment.Center;
                        _canvas.Children.Add(_rightEyeEllipse);
                    }

                    double eyeRadius = _mainCanvas.OptionDialog.EyeDiameter / 2.0 * MyWidth;
                    if (_currentBitmap == _primaryBitmap)
                    {
                        Canvas.SetLeft(_leftEyeEllipse, _mainCanvas.OptionDialog.NormalizeLeftEyeLocation.X - eyeRadius);
                        Canvas.SetTop(_leftEyeEllipse, _mainCanvas.OptionDialog.NormalizeLeftEyeLocation.Y - eyeRadius);
                        Canvas.SetLeft(_rightEyeEllipse, _mainCanvas.OptionDialog.NormalizeRightEyeLocation.X - eyeRadius);
                        Canvas.SetTop(_rightEyeEllipse, _mainCanvas.OptionDialog.NormalizeRightEyeLocation.Y - eyeRadius);
                    }
                    else
                    {
                        Canvas.SetLeft(_leftEyeEllipse, LeftEye.X - eyeRadius);
                        Canvas.SetTop(_leftEyeEllipse, LeftEye.Y - eyeRadius);
                        Canvas.SetLeft(_rightEyeEllipse, RightEye.X - eyeRadius);
                        Canvas.SetTop(_rightEyeEllipse, RightEye.Y - eyeRadius);
                    }
                    _leftEyeEllipse.Visibility = Visibility.Visible;
                    _rightEyeEllipse.Visibility = Visibility.Visible;
                    Canvas.SetZIndex(_leftEyeEllipse, 50);
                }
                else 
                {
                    if (null != _leftEyeEllipse)
                    {
                        _leftEyeEllipse.Visibility = Visibility.Hidden;
                    }
                    if (null != _leftEyeEllipse)
                    {
                        _rightEyeEllipse.Visibility = Visibility.Hidden;
                    }
                }

            }
        }
        /// <summary>
        /// Test if a point lies inside the face
        /// </summary>
        /// <param name="testPoint"></param>
        /// <returns></returns>
        public bool HitTest(Point testPoint)
        {
            Point topLeft = GetPositionRelativeToCanvas();
            Rect rect = new Rect(topLeft.X, topLeft.Y, MyWidth, MyHeight);

            //Check Top Left
            if (testPoint.X >= rect.X &&
                testPoint.X < rect.Right &&
                testPoint.Y >= rect.Top &&
                testPoint.Y < rect.Bottom)
            {
                return true;
            }

            return false;

        }


        /// <summary>
        /// Get true if face is within a given rectangle
        /// </summary>
        /// <param name="rect">The rectangle to test</param>
        /// <returns>true if face is part of the rectangle</returns>
        public bool HitTest(Rect rect)
        {
            Point pos = GetPositionRelativeToCanvas();

            //Check Top Left
            if (pos.X >= rect.X &&
                pos.X < rect.Right &&
                pos.Y >= rect.Top &&
                pos.Y < rect.Bottom)
            {
                return true;
            }

            // Check Bottom Right
            pos.X += ImageWidth;
            pos.Y += ImageHeight;

            if (pos.X >= rect.X &&
                pos.X < rect.Right &&
                pos.Y >= rect.Top &&
                pos.Y < rect.Bottom)
            {
                return true;
            }
            return false;

        }
        /// <summary>
        /// Get the unique id for the face
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }
        }

        /// <summary>
        /// Get reference to my parent photo if any
        /// </summary>
        public Photo MyPhoto
        {
            get
            {
                return _parentPhoto;
            }
        }
        /// <summary>
        /// Get the display width of my face
        /// </summary>
        public double MyWidth
        {
            get
            {
                return (ActualWidth > 0) ? ActualWidth : ImageWidth;
            }
        }
        /// <summary>
        /// Get the display Height of my face
        /// </summary>
        public double MyHeight
        {
            get
            {
                return (ActualHeight > 0) ? ActualHeight : ImageHeight;

            }
        }

        /// <summary>
        /// Get/Set the final topLeft position after a move.
        /// </summary>
        public Point TargetAnimationPos
        {
            get
            {
                return _targetAnimationPos;
            }
            set
            {
                _targetAnimationPos = value;
            }
        }
        /// <summary>
        /// Get/Set the final left edge after a move
        /// </summary>
        public double TargetAnimationPos_X
        {
            get
            {
                return _targetAnimationPos.X;
            }
            set
            {
                _targetAnimationPos.X = value;
            }
        }
        /// <summary>
        /// Get/Set the final Top edge after a move
        /// </summary>
        public double TargetAnimationPos_Y
        {
            get
            {
                return _targetAnimationPos.Y;
            }
            set
            {
                _targetAnimationPos.Y = value;
            }
        }

        /// <summary>
        /// Get the index of my face in my parent photo
        /// </summary>
        public int PhotoIdx
        {
            get
            {
                if (null != MyPhoto)
                {

                    return _photoID;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Gets or sets the face selection mode
        /// </summary>
        public SelectionStateEnum Selected
        {
            get
            {
                return _selectState;
            }

            set
            {
                _selectState = value;
                if (_selectState == SelectionStateEnum.None)
                {
                    BorderBrush = System.Windows.Media.Brushes.Black;
                }
                else
                {
                    BorderBrush = System.Windows.Media.Brushes.Gold;
                }
            }
        }

        /// <summary>
        /// Get the width of the underlying image in pixels
        /// </summary>
        public int ImageWidth
        {
            get
            {
                return _currentBitmap.PixelWidth;
            }
        }


        /// <summary>
        /// Get the height of the underlying image in pixels
        /// </summary>
        public int ImageHeight
        {
            get
            {
                return _currentBitmap.PixelHeight;
            }
        }

        /// <summary>
        /// Gets or set the vector of distances to other faces in the collection
        /// </summary>
        public Dictionary<Face,double> DistanceVector
        {
            get
            {
                if (null == _distanceDictionary)
                {
                    _distanceDictionary = new Dictionary<Face, double>();
                }
                return _distanceDictionary;
            }
        }

        /// <summary>
        /// Access the image raw data as a byte array
        /// Length of array is width*height
        /// </summary>
        public byte[] ByteData
        {
            get
            {

                PixelFormat pixFormat = _currentBitmap.Format;
                int numPix = _currentBitmap.PixelHeight * _currentBitmap.PixelWidth;
                int bytePerPixel = pixFormat.BitsPerPixel / 8;
                int cTotByte = numPix * bytePerPixel;
                int stride = _currentBitmap.PixelWidth * bytePerPixel;
                int pixPerColor = Math.Min(3, bytePerPixel);

                if (cTotByte <= 0)
                {
                    return null;
                }

                byte[] rgbData = new byte[cTotByte];
                _currentBitmap.CopyPixels(rgbData, stride, 0);

                byte [] dataBuf = new byte[numPix];
                int iNext = 0;
                for (int iRow = 0; iRow < _currentBitmap.PixelHeight; ++iRow)
                {
                    int ir = iRow * stride;
                    for (int iCol = 0; iCol < _currentBitmap.PixelWidth; ++iCol)
                    {
                        int val = 0;
                        for (int iByte = 0; iByte < pixPerColor; ++iByte)
                        {
                            val += rgbData[ir + iByte];
                        }

                        dataBuf[iNext++] = (byte)(val / pixPerColor);
                        ir += bytePerPixel;
                    }
                }
                return dataBuf;

            }
        }

        public bool IsGrouped
        {
            get
            {
                return (_parentGroup == null) ? false : true;
            }
        }

        /// <summary>
        /// Check if the point appears before the Top left edge of
        /// me when as viewed in in scan order
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsDisplayedBefore(Point pos)
        {
            double y = Canvas.GetTop(this);
            double h = ImageHeight / 4.0 + _mainCanvas.OptionDialog.GridFaceSpace;

            if (y + h < pos.Y)
            {
                return true;
            }

            if (y - h < pos.Y && y + h >= pos.Y)
            {
                double x = Canvas.GetLeft(this);

                if (x < pos.X)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Get the filenname associated with the face. Could be either the
        /// filename from a cropped face file or the file name of the photo in
        /// which a face is detected.
        /// </summary>
        public String FileName
        {
            get
            {
                return _fileName;
            }
        }

        #region IDisplayableElementImplementation
        /// <summary>
        /// Get a reference to my parent canvas
        /// </summary>
        public IDisplayableElement MyParent
        {
            get
            {
                return (IDisplayableElement)_parent;
            }
        }
        /// <summary>
        /// Get my unique ID (inherited form IDisplayableElement
        /// </summary>
        public int MyID
        {
            get
            {
                return _ID;
            }
        }
        /// <summary>
        /// Ensure that parentID is synced to current parent 
        /// Should sync before serializing
        /// </summary>
        public void SyncParentID()
        {
            if (null != _parent)
            {
                _parentID = ((IDisplayableElement)_parent).MyID;
            }
            else
            {
                _parentID = BackgroundCanvas.NOPARENT;
            }

            if (null != _parentGroup)
            {
                _parentGroupID = _parentGroup.MyID;
            }
            else
            {
                _parentGroupID = BackgroundCanvas.NOPARENT;
            }

            if (null != _parentPhoto)
            {
                _parentPhotoID = _parentPhoto.MyID;
            }
            else
            {
                _parentPhotoID = BackgroundCanvas.NOPARENT;
            }
        }


        /// <summary>
        /// Return my position relative to the main canvas
        /// </summary>
        /// <returns>Location </returns>
        public Point GetPositionRelativeToCanvas()
        {
            return _mainCanvas.GetPositionRelativeToCanvas(this);
        }
        /// <summary>
        /// Rebuild my parent hierachy, typically following deserialization
        /// </summary>
        /// <param name="backgroundCanvas"></param>
        public void RebuildTree(BackgroundCanvas backgroundCanvas)
        {
            _mainCanvas = backgroundCanvas;
            _parentPhoto = (Photo)_mainCanvas.FindParent(_parentPhotoID);
            _parent = (Canvas)_mainCanvas.FindParent(_parentID);
            _parentGroup = (Group)_mainCanvas.FindParent(_parentGroupID);
            if (null != _parentGroup)
            {
                AddToGroup(_parentGroup, _parentGroup.ExtentRect);
                _parentGroup.Display();
            }

            // Rebuild the real distance vector from a desirialized version if any
            if (null != _deserializeDistance)
            {
                _distanceDictionary = new Dictionary<Face, double>();
                foreach (KeyValuePair<int, double> keyVal in _deserializeDistance)
                {
                    Face otherFace = (Face)_mainCanvas.FindParent(keyVal.Key);
                    _distanceDictionary.Add(otherFace, keyVal.Value);
                }

                _deserializeDistance.Clear();
            }

            SetCurrentBitmap();
        }

        /// <summary>
        /// Sync with options dialog
        /// - Set currently displayed bitmap
        /// </summary>
        public void SyncDisplayOptions()
        {
            SetCurrentBitmap();
            ShowEyes = _mainCanvas.OptionDialog.IsShowEyes;
        }

        #endregion IDisplayableElementImplementation
        #endregion publicmethods

        #region privatemethods


        private void AddDistancesToDebug()
        {
            if (_mainCanvas.IsDebugVisible == true)
            {
                _mainCanvas.DebugPanelListBoxHeader = _fileName;

                GridView gridView = (GridView)_mainCanvas.DebugPanelListView.View;

                foreach (GridViewColumn col in gridView.Columns)
                {
                    if (col.Header.ToString() == "FaceName")
                    {
                        col.DisplayMemberBinding = new System.Windows.Data.Binding("FaceName");
                    }
                    else if (col.Header.ToString() == "Distance")
                    {
                        col.DisplayMemberBinding = new System.Windows.Data.Binding("Distance");
                    }
                }

                _mainCanvas.DebugPanelListView.Items.Clear();

                foreach (KeyValuePair<Face, double> keyValue in DistanceVector)
                {

                    _mainCanvas.DebugPanelListView.Items.Add(new FaceDistance(keyValue.Key, keyValue.Value));
                }

            }
        }
        private void OnMouseEnterHandler(object sender, MouseEventArgs e)
        {
            BitmapEffect = _mainCanvas.GlowEffect;

            if (null != MyPhoto)
            {
                MyPhoto.MakeVisible(this);
            }

            if (_mainCanvas.IsDebugVisible == true)
            {
                _mainCanvas.DebugPanelListBoxHeader = _fileName;
            }

            if (null != _parentGroup)
            {
                _parentGroup.ShowSplitIcon(this);
            }
        }

        private void OnMouseLeaveHandler(object sender, MouseEventArgs e)
        {
            BitmapEffect = null;
            _mainCanvas.DebugPanelListBoxHeader = "";
            if (null != MyPhoto)
            {
                MyPhoto.MakeInvisible();
            }
            if (null != _parentGroup)
            {
                Point pos = e.GetPosition(_mainCanvas);
                if (false == HitTest(pos))
                {
                    _parentGroup.HideSplitIcon(this);
                }
            }
        }
        /// <summary>
        /// Cache mouse data at the start of a selection
        /// </summary>
        private void CacheMouseStartSelection(MouseButtonEventArgs e)
        {
            _mouseDownMainOffset = e.GetPosition(_mainCanvas);

            Point pos = GetPositionRelativeToCanvas();

            _mouseDownOffset.X = _mouseDownMainOffset.X - Canvas.GetLeft(this);
            _mouseDownOffset.Y = _mouseDownMainOffset.Y - Canvas.GetTop(this);

            _mouseDownOffset.X = _mouseDownMainOffset.X - pos.X;
            _mouseDownOffset.Y = _mouseDownMainOffset.Y - pos.Y;

            _mainCanvas.MouseMove += MouseMoveEventHandler;
            _mainCanvas.MoveToFrontDisplayOrder(this);

        }
        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (System.Windows.Input.ModifierKeys.Control == System.Windows.Input.Keyboard.Modifiers
                && Selected == SelectionStateEnum.ExtendSelectDrag)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                if (null != _parentGroup)
                {
                    _parentGroup.SortOnFace(this);
                    e.Handled = true;
                }
                else
                {
                    Selected = SelectionStateEnum.ElementSelect;
                    _mainCanvas.DoUpdateLayout();
                }
                return;
            }

            CacheMouseStartSelection(e);
            if (System.Windows.Input.ModifierKeys.Control == System.Windows.Input.Keyboard.Modifiers &&
                SelectionStateEnum.ExtendSelectDrag != Selected)
            {
                Selected = SelectionStateEnum.ExtendSelect;
                if (BackgroundCanvas.SelectState.None == _mainCanvas.SelectionState)
                {
                    _mainCanvas.SelectionState = BackgroundCanvas.SelectState.ExtendElementSelect;
                }
            }
            else
            {
                Selected = SelectionStateEnum.ElementSelect;
                _mainCanvas.SelectionState = BackgroundCanvas.SelectState.Element;
                AddDistancesToDebug();
            }
        }

        /// <summary>
        /// Completes movement of afae. Required to fix the final location
        /// of teh face
        /// </summary>
        /// <param name="sender">Caller</param>
        /// <param name="e">params</param>
        public void DisplayCompleteHandler(Object sender, EventArgs e)
        {
            Canvas.SetTop(this, _targetAnimationPos.Y);
            Canvas.SetLeft(this, _targetAnimationPos.X);
        }

        /// <summary>
        /// Handle the end of a face move by restablishing group hierarchy
        /// and selection state. Does the replumbing if a face moves in/out of groups
        /// NOTE: When a face moves in/out of a group the rendered appearance of the 
        /// group changes which can involve moving other faces. This can give undesirable side
        /// effects particulalry in extendedSlect moves, so the input parameter 
        /// can suppress the adjustment
        /// 
        /// </summary>
        /// <param name="doUpdateDisplay">true - Automatically adjust groups on face transitions</param>
        public void HandleMoveCompletion(bool doUpdateDisplay)
        {
            if (Selected == SelectionStateEnum.None)
            {
                return;
            }

            // Check if have moved out of a group
            if (_parentGroup != null)
            {
                if (false == HitTest(_parentGroup.ExtentRect))
                {
                    Group oldParent = _parentGroup;
                    Point pos = GetPositionRelativeToCanvas();
                    RemoveFromGroup(_mainCanvas);
                    Canvas.SetLeft(this, pos.X);
                    Canvas.SetTop(this, pos.Y);
                    if (true == doUpdateDisplay)
                    {
                        oldParent.Display();
                    }
                }
                else
                {
                    if (true == doUpdateDisplay)
                    {
                        _parentGroup.Reposition(this);
                        _parentGroup.Display();
                    }
                }
            }

            // Check if moved into a new group
            if (_parentGroup == null)
            {
                for (int iGroup = 0; iGroup < _mainCanvas.GroupCount; ++iGroup)
                {
                    Group group = _mainCanvas.GetGroup(iGroup);

                    if (null != group && true == HitTest(group.ExtentRect))
                    {
                        AddToGroup(group, group.ExtentRect);
                        if (true == doUpdateDisplay)
                        {
                            group.Display();
                        }
                        break;
                    }
                }
            }
            Selected = SelectionStateEnum.None;
            _mainCanvas.MouseMove -= MouseMoveEventHandler;
            _mainCanvas.ResetDisplayOrder();
        }

        private void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            bool hasMoved = _mainCanvas.HasMoved(_mouseDownMainOffset, e.GetPosition(_mainCanvas));
                
                // Handle Extended selection
            if (System.Windows.Input.ModifierKeys.Control == System.Windows.Input.Keyboard.Modifiers)
            {
                if (true == hasMoved)
                {
                    _mainCanvas.PropogateMoveCompletionToExtendSelection();
                    _mainCanvas.ClearExtendedFaceSelection();
                    _mainCanvas.SelectionState = BackgroundCanvas.SelectState.None;
                    Selected = SelectionStateEnum.None;
                }
                else
                {
                    // Toggle already selected
                    if (Selected == SelectionStateEnum.ExtendSelectDrag)
                    {
                        _mainCanvas.MouseMove -= MouseMoveEventHandler;
                        _mainCanvas.RemoveExtendedFaceSelection(this);
                        Selected = SelectionStateEnum.None;
                    }
                    else
                    {
                        Selected = SelectionStateEnum.ExtendSelectDrag;
                        _mainCanvas.AddToExtendFaceSelection(this);
                        CacheMouseStartSelection(e);
                        _mainCanvas.SelectionState = BackgroundCanvas.SelectState.ExtendElementDrag;
                    }
                }
            }
            else if (SelectionStateEnum.ElementSelect == Selected && null != _parentGroup && false == hasMoved)
            {
                _parentGroup.FaceSortMove(this);
            }
            else
            {
                // Regular selection
                if (true == hasMoved)
                {
                    HandleMoveCompletion(true);
                    _mainCanvas.SelectionState = BackgroundCanvas.SelectState.None;
                }
            }

        }

        private void ShowContextMenu()
        {
            FaceContextMenu menu = new FaceContextMenu();

            for (int iCount = 0; iCount < menu.Items.Count; ++iCount)
            {
                MenuItem it = (MenuItem)menu.Items[iCount];

                switch (it.Name)
                {
                    case "Resort":
                        if (DistanceVector.Count <= 0)
                        {
                            it.IsEnabled = false;
                        }
                        it.Click += MenuResortHandler;
                        break;

                    case "Move":
                        if (_parentGroup.DisplayMode == Group.DisplayState.SortSplit)
                        {
                            it.Click += MenuMoveHandler;
                        }
                        else
                        {
                            it.IsEnabled = false;
                        }
                        break;

                    case "MoveAll":
                        if (_parentGroup.DisplayMode == Group.DisplayState.SortSplit)
                        {
                            it.Click += MenuMoveAllHandler;
                        }
                        else
                        {
                            it.IsEnabled = false;
                        }
                        break;

                    case "SplitAndMove":
                        if (_parentGroup.DisplayMode == Group.DisplayState.SortSplit)
                        {
                            it.IsEnabled = false;
                        }
                        else
                        {
                            it.Click += MenuSplitAndMove;
                        }
                        break;

                    default:
                        break;
                }

            }
            menu.IsOpen = true;
        }

        private void MouseRightButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (null != _parentGroup)
            {
                ShowContextMenu();
                e.Handled = true;
            }
        }


        private void MenuResortHandler(Object sender, RoutedEventArgs e)
        {
            if (null != _parentGroup)
            {
                _parentGroup.SortOnFace(this);
            }
       }

        private void MenuMoveHandler(Object sender, RoutedEventArgs e)
        {
            if (null != _parentGroup)
            {
                _parentGroup.FaceSortMove(this);
            }
        }
        private void MenuMoveAllHandler(Object sender, RoutedEventArgs e)
        {
            if (null != _parentGroup)
            {
                _parentGroup.FaceSortMoveAll(this);
            }
        }

        /// <summary>
        /// Create a Sort group and performa split at the location of my face
        /// FAce must be in a group.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void MenuSplitAndMove(Object sender, RoutedEventArgs e)
        {
            if (null != _parentGroup )
            {
                _parentGroup.HideSplitIcon(this);
                _mainCanvas.AddSplitGroup(_parentGroup);
                _parentGroup.FaceSortMoveAll(this);
                _mainCanvas.RemoveSplitGroup(_parentGroup);
            }

        }
        private void MouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            if (SelectionStateEnum.SortGroupSelect == Selected)
            {
                return;
            }

            // Move me if my ste is "draggable" ie I am part of an extended select or single select
            if (e.LeftButton == MouseButtonState.Pressed &&
                (SelectionStateEnum.ElementSelect == _selectState || SelectionStateEnum.ExtendSelectDrag == _selectState ||
                    SelectionStateEnum.ExtendSelect == _selectState) &&
                (_mainCanvas.SelectionState == BackgroundCanvas.SelectState.Element ||
                 _mainCanvas.SelectionState == BackgroundCanvas.SelectState.ExtendElementDrag ||
                 _mainCanvas.SelectionState == BackgroundCanvas.SelectState.ExtendElementSelect))
            {
                Point mouseAbsPos = e.GetPosition(_mainCanvas);
                Point mousePos = e.GetPosition(_parent);

                // Highlight groups I enter
                _mainCanvas.HighlightGroups(mouseAbsPos, _parentGroup);

                // Make siure that the Split bar does not follow me
                if (null != _parentGroup)
                {
                    _parentGroup.HideSplitIcon(this);
                }


                Canvas.SetLeft(this, mousePos.X - _mouseDownOffset.X);
                Canvas.SetTop(this, mousePos.Y - _mouseDownOffset.Y);

                // Ok turn me into "drag" ste if I was part of an extended selection
                if (SelectionStateEnum.ExtendSelect == _selectState)
                {
                    _mainCanvas.SelectionState = BackgroundCanvas.SelectState.ExtendElementDrag;
                    Selected = SelectionStateEnum.ExtendSelectDrag;
                    _mainCanvas.AddToExtendFaceSelection(this);
                }
            }
        }

        #endregion
    }
}
