using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FaceSortUI
{
    /// <summary>
    /// An element of data exchange betwwen UI and backend. Typically an element 
    /// holds information about a face, such as its imagedata, Distance to other faces, etc
    /// 
    /// </summary>
    public class ExchangeElement
    {
        public enum ElementState { FreeLocation, FrozenLocation, Selected};

        private int _idx; // Original index

        private byte [] _byteData;
        private int _width;
        private int _height;
        double _left;
        double _top;
        private ElementState _state;


        /// <summary>
        /// Constructor. Typically the UI constructs an element
        /// </summary>
        /// <param name="idx">Unique id for the elemenet</param>
        /// <param name="data">Image data as a byte vector</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image Height in pixels</param>
        public ExchangeElement(int idx, byte [] data, int width, int height)
        {
            _idx = idx;
            _byteData = data;
            _width = width;
            _height = height;
            _state = ElementState.FreeLocation;
        }

    /// <summary>
    /// Elements ID
    /// </summary>
       public int ID
       {
           get
           {
               return _idx;
           }
       }

        /// <summary>
        /// Copy of original image byte data as a vector
        /// </summary>
        public byte[] ByteData
        {
            get
            {
                return _byteData;
            }
        }

        /// <summary>
        /// Element top
        /// </summary>
        public double Top 
        {
            set 
            {
                _top = value;
            }
            get
            {
                return _top;
            }
        }

        /// <summary>
        /// Element Left edge
        /// </summary>
        public double Left
        {
            set
            {
                _left = value;
            }
            get
            {
                return _left;
            }
        }
        /// <summary>
        /// Element Width
        /// </summary>
        public int Width
        {
            get
            {
                return _width;
            }
        }

        /// <summary>
        /// Element height
        /// </summary>
        public int Height
        {
            get
            {
                return _height;
            }
        }

        public ElementState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        /// <summary>
        /// Access the raw image data as a 2 dim byte matrix
        /// </summary>
        public Byte[,] ByteDataMatrix
        {
            get
            {
                return ByteStreamToArray2(ByteData, _width, _height);
            }
        }

        /// <summary>
        /// Access the raw image data as a 2 dim double matrix
        /// </summary>
        public double[,] DoubleDataMatrix
        {
            get
            {
                return ByteStreamToDoubleArray2(ByteData, _width, _height);
            }
        }

        /// <summary>
        /// Returns the image data as a 2D array. 
        /// </summary>
        /// <param name="width">Image Width</param>
        /// <param name="height">Image Height</param>
        /// <returns></returns>
        private double[,] GetDoubleDataMatrix(int width, int height)
        {
            return ByteStreamToDoubleArray2(ByteData, width, height);
        }
        
        private Byte[,] ByteStreamToArray2(Byte[] Data, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            double downSampX = _width / width;
            double downSampY = _height / height;

            if (downSampX <= 0.0 || downSampY <= 0.0)
            {
                return null;
            }

            Byte[,] mat = new Byte[height, width];

            for (int iRow = 0; iRow < height; ++iRow)
            {
                int rowStart = (int)Math.Round(iRow * downSampY);

                for (int iCol = 0; iCol < width; ++iCol)
                {
                    mat[iRow, iCol] = Data[rowStart + (int)Math.Round(iCol * downSampX)];
                }
            }
            return mat;
        }

        private double[,] ByteStreamToDoubleArray2(Byte[] Data, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            double downSampX = _width / width;
            double downSampY = _height / height;

            if (downSampX <= 0.0 || downSampY <= 0.0)
            {
                return null;
            }

            double[,] mat = new double[height, width];

            for (int iRow = 0; iRow < height; ++iRow)
            {
                int rowStart = (int)Math.Round(iRow * downSampY * width);

                for (int iCol = 0; iCol < width; ++iCol)
                {
                    mat[iRow, iCol] = Data[rowStart + (int)Math.Round(iCol * downSampX)];
                }
            }
            return mat;
        }

    }
    /// <summary>
    /// Enables data exchange between UI and backend
    /// </summary>
    public class DataExchange
    {
#region memberVariables

        private DateTime _creationTime;
        private List<Face> _faceList;
        private List<ExchangeElement> _elements;
        private string _backEndConfigFile;
        double[,] _distanceMatrix;
        double[,] _coordinates;
        Rect _dataRect;       // Range of Co-ordinates

        private static int _dim = 2;

#endregion memberVariables

#region PublicMethods
        /// <summary>
        /// Constructor. Typically called by the UI
        /// </summary>
        /// <param name="faceList">List of faces to include</param>
        public DataExchange(List<Face> faceList, Rect dataRect, OptionDialog optionDialog)
        {
            _creationTime = DateTime.UtcNow;
            _elements = new List<ExchangeElement>();
            _faceList = faceList;
            bool doDistanceMatrix = true;
            _coordinates = new double[_faceList.Count, _dim];
            _dataRect = new Rect(dataRect.X, dataRect.Y, dataRect.Width, dataRect.Height);
            _backEndConfigFile = optionDialog.BackEndConfigFile;

            int idx = 0;

            foreach (Face face in faceList)
            {
                ExchangeElement ele = new ExchangeElement(idx, face.ByteData, face.ImageWidth, face.ImageHeight);
                Point location;

                if (Face.SelectionStateEnum.ElementSelect == face.Selected ||
                    Face.SelectionStateEnum.ExtendSelectDrag == face.Selected)
                {
                    System.Diagnostics.Debug.Assert(false == face.IsGrouped, "Data exchange found a grouped + Selected face " + face.FileName);
                    ele.State = ExchangeElement.ElementState.Selected;
                }

                if (true == face.IsGrouped)
                {
                    ele.State = ExchangeElement.ElementState.FrozenLocation;
                    location = face.MyParent.GetPositionRelativeToCanvas();
                }
                else
                {
                    location = face.GetPositionRelativeToCanvas();
                }

                _elements.Add(ele);
                ele.Left = location.X;
                ele.Top = location.Y;
                _coordinates[idx, 0] = location.X;
                _coordinates[idx, 1] = location.Y;

                if (null == face.DistanceVector || face.DistanceVector.Count != faceList.Count)
                {
                    doDistanceMatrix = false;
                }
                ++idx;
            }

            if (true == doDistanceMatrix && _faceList.Count > 0)
            {
                _distanceMatrix = new double[_faceList.Count, _faceList.Count];
                idx = 0;
                foreach (Face face in _faceList)
                {
                    int idxOther = 0;

                    foreach (Face faceOther in faceList)
                    {
                        _distanceMatrix[idx, idxOther++] = face.GetDistanceToFace(faceOther);
                    }
                    ++idx;
                }
            }

        }

        /// <summary>
        /// Returns number of faces available
        /// </summary>
        public int ElementCount
        {
            get
            {
                return _elements.Count;
            }
        }

        /// <summary>
        /// Distance matrix between all elements. Can be null
        /// </summary>
        public double[,] DistanceMatrix
        {
            get
            {
                return _distanceMatrix;
            }

            set
            {
                if (null == value)
                {
                    _distanceMatrix = null;
                }
                else
                {
                    _distanceMatrix = new double[value.GetLength(0), value.GetLength(1)];
                    for (int i = 0; i < value.GetLength(0); ++i)
                    {
                        for (int j = 0; j < value.GetLength(1); ++j)
                        {
                            _distanceMatrix[i, j] = value[i, j];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get Config File for Backend. This is a copy of the one 
        /// in from the Optiondialog
        /// </summary>
        public string BackEndConfigFile
        {
            get
            {
                return _backEndConfigFile;
            }

        }

        /// <summary>
        /// Returns a particular face
        /// </summary>
        /// <param name="idx">Face Id</param>
        /// <returns>Requested face or null if index is out of range</returns>
        public ExchangeElement GetElement(int idx)
        {
            if (idx >= 0 && idx < ElementCount)
            {
                return _elements[idx];
            }

            return null;
        }

        /// <summary>
        /// Returns the List of faces
        /// </summary>
        public List<Face> FaceList
        {
            get
            {
                return _faceList;
            }
        }

        /// <summary>
        /// Locations of elements in the dataexchange. 
        /// Coordinate[i,0] - Is Top left edge of element i
        /// Coordinate[i,1] - Is Top edge of element i
        /// </summary>
        public double[,] Coordinates
        {
            get
            {
                return _coordinates;
            }

            set
            {
                _coordinates = new double[value.GetLength(0), value.GetLength(1)];
                for (int i = 0; i < value.GetLength(0); ++i)
                {
                    for (int j = 0; j < value.GetLength(1); ++j)
                    {
                        _coordinates[i,j] = value[i,j];
                    }
                }
            }
        }

        /// <summary>
        /// Get the Bounding rect of elements
        /// </summary>
        public Rect DataRect
        {
            get
            {
                return _dataRect;
            }
            set
            {
                _dataRect = new Rect(value.X, value.Y, value.Width, value.Height);
            }
        }

        /// <summary>
        /// Rescale current Coordinates to fall in a specified range
        /// </summary>
        /// <param name="xMin">Minimum X</param>
        /// <param name="xMax">Maximum X</param>
        /// <param name="yMin">Minimum Y</param>
        /// <param name="yMax">Maximum Y</param>
        public void RescaleCoordinates(double xMin, double xMax, double yMin, double yMax)
        {

            if (null == _coordinates)
            {
                return;
            }

            if (xMax <= xMin || yMax <= yMin)
            {
                throw new Exception("Bad scaling range supplied - recsale is ignored");
            }

            if (xMin == _dataRect.X && yMin == _dataRect.Y &&
                (xMax - xMin) == _dataRect.Width &&
                (yMax - yMin) == _dataRect.Height)
            {
                return;
            }

            double xScale = (xMax - xMin) / _dataRect.Width;
            double yScale = (yMax - yMin) / _dataRect.Height;

            for (int i = 0; i < _coordinates.GetLength(0); ++i)
            {
                _coordinates[i, 0] = (_coordinates[i, 0] - _dataRect.X) * xScale + xMin;
                _coordinates[i, 1] = (_coordinates[i, 1] - _dataRect.Y) * yScale + yMin;
            }
        }

#endregion

    }
}
