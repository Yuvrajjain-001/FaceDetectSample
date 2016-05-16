

using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

using Dpu.Utility;

using PixelType = System.Single;

namespace Dpu.ImageProcessing
{
    public class ImagePixelEnumerator : IEnumerator
    {
        int _offset = 0;
        PixelType[] _pixels;
        Image _image;

        public ImagePixelEnumerator(Image im)
        {
            _offset = -1;
            _image = im;
            _pixels = _image.Pixels;
        }

        public object Current
        {
            get { return _pixels[_offset]; }
        }

        public bool MoveNext()
        {
            if (_offset < _pixels.Length - 1)
            {
                _offset++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            _offset = -1;
        }

    }


    interface IImage
    {
        int Height
        {
            get;
        }
        int Width
        {
            get;
        }
        Rectangle BoundingRect
        {
            get;
        }
    }

    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    [Serializable]
    public unsafe class Image : IList
    {
        public  PixelType[]  Pixels;
        private int          NumRows;
        private int          NumCols;

        private int          _rowStride;
        private int          _pixelStride;
        private int          _offset;
        [NonSerialized] 
        PixelType*   _pdata;


        private static PixelType Square(PixelType val)
        {
            return val * val;
        }

        public Rectangle BoundingRect
        {
            get { return new Rectangle(0, 0, NumCols,NumRows); }
        }

        public Size GetSize
        {
            get { return new Size(NumCols, NumRows); }
        }

        public int Width
        {
            get { return NumCols; }
        }

        public int Height
        {
            get { return NumRows; }
        }

        private int Nelement()  { return Width * Height; }

        // Gives you raw pointer access to the data.  Clearly unsafe but potentially 2-3 times faster.
        public PixelType* Data() { return _pdata; }
        public PixelType* Row_Start(int nrow)  { return _pdata + nrow * _rowStride; }
        public PixelType* Row_End  (int nrow) { return _pdata + (nrow * _rowStride) + (Width * _pixelStride); }

        public int         Step_Row()  { return (int)_rowStride; }
        public int         Step_Col() { return (int)_pixelStride; }

        private int        PixelIndex(int ncol, int nrow)
        {
            return _offset + (_rowStride * nrow) + (_pixelStride * ncol);
        }

        public static bool Compatible(Image one, Image two)
        {
            return (one.NumCols == two.NumCols) && (one.Height == two.Height);
        }

        public PixelType   GetPixel         (int ncol, int nrow)
        {
            return Pixels[PixelIndex(ncol, nrow)];
        }

        public PixelType   GetPixel         (int pixelNumber)
        {
            return Pixels[pixelNumber];
        }
        
        public void        SetPixelData     (PixelType* data) 
        {
            _pdata = data;
        }

        public void        SetupFastAccess  (PixelType* data)
        {
            _pdata = data;
        }

        public PixelType   GetPixelFast     (int ncol, int nrow)
        {
            return this._pdata[PixelIndex(ncol, nrow)];
        }
        /// <summary>
        /// Reflect around the boundaries.
        /// </summary>
        public PixelType   GetPixelReflect  (int ncol, int nrow)
        {
            if (ncol < 0)
                ncol = - ncol;

            ncol = ncol % (Width * 2);

            if (ncol >= Width)
                ncol = Width - (1 + ncol - Width);
            
            if (nrow < 0)
                nrow = - nrow;

            nrow = nrow % (Height * 2);

            if (nrow >= Height)
                nrow = Height - (1 + nrow - Height);

            return this._pdata[PixelIndex(ncol, nrow)];
        }

        public void        SetPixel         (int ncol, int nrow, PixelType pixel)
        {
            Pixels[PixelIndex(ncol, nrow)] = pixel;
        }

        public void        SetPixelFast     (int ncol, int nrow, PixelType pixel)
        {
            this._pdata[PixelIndex(ncol, nrow)] = pixel;
        }

        private void Setup(int numCols, int numRows)
        {
            _offset = 0;
            _pixelStride = 1;
            NumCols = numCols;
            NumRows = numRows;
            _rowStride = NumCols;
        }

        private void Allocate()
        {
            Pixels = new PixelType[NumCols * NumRows];
        }

        public Image()
        {
        }

        public Image(PixelType[] vvpixel)
        {
            Pixels = vvpixel;
            _offset = 0;
            _pixelStride = 1;
            NumCols = vvpixel.GetLength(0);
            NumRows = vvpixel.GetLength(1);
            _rowStride = NumCols;
        }

        public Image(char[] vchar, int numCols, int numRows)
        {
            Debug.Assert(vchar.Length == numCols * numRows);

            Setup(numCols, numRows);
            Allocate();

            fixed(PixelType *pt = Pixels) 
            {
                PixelType *current = pt;
                for(int n = 0; n < NumRows * NumCols; ++n)
                {
                    *(current++) = (PixelType) vchar[n];
                }
            }
        }

        public Image(BitmapData data, ColorPlane plane)
        {
            this.InitFromBitmapData(data, plane);
        }

        public Image(Bitmap bmp, ColorPlane plane)
        {
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb
                );
            InitFromBitmapData(data, plane);
            bmp.UnlockBits(data);
        }

        public Image(BitmapData data)
        {
            this.InitFromBitmapDataGray(data);
        }

        public Image(Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb
                );
            InitFromBitmapDataGray(data);
            bmp.UnlockBits(data);
        }

        /// <summary>
        /// Constructor taking an byte array representing an image
        /// </summary>
        /// <param name="data">data array. 1 Byte per channel pixel</param>
        /// <param name="iChannel">Which channel to use Must be < bytePerPixel</param>
        /// <param name="numCols">Width</param>
        /// <param name="numRows">Height</param>
        /// <param name="numChannels">Number of channels</param>
        public Image(byte[] data, int iChannel, int numCols, int numRows, int numChannels)
        {
            Setup(numCols, numRows);
            Allocate();

            fixed (byte* pDataBase = data)
            {
                byte* pData = pDataBase + iChannel;

                fixed (PixelType* pPixelBufBase = Pixels)
                {
                    PixelType* pPixelBuf = pPixelBufBase;

                    for (int n = 0; n < NumRows * NumCols; ++n)
                    {
                        *pPixelBuf = (PixelType)(*pData);
                        pData += numChannels;
                        ++pPixelBuf;
                    }
                }
            }
        }

        public Image(byte[] data, int iChannel, int numCols, int numRows, int numChannels, int stride)
        {
            Setup(numCols, numRows);
            Allocate();

            int n = 0;
            for (int row = 0; row < NumRows; ++row)
            {
                int rowOffSet = row * stride;
                int col = rowOffSet + iChannel;

                for (int iCol = 0; iCol < numCols; ++iCol, col += numChannels)
                {
                    Pixels[n++] = data[col];
                }
            }
        }

        public Image(Size imageSize)
        {
            Setup(imageSize.Width, imageSize.Height);
            Allocate();
        }

        public enum ColorPlane { red, green, blue };

        public void InitFromBitmapDataGray(BitmapData data)
        {
            Debug.Assert(data.PixelFormat == PixelFormat.Format32bppArgb);

            Setup(data.Width, data.Height);
            Allocate();

            uint* bitmapRowPointer = (uint*)data.Scan0.ToPointer();

            fixed (PixelType* pt = Pixels)
            {
                PixelType* current = pt;

                for (int nr = 0; nr < NumRows; ++nr)
                {
                    uint* bitmapPointer = bitmapRowPointer;
                    for (int nc = 0; nc < NumCols; ++nc)
                    {
                        uint pixel = 0;
                        PixelType grayVal = 0;

                        pixel = (*bitmapPointer) >> 16;
                        pixel &= 0xFF;
                        grayVal += pixel;

                        pixel = (*bitmapPointer) >> 8;
                        pixel &= 0xFF;
                        grayVal += pixel;

                        pixel = (*bitmapPointer);
                        pixel &= 0xFF;
                        grayVal += pixel;

                        *(current++) = grayVal;
                        bitmapPointer++;
                    }
                    bitmapRowPointer += data.Stride / 4;
                }
            }
        }

        public void InitFromBitmapData(BitmapData data, ColorPlane plane)
        {
            Debug.Assert(data.PixelFormat == PixelFormat.Format32bppArgb);

            Setup(data.Width, data.Height);
            Allocate();

            uint* bitmapRowPointer = (uint*)data.Scan0.ToPointer();

            fixed (PixelType* pt = Pixels)
            {
                PixelType* current = pt;

                for (int nr = 0; nr < NumRows; ++nr)
                {
                    uint* bitmapPointer = bitmapRowPointer;
                    for (int nc = 0; nc < NumCols; ++nc)
                    {
                        uint pixel = 0;

                        switch (plane)
                        {
                            case ColorPlane.red:
                                pixel = (*bitmapPointer) >> 16;
                                pixel &= 0xFF;
                                break;
                            case ColorPlane.green:
                                pixel = (*bitmapPointer) >> 8;
                                pixel &= 0xFF;
                                break;
                            case ColorPlane.blue:
                                pixel = (*bitmapPointer);
                                pixel &= 0xFF;
                                break;
                        }
                        *(current++) = (PixelType)pixel;
                        bitmapPointer++;
                    }
                    bitmapRowPointer += data.Stride / 4;
                }
            }
        }

        public static Bitmap ToBitmap(Image red, Image green, Image blue) 
        {
            Bitmap res = new Bitmap(red.Width, red.Height);
            ToBitmap(res, red, green, blue);
            return res;
        }

        public static void ToBitmap(Bitmap bmp, Image red, Image green, Image blue)
        {
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height), 
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb
                );
            ToBitmapData(data, red, green, blue);
            bmp.UnlockBits(data);
        }

        public static void ToBitmapData(BitmapData data, Image red, Image green, Image blue)
        {
            Debug.Assert(data.PixelFormat == PixelFormat.Format32bppArgb);

            byte *bitmapRowPointer = (byte*) data.Scan0.ToPointer();

            fixed(PixelType *ptRedBase = red.Pixels) 
            {
                PixelType *ptRed = ptRedBase;

                fixed(PixelType *ptGreenBase = green.Pixels) 
                {
                    PixelType *ptGreen = ptGreenBase;

                    fixed(PixelType *ptBlueBase = blue.Pixels) 
                    {
                        PixelType *ptBlue = ptBlueBase;


                        for(int nr = 0; nr < red.Height; ++nr) 
                        {
                            byte *bitmapPointer = bitmapRowPointer;
                            for(int nc = 0; nc < red.NumCols; ++nc) 
                            {

                                /*
                                *bitmapPointer = 255;
                                ++bitmapPointer;
                                *bitmapPointer = 0;
                                ++bitmapPointer;
                                *bitmapPointer = 0;
                                ++bitmapPointer;
                                */

                                *bitmapPointer = ((byte) Math.Min(255, Math.Max(0, (int) *ptBlue)));
                                ++bitmapPointer;

                                *bitmapPointer = ((byte) Math.Min(255, Math.Max(0, (int) *ptGreen)));
                                ++bitmapPointer;

                                *bitmapPointer = ((byte) Math.Min(255, Math.Max(0, (int) *ptRed)));
                                ++bitmapPointer;

                                *bitmapPointer = 255;
                                ++bitmapPointer;


                                ++ptRed;
                                ++ptGreen;
                                ++ptBlue;
                            }
                            bitmapRowPointer += data.Stride;
                        }
                    }
                }
            }
        }
        
        public unsafe Image(int numCols, int numRows)
        {
            Setup(numCols, numRows);
            Allocate();
        }

        public static void Print(Image one, TextWriter tw)
        {
            for(int row = 0; row < one.Height; ++row) 
            {
                string line = "";
                for(int col = 0; col < one.Width; ++col) 
                {
                    line += String.Format(" {0: 000.0000;-000.0000}", one.GetPixel(col, row));
                }
                Console.WriteLine(line);
            }
        }
        /// <summary>
        /// Initialize the image with this value
        /// </summary>
        public static void Init(Image one, PixelType val)
        {
            fixed(PixelType *pt = one.Pixels) 
            {
                one._pdata = pt;

                for(int nrow = 0; nrow < one.Height; ++nrow) 
                {
                    PixelType* prow = one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while(prow < prow_end)
                    {
                        *prow = val;
                        prow += one.Step_Col();
                    }
                }
            }
        }

        public static PixelType PixelMean(Image one)
        {
            return PixelSum(one) / (one.Width * one.Height);
        }

        public static PixelType PixelVariance(Image one)
        {
            PixelType mean = PixelMean(one);
            PixelType sum = 0;
            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                for (int nrow = 0; nrow < one.Height; ++nrow)
                {
                    PixelType* prow = one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while (prow < prow_end)
                    {
                        PixelType val = *prow - mean;
                        sum += val * val;
                        prow += one.Step_Col();
                    }
                }
            }
            return sum / (one.Width * one.Height);
        }

        public static PixelType PixelStandardDeviation(Image one)
        {
            return (PixelType) Math.Sqrt(PixelVariance(one));
        }
        /// <summary>
        /// Compute the sum of the pixels.
        /// </summary>
        public static PixelType PixelSum(Image one)
        {
            PixelType sum = 0;
            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                sum = _PixelSumInternal(one, sum);
            }
            return sum;
        }

        private static PixelType _PixelSumInternal(Image one, PixelType sum)
        {
            for (int nrow = 0; nrow < one.Height; ++nrow)
            {
                PixelType* prow = one.Row_Start(nrow);
                PixelType* prow_end = one.Row_End(nrow);
                while (prow < prow_end)
                {
                    sum += *prow;
                    prow += one.Step_Col();
                }
            }
            return sum;
        }
        
        public static PixelType PixelSumSquared (Image one) 
        {
            PixelType sum = 0;
            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                for (int nrow = 0; nrow < one.Height; ++nrow)
                {
                    PixelType* prow = one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while (prow < prow_end)
                    {
                        sum += *prow * *prow;
                        prow += one.Step_Col();
                    }
                }
            }
            return sum;
        }
        /// <summary>
        /// Compute the minimum pixel value.
        /// </summary>
        public static PixelType PixelMin    (Image one) 
        {
            PixelType val = PixelType.MaxValue;
            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                for(int nrow = 0; nrow < one.Height; ++nrow) 
                {
                    PixelType* prow =     one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while(prow < prow_end)
                    {
                        val = Math.Min(*prow, val);
                        prow += one.Step_Col();
                    }
                }
            }
            return val;
        }
        /// <summary>
        /// Compute the minimum pixel value.
        /// </summary>
        public static PixelType PixelMax    (Image one) 
        {
            PixelType val = PixelType.MinValue;
            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                for(int nrow = 0; nrow < one.Height; ++nrow) 
                {
                    PixelType* prow =     one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while(prow < prow_end)
                    {
                        val = Math.Max(*prow, val);
                        prow += one.Step_Col();
                    }
                }
            }
            return val;
        }
        /// <summary>
        /// Add val to each pixel
        /// </summary>
        public static void      PixelAdd(Image one, float val, Image res)
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < res.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while (prow < prow_end)
                        {
                            *prow = (PixelType)(*prow_one + val);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Threshold image
        /// </summary>
        public static void      PixelThreshold(Image one, float threshold, Image res)
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < res.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while (prow < prow_end)
                        {
                            if (*prow_one > threshold)
                                *prow = (PixelType)1;
                            else
                                *prow = (PixelType)0;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }

        public static void      PixelMultiply (Image one, float val, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;
                    
                    for(int nrow = 0; nrow < res.Height; ++nrow) 
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while(prow < prow_end)
                        {
                            *prow = (PixelType) (*prow_one * val);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Initialize the random seed used for the randomized functions in 
        /// this class.
        /// </summary>
        public static void      RandomInit    (int seed)
        {
            SharedRandom.Init(seed);
        }
        /// <summary>
        /// Set pixels to random values.
        /// </summary>
        /// 
        public static void      RandomUniform (Image one, PixelType tmax, PixelType tmin)
        {
            fixed(PixelType *pt = one.Pixels) 
            {
                one._pdata = pt;

                for(int nrow = 0; nrow < one.Height; ++nrow) 
                {
                    PixelType* prow = one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while(prow < prow_end)
                    {
                        double ran = SharedRandom.Generator.NextDouble();
                        *prow = (PixelType)( tmin + ran * (tmax - tmin));
                        prow += one.Step_Col();
                    }
                }
            }
        }
        /// <summary>
        /// Set pixels to random values.
        /// </summary>
        public static void      RandomNormal  (Image one, PixelType mean, PixelType stddev)
        {
            fixed(PixelType *pt = one.Pixels) 
            {
                one._pdata = pt;

                for(int nrow = 0; nrow < one.Height; ++nrow) 
                {
                    PixelType* prow = one.Row_Start(nrow);
                    PixelType* prow_end = one.Row_End(nrow);
                    while(prow < prow_end)
                    {
                        double ran = SharedRandom.NextNormal();
                        *prow = (PixelType)( (ran * stddev) + mean ) ;
                        prow += one.Step_Col();
                    }
                }
            }
        }
        /// <summary>
        /// Compute absolute value of sim
        /// </summary>
        public static void      Abs           (Image one, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    _AbsInternal(one, res);
                }
            }
        }

        private static void     _AbsInternal(Image one, Image res)
        {
            for (int nrow = 0; nrow < one.Height; ++nrow)
            {
                PixelType* prow = res.Row_Start(nrow);
                PixelType* prow_end = res.Row_End(nrow);
                PixelType* prow_one = one.Row_Start(nrow);
                while (prow < prow_end)
                {
                    *prow = Math.Abs(*prow_one);
                    prow += res.Step_Col();
                    prow_one += one.Step_Col();
                }
            }
        }
        /// <summary>
        /// Product of the squares of the dx dx and dy dy.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="res"></param>
        public static void      FastHarris    (Image one, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    for(int nrow = 1; nrow < one.Height -1; ++nrow) 
                    {
                        int dcol = one.Step_Col();
                        int drow = one.Step_Row();
                        PixelType* prow =     res.Row_Start(nrow) + res.Step_Col();
                        PixelType* prow_end = res.Row_End(nrow) - res.Step_Col();
                        PixelType* pone = one.Row_Start(nrow) + one.Step_Col();
                        while(prow < prow_end)
                        {

                            PixelType dx   = *pone - *(pone + dcol);
                            PixelType dy   = *pone - *(pone + drow);
                            PixelType dxdx = *pone - 0.5f * ( *(pone + dcol) + *(pone - dcol));
                            PixelType dydy = *pone - 0.5f * ( *(pone + drow) + *(pone - drow));

                            PixelType determinant = (dx * dy) - (dxdx * dydy);
                            PixelType trace       = dx + dy;

                            *prow = determinant / trace;
                            prow += res.Step_Col();
                            pone += one.Step_Col();
                        }
                    }
                }
            }
        } 
        
        public static void      LocalMax      (Image one, PixelType threshold, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    for(int nrow = 1; nrow < one.Height -1; ++nrow) 
                    {
                        int dcol = one.Step_Col();
                        int drow = one.Step_Row();
                        PixelType* prow =     res.Row_Start(nrow) + res.Step_Col();
                        PixelType* prow_end = res.Row_End(nrow) - res.Step_Col();
                        PixelType* pone = one.Row_Start(nrow) + one.Step_Col();
                        while(prow < prow_end)
                        {
                            PixelType center = *pone - threshold;

                            if (true 
                                && (center > *(pone + dcol))
                                && (center > *(pone + drow))
                                && (center > *(pone - drow))
                                && (center > *(pone - drow))) 
                            {
                                *prow = (PixelType) 255.0;
                            }
                            else 
                            {
                                *prow = (PixelType) 0.0;
                            }

                            prow += res.Step_Col();
                            pone += one.Step_Col();
                        }
                    }
                }
            }
        }

        public static void      Dog           (Image one, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    for(int nrow = 1; nrow < one.Height -1; ++nrow) 
                    {
                        int dcol = one.Step_Col();
                        int drow = one.Step_Row();
                        PixelType* prow =     res.Row_Start(nrow) + res.Step_Col();
                        PixelType* prow_end = res.Row_End(nrow) - res.Step_Col();
                        PixelType* pone = one.Row_Start(nrow) + one.Step_Col();
                        while(prow < prow_end)
                        {
                            PixelType val = *pone - 0.25f * 
                                ( *(pone + dcol) + *(pone + drow) + *(pone - drow) + *(pone - drow));

                            *prow = val;

                            prow += res.Step_Col();
                            pone += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Pad the image on the right hand side with pixels taken from the left hand side
        /// (periodic boundary conditions).
        /// </summary>
        /// <param name="one"></param>
        /// <param name="res"></param>
        public static void      RightPad(Image one, Image res)
        {
            int width = one.Width;
            int resWidth = res.Width;
            int pad = resWidth - width;

            Debug.Assert(pad >= 0);

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < one.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        PixelType* prow_one_end = one.Row_End(nrow);

                        int col = 0;
                        while (col < width)
                        {
                            *prow = *prow_one;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                            col++;
                        }

                        prow_one = one.Row_Start(nrow) + ((pad - 1) * one.Step_Col());
                        while (col < resWidth)
                        {
                            *prow = *prow_one;
                            prow += res.Step_Col();
                            prow_one -= one.Step_Col();
                            col++;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Insert ONE into RES so that it is in the center (assumes RES is not smaller).
        /// </summary>
        /// <param name="one"></param>
        /// <param name="res"></param>
        public static void      CenterPad(Image one, Image res)
        {
            int width = one.Width;
            int resWidth = res.Width;
            int height = one.Height;
            int resHeight = res.Height;
            int padWidth = (int)Math.Round((resWidth - width) / 2.0);
            int padHeight = (int)Math.Round((resHeight - height) / 2.0);

            Debug.Assert(padWidth >= 0 && padHeight >= 0);

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < one.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow + padHeight);
                        prow += res.Step_Col() * padWidth;

                        PixelType* prow_one = one.Row_Start(nrow);
                        PixelType* prow_one_end = one.Row_End(nrow);

                        while (prow_one < prow_one_end)
                        {
                            *prow = *prow_one;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Insert ONE into RES so that it is in the center.  Assumes RES is not smaller.  
        /// Pad out the boundaries in a reflecting fashion.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="res"></param>
        public static void      ReflectPad(Image one, Image res, int dcol, int drow)
        {
            int width = one.Width;
            int resWidth = res.Width;
            int height = one.Height;
            int resHeight = res.Height;
            int padWidth = (int)Math.Round((resWidth - width) / 2.0);
            int padHeight = (int)Math.Round((resHeight - height) / 2.0);

            padWidth += dcol;
            padHeight += drow;

            Debug.Assert(padWidth >= 0 && padHeight >= 0);

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < res.Height; ++nrow)
                    {
                        for (int ncol = 0; ncol < res.Width; ++ncol)
                        {
                            res.SetPixelFast(ncol, nrow, one.GetPixelReflect(ncol - padWidth, nrow - padHeight));
                        }
                    }
                }
            }
        }

        public static double    TransCol(double[,] trans, double fcol, double frow)
        {
            return (trans[0, 0] * fcol) + (trans[0, 1] * frow) + trans[0, 2];
        }

        public static double    TransRow(double[,] trans, double fcol, double frow)
        {
            return (trans[1, 0] * fcol) + (trans[1, 1] * frow) + trans[1, 2];
        }
        /// <summary>
        /// Copy pixels into RES from ONE.  Mapping is determined by the homogenous
        /// transformation TRANS.  Note, all coordinates are relative to the center of the two images.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="res"></param>
        public static void      AffineTransPad(Image one, Image res, double[,] trans)
        {
            int width = one.Width;
            int height = one.Height;
            double cColOne = width / 2.0;
            double cRowOne = height / 2.0;
            AffineTransPadProvideCenter(one, cColOne, cRowOne, res, trans);
        }
        /// <summary>
        /// Copy pixels into RES from ONE.  Mapping is determined by the homogenous
        /// transformation TRANS.  Mapping is relative to the passed in centre for ONE and the centre of RES
        /// </summary>
        /// <param name="one"></param>
        /// <param name="cColOne"></param>
        /// <param name="cRowOne"></param>
        /// <param name="res"></param>
        /// <param name="trans"></param>
        public static void      AffineTransPadProvideCenter(Image one, double cColOne, double cRowOne, Image res, double[,] trans)
        {
            int resWidth = res.Width;
            int resHeight = res.Height;

            double cColRes = resWidth / 2.0;
            double cRowRes = resHeight / 2.0;

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < res.Height; ++nrow)
                    {
                        for (int ncol = 0; ncol < res.Width; ++ncol)
                        {
                            double fcol = cColOne + TransCol(trans, ncol - cColRes, nrow - cRowRes);
                            double frow = cRowOne + TransRow(trans, ncol - cColRes, nrow - cRowRes);
                            res.SetPixelFast(ncol, nrow, BilinearReflect(one, fcol, frow));
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Copy pixels into RES from ONE.  Mapping is determined by the homogenous
        /// transformation TRANS.  Note, all coordinates are relative to the upper left
        /// corner of the images.
        /// </summary>
        public static void      MatrixTransPad(Image one, Image res, double[,] trans)
        {
            int width = one.Width;
            int resWidth = res.Width;
            int height = one.Height;
            int resHeight = res.Height;

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < res.Height; ++nrow)
                    {
                        for (int ncol = 0; ncol < res.Width; ++ncol)
                        {
                            double fcol = TransCol(trans, ncol, nrow);
                            double frow = TransRow(trans, ncol, nrow);
                            res.SetPixelFast(ncol, nrow, BilinearReflect(one, fcol, frow));
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Add offset and compute floor
        /// </summary>
        public static void      Floor         (Image one, PixelType offset, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    for(int nrow = 0; nrow < one.Height; ++nrow) 
                    {
                        PixelType* prow =     res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while(prow < prow_end)
                        {
                            *prow = (float) Math.Floor(*prow_one + offset);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Compute square of the pixels in one
        /// </summary>
        public static void      Square        (Image one, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    for(int nrow = 0; nrow < one.Height; ++nrow) 
                    {
                        PixelType* prow =     res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while(prow < prow_end)
                        {
                            *prow = *prow_one * *prow_one;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Compute sqrt of the pixels in one
        /// </summary>
        public static void      Sqrt(Image one, Image res)
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < one.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while (prow < prow_end)
                        {
                            *prow = (PixelType)Math.Sqrt(*prow_one);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Downsample by a fixed rate
        /// </summary>
        public static void      DownSample(Image one, int colStep, int rowStep, Image res)
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    if (Math.Ceiling(one.Width / (float) colStep) != res.Width)
                        throw new Exception("Wrong sizes.");
                    if (Math.Ceiling(one.Height / (float) rowStep) != res.Height)
                        throw new Exception("Wrong sizes.");

                    int nrow_res = 0;
                    for (int nrow = 0; nrow < one.Height; nrow += rowStep)
                    {
                        PixelType* prow_one = one.Row_Start(nrow);
                        PixelType* prow_one_end = one.Row_End(nrow);
                        PixelType* prow = res.Row_Start(nrow_res);
                        while (prow_one < prow_one_end)
                        {
                            *prow = *prow_one;
                            prow += res.Step_Col();
                            prow_one += (colStep * one.Step_Col());
                        }
                        ++nrow_res;
                    }
                }
            }
        }

        public static void      Log           (Image one, Image res) 
         {
             Debug.Assert(Compatible(one, res), "Images not compatible");

             fixed(PixelType *ptone = one.Pixels) 
             {
                 one._pdata = ptone;

                 fixed(PixelType *ptres = res.Pixels) 
                 {
                     res._pdata = ptres;

                     for(int nrow = 0; nrow < one.Height; ++nrow) 
                     {
                         PixelType* prow =     res.Row_Start(nrow);
                         PixelType* prow_end = res.Row_End(nrow);
                         PixelType* prow_one = one.Row_Start(nrow);
                         while(prow < prow_end)
                         {
                             *prow = (PixelType) Math.Log(*prow_one);
                             prow += res.Step_Col();
                             prow_one += one.Step_Col();
                         }
                     }
                 }
             }
         }
        /// <summary>
        /// Add the pixels
        /// </summary>
        public static void      Add           (Image one, Image two, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");
            Debug.Assert(Compatible(two, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *pttwo = two.Pixels) 
                {
                    two._pdata = pttwo;

                    fixed(PixelType *ptres = res.Pixels) 
                    {
                        res._pdata = ptres;
                    
                        for(int nrow = 0; nrow < res.Height; ++nrow) 
                        {
                            PixelType* prow = res.Row_Start(nrow);
                            PixelType* prow_end = res.Row_End(nrow);
                            PixelType* prow_one = one.Row_Start(nrow);
                            PixelType* prow_two = two.Row_Start(nrow);
                            while(prow < prow_end)
                            {
                                *prow = (PixelType) (*prow_one + *prow_two);
                                prow += res.Step_Col();
                                prow_one += one.Step_Col();
                                prow_two += two.Step_Col();
                            }
                        }
                    }
                }
            }
        }

        private static void     _AddInternal( Image one, Image two, Image res )
        {
            for (int nrow = 0; nrow < res.Height; ++nrow)
            {
                PixelType* prow = res.Row_Start(nrow);
                PixelType* prow_end = res.Row_End(nrow);
                PixelType* prow_one = one.Row_Start(nrow);
                PixelType* prow_two = two.Row_Start(nrow);
                while (prow < prow_end)
                {
                    *prow = (PixelType)(*prow_one + *prow_two);
                    prow += res.Step_Col();
                    prow_one += one.Step_Col();
                    prow_two += two.Step_Col();
                }
            }
        }
        /// <summary>
        /// Subtract the pixels
        /// </summary>
        public static void      Subtract      (Image one, Image two, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");
            Debug.Assert(Compatible(two, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *pttwo = two.Pixels) 
                {
                    two._pdata = pttwo;

                    fixed(PixelType *ptres = res.Pixels) 
                    {
                        res._pdata = ptres;
                    
                        for(int nrow = 0; nrow < res.Height; ++nrow) 
                        {
                            PixelType* prow = res.Row_Start(nrow);
                            PixelType* prow_end = res.Row_End(nrow);
                            PixelType* prow_one = one.Row_Start(nrow);
                            PixelType* prow_two = two.Row_Start(nrow);
                            while(prow < prow_end)
                            {
                                *prow = (PixelType) (*prow_one - *prow_two);
                                prow += res.Step_Col();
                                prow_one += one.Step_Col();
                                prow_two += two.Step_Col();
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Multiply the pixels
        /// </summary>
        public static void      Multiply      (Image one, Image two, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");
            Debug.Assert(Compatible(two, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *pttwo = two.Pixels) 
                {
                    two._pdata = pttwo;

                    fixed(PixelType *ptres = res.Pixels) 
                    {
                        res._pdata = ptres;
                    
                        for(int nrow = 0; nrow < res.Height; ++nrow) 
                        {
                            PixelType* prow = res.Row_Start(nrow);
                            PixelType* prow_end = res.Row_End(nrow);
                            PixelType* prow_one = one.Row_Start(nrow);
                            PixelType* prow_two = two.Row_Start(nrow);
                            while(prow < prow_end)
                            {
                                *prow = (PixelType) (*prow_one * *prow_two);
                                prow += res.Step_Col();
                                prow_one += one.Step_Col();
                                prow_two += two.Step_Col();
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Ratioo of of the pixels a / b. Pixel by pixel
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="res"></param>
        public static void Divide(Image one, Image two, Image res)
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");
            Debug.Assert(Compatible(two, res), "Images not compatible");

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* pttwo = two.Pixels)
                {
                    two._pdata = pttwo;

                    fixed (PixelType* ptres = res.Pixels)
                    {
                        res._pdata = ptres;

                        for (int nrow = 0; nrow < res.Height; ++nrow)
                        {
                            PixelType* prow = res.Row_Start(nrow);
                            PixelType* prow_end = res.Row_End(nrow);
                            PixelType* prow_one = one.Row_Start(nrow);
                            PixelType* prow_two = two.Row_Start(nrow);
                            while (prow < prow_end)
                            {
                                if (*prow_two != 0.0)
                                {
                                *prow = (PixelType)(*prow_one / *prow_two);
                                }
                                else
                                {
                                    *prow = (PixelType) (*prow_one);
                                }
                                prow += res.Step_Col();
                                prow_one += one.Step_Col();
                                prow_two += two.Step_Col();
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Linear combination of the pixels  res = a*one + b*two
        /// </summary>
        public static void      LinearCombine (double a, Image one, double b, Image two, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");
            Debug.Assert(Compatible(two, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *pttwo = two.Pixels) 
                {
                    two._pdata = pttwo;

                    fixed(PixelType *ptres = res.Pixels) 
                    {
                        res._pdata = ptres;
                    
                        for(int nrow = 0; nrow < res.Height; ++nrow) 
                        {
                            PixelType* prow = res.Row_Start(nrow);
                            PixelType* prow_end = res.Row_End(nrow);
                            PixelType* prow_one = one.Row_Start(nrow);
                            PixelType* prow_two = two.Row_Start(nrow);
                            while(prow < prow_end)
                            {
                                *prow = (PixelType) (a * *prow_one + b * *prow_two);
                                prow += res.Step_Col();
                                prow_one += one.Step_Col();
                                prow_two += two.Step_Col();
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Linear combination of the pixels  res = a*one + b*two
        /// </summary>
        public static void      Scale         (Image one, double a, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;
                    
                    for(int nrow = 0; nrow < res.Height; ++nrow) 
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while(prow < prow_end)
                        {
                            *prow = (PixelType) (a * *prow_one);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }

        public static void      Copy          (Image one, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;
                    
                    for(int nrow = 0; nrow < res.Height; ++nrow) 
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while(prow < prow_end)
                        {
                            *prow = (PixelType) (*prow_one);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }

        public static void      Stretch   (Image one, PixelType min, PixelType max, Image res) 
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            PixelType currMin = PixelMin(one);
            PixelType currMax = PixelMax(one);

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;
                    
                    for(int nrow = 0; nrow < res.Height; ++nrow) 
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while(prow < prow_end)
                        {
                            *prow = (PixelType) min + ( (max - min) * ((*prow_one - currMin) / (currMax - currMin)) ) ;
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Normalize the image so that the sum of the squares is one.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="res"></param>
        public static void      Normalize(Image one, Image res)
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            double factor = Math.Sqrt(PixelSumSquared(one));

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptres = res.Pixels)
                {
                    res._pdata = ptres;

                    for (int nrow = 0; nrow < res.Height; ++nrow)
                    {
                        PixelType* prow = res.Row_Start(nrow);
                        PixelType* prow_end = res.Row_End(nrow);
                        PixelType* prow_one = one.Row_Start(nrow);
                        while (prow < prow_end)
                        {
                            *prow = (PixelType)(*prow_one / factor);
                            prow += res.Step_Col();
                            prow_one += one.Step_Col();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Normalize the image so that mean is MEAN and stdev and STDEV
        /// </summary>
        public static void      NormalizeMeanStdev(Image one, PixelType mean, PixelType stdev, Image res)
        {
            Debug.Assert(Compatible(one, res), "Images not compatible");

            PixelType prevMean = PixelMean(one);

            PixelAdd(one, -prevMean, res);

            PixelType factor = (PixelType)(stdev / PixelStandardDeviation(res));

            PixelMultiply(res, factor, res);
            PixelAdd(res, mean, res);

        }

        public static double    Bilinear(Image one, double fcol, double frow)
        {
            Debug.Assert((fcol >= 0.0) && (fcol <= (one.NumCols - 1)), "Column is out of range");
            Debug.Assert((frow >= 0.0) && (frow <= (one.Height - 1)), "Row is out of range");

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;


                int ncol_floor = (int)(Math.Floor(fcol));
                int ncol_ceil = (int)(Math.Ceiling(fcol));
                double fcol_rem = fcol - ncol_floor;

                int nrow_floor = (int)(Math.Floor(frow));
                int nrow_ceil = (int)(Math.Ceiling(frow));
                double frow_rem = frow - nrow_floor;

                // Determine if the coordinaet (ncol or nrow) actually straddles two integer coordinates
                int stepRow = 0, stepCol = 0;
                if (nrow_floor != nrow_ceil) stepRow = one.Step_Row();
                if (ncol_floor != ncol_ceil) stepCol = one.Step_Col();

                PixelType* pbase = one.Row_Start((int)nrow_floor) + ncol_floor * one.Step_Col();
                PixelType* pbase_down = pbase + stepRow;
                PixelType* pbase_right = pbase + stepCol;
                PixelType* pbase_down_right = pbase + stepRow + stepCol;

                double fup = ((1 - fcol_rem) * *pbase) + fcol_rem * *pbase_right;
                double fdown = ((1 - fcol_rem) * *pbase_down) + fcol_rem * *pbase_down_right;

                return (1 - frow_rem) * fup + frow_rem * fdown;
            }
        }
        /// <summary>
        /// Bilinear interpolative lookup...  PLUS reflect padding.
        /// </summary>
        /// <returns></returns>
        public static PixelType BilinearReflect(Image one, double fcol, double frow)
        {

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                // Reflecting boundary conditions.
                if (fcol < 0)
                    fcol = -fcol;

                fcol = fcol % ((one.Width - 1) * 2);

                if (fcol >= one.Width)
                    fcol = one.Width - (1 + fcol - one.Width);
                //////
                if (frow < 0)
                    frow = -frow;

                frow = frow % ((one.Height - 1)* 2);

                if (frow >= one.Height)
                    frow = one.Height - (1 + frow - one.Height);


                int ncol_floor = (int)(Math.Floor(fcol));
                int ncol_ceil = (int)(Math.Ceiling(fcol));
                double fcol_rem = fcol - ncol_floor;

                int nrow_floor = (int)(Math.Floor(frow));
                int nrow_ceil = (int)(Math.Ceiling(frow));
                double frow_rem = frow - nrow_floor;

                // Two reasons not to step...  one if the coordinate is an integer, two if we
                // would step out of the image
                int stepRow = 0, stepCol = 0;
                if ((nrow_floor != nrow_ceil)  && (nrow_ceil < one.Height))
                    stepRow = one.Step_Row();
                if ((ncol_floor != ncol_ceil)  && (ncol_ceil < one.Width))
                    stepCol = one.Step_Col();

                PixelType* pbase = one.Row_Start((int)nrow_floor) + ncol_floor * one.Step_Col();
                PixelType* pbase_down = pbase + stepRow;
                PixelType* pbase_right = pbase + stepCol;
                PixelType* pbase_down_right = pbase + stepRow + stepCol;

                double fup = ((1 - fcol_rem) * *pbase) + fcol_rem * *pbase_right;
                double fdown = ((1 - fcol_rem) * *pbase_down) + fcol_rem * *pbase_down_right;

                return (PixelType) ((1 - frow_rem) * fup + frow_rem * fdown);
            }
        }
        /// <summary>
        /// Initialize a gaussian bluring kernel.  
        /// </summary>
        /// <param name="res">Result image</param>
        /// <param name="fscale">Multiply each pixel by this quantity.</param>
        /// <param name="fvar_col">Variance in the vertical axis</param>
        /// <param name="fvar_row">Variance in the horizontal axis</param>
        /// <param name="bnormalize">Normalize to sum to 1 ??</param>
        public static void      Blur_Kernel   (Image res, float fscale, float fvar_col, float fvar_row, bool bnormalize)
        {
            float fcenter_col = (res.Width - 1) / (float)(2);
            float fcenter_row = (res.Height - 1) / (float)(2);

            PixelType sum = 0;
            for(int nrow = 0; nrow < res.Height ; nrow++) 
            {
                for(int ncol = 0; ncol < res.Width ; ncol++) 
                {
                    PixelType val = (PixelType) (fscale * 
                        Math.Exp(- 0.5 * ((Square(nrow - fcenter_row) / fvar_row) 
                                          + (Square(ncol - fcenter_col) / fvar_col))));
                    res.SetPixel(ncol, nrow, val);
                    sum += val;
                        
                }
            }
            if (bnormalize)
            {
                Scale(res, 1/sum, res);
            }
        }

        // Resize using bilinear interpolation.  This should work OK for moderate down or up sampling.
        public static void      BilinearResample(Image one, Image res) 
        {
            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;

                    _BilinearResampleInternal(one, res);
                }
            }
        }

        private static void     _BilinearResampleInternal(Image one, Image res)
        {
            int ncol_cur = one.Width;
            int ncol_new = res.Width;
            double fcol_delta = ((double)(ncol_cur - 1.001) / (double)(ncol_new - 1));

            int nrow_cur = one.Height;
            int nrow_new = res.Height;
            double frow_delta = ((double)(nrow_cur - 1.001) / (double)(nrow_new - 1));

            double frow = 0;
            for (int nrow = 0; nrow < nrow_new; nrow++)
            {
                double fcol = 0;
                for (int ncol = 0; ncol < ncol_new; ncol++)
                {
                    res.SetPixel(ncol, nrow, (float)(Bilinear(one, fcol, frow)));
                    fcol += fcol_delta;
                }
                frow += frow_delta;
            }
        }

        public static void      IntegralImage (Image one, Image res)
        {
            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptres = res.Pixels) 
                {
                    res._pdata = ptres;
                
                    // Initialize first row to 0
                    PixelType* prow_res = res.Row_Start(0);
                    PixelType* prow_res_end = res.Row_End(0);
                    PixelType sum = 0;
                    while(prow_res < prow_res_end)
                    {
                        *prow_res = 0;
                        prow_res += res.Step_Col();
                    }

                    // Subsequent rows are the the sum along the current row PLUS the sum of all 
                    // elements above (recursive).
                    for(int nrow = 0; nrow < one.Height; ++nrow) 
                    {
                        PixelType* prow = one.Row_Start(nrow);
                        PixelType* prow_end = one.Row_End(nrow);
                        prow_res = res.Row_Start(nrow+1);
                        // First element is zero
                        *prow_res = 0;
                        prow_res += res.Step_Col();
                        sum = 0;
                        while(prow < prow_end)
                        {
                            sum += *prow;
                            *prow_res = (PixelType) (sum + *(prow_res - res.Step_Row()));
                            prow += one.Step_Col();
                            prow_res += res.Step_Col();
                        }
                    }
                }
            }
        }

        public static void Convolution_Generic(
            Image one, // Input image
            int nrow_skip, // Step size in image
            int ncol_skip,
            Image kernel,
            double fdivisor,
            Image res
            )
        {
            Debug.Assert((1 + one.Width - kernel.Width == res.Width)
                         && (1 + one.Height - kernel.Height == res.Height));

            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptkernel = kernel.Pixels)
                {
                    kernel._pdata = ptkernel;

                    fixed (PixelType* ptres = res.Pixels)
                    {
                        res._pdata = ptres;

                        _ConvolutionInternal(one, nrow_skip, ncol_skip, kernel, fdivisor, res);
                    }
                }
            }
        }

        private static void _ConvolutionInternal(Image one, int nrow_skip, int ncol_skip, Image kernel, double fdivisor, Image res)
        {
            double sum;

            int nrow = one.Height;
            int ncol = one.Width;
            int nrow_k = kernel.Height;
            int ncol_k = kernel.Width;

            // Two different steps...  one get the next pixel and one to get the next location 
            // to evalute the kernel
            int ncol_step = one.Step_Col();
            int ncol_step_eval = ncol_skip * ncol_step;

            int ncol_step_k = kernel.Step_Col();

            PixelType* pres = res.Data();

            for (int ri = 0; ri < (1 + nrow - nrow_k); ri += nrow_skip)
            {
                PixelType* prow_image = one.Row_Start(ri);
                // Don't go to the very end or else you run out of pixels
                PixelType* prow_image_end = (1 + one.Row_End(ri) - ncol_k);
                while (prow_image < prow_image_end)
                {
                    sum = 0;
                    // temporary pointer into the image
                    for (int rk = 0; rk < nrow_k; ++rk)
                    {
                        PixelType* prow_kernel = kernel.Row_Start(rk);
                        PixelType* prow_kernel_end = kernel.Row_End(rk);
                        PixelType* prow_offset = prow_image + rk * one.Step_Row();
                        while (prow_kernel < prow_kernel_end)
                        {
                            sum += (double)(*prow_offset * *prow_kernel);
                            ++prow_offset;
                            ++prow_kernel;
                        }
                    }
                    *(pres++) = (PixelType)(sum / fdivisor);
                    prow_image += ncol_step_eval;
                }
            }
        }

        public static void ConvolutionPad(
            Image one, // Input image
            int nrow_skip, // Step size in image
            int ncol_skip,
            Image kernel,
            double fdivisor,
            Image res
            )
        {
            fixed (PixelType* ptone = one.Pixels)
            {
                one._pdata = ptone;

                fixed (PixelType* ptkernel = kernel.Pixels)
                {
                    kernel._pdata = ptkernel;

                    fixed (PixelType* ptres = res.Pixels)
                    {
                        res._pdata = ptres;

                        _ConvolutionPadInternal(one, nrow_skip, ncol_skip, kernel, fdivisor, res);
                    }
                }
            }
        }
        
        private static void _ConvolutionPadInternal(
            Image one, int nrow_skip, int ncol_skip, Image kernel, double fdivisor, Image res)
        {
            double sum;

            int nrow = one.Height;
            int ncol = one.Width;
            int nrow_k = kernel.Height;
            int ncol_k = kernel.Width;
            int nrow_pad = nrow_k / 2;
            int ncol_pad = ncol_k / 2;


            // Two different steps...  one get the next pixel and one to get the next location 
            // to evalute the kernel
            int ncol_step = one.Step_Col();
            int ncol_step_eval = ncol_skip * ncol_step;

            int ncol_step_k = kernel.Step_Col();


            for (int ri = 0; ri < (1 + nrow - nrow_k); ri += nrow_skip)
            {
                PixelType* prow_image = one.Row_Start(ri);
                // Don't go to the very end or else you run out of pixels
                PixelType* prow_image_end = (1 + one.Row_End(ri) - ncol_k);

                PixelType* pres = res.Row_Start(ri + nrow_pad);
                pres += ncol_pad * res.Step_Col();

                while (prow_image < prow_image_end)
                {
                    sum = 0;
                    // temporary pointer into the image
                    for (int rk = 0; rk < nrow_k; ++rk)
                    {
                        PixelType* prow_kernel = kernel.Row_Start(rk);
                        PixelType* prow_kernel_end = kernel.Row_End(rk);
                        PixelType* prow_offset = prow_image + rk * one.Step_Row();
                        while (prow_kernel < prow_kernel_end)
                        {
                            sum += (double)(*prow_offset * *prow_kernel);
                            ++prow_offset;
                            ++prow_kernel;
                        }
                    }
                    *(pres++) = (PixelType)(sum / fdivisor);
                    prow_image += ncol_step_eval;
                }
            }
        }
        

        public static void ConvolutionReflecting(
            Image one, // Input image
            int nrow_skip, // Step size in image
            int ncol_skip,
            Image kernel,
            Image res
            )
        {

            fixed(PixelType *ptone = one.Pixels) 
            {
                one._pdata = ptone;

                fixed(PixelType *ptkernel = kernel.Pixels) 
                {
                    kernel._pdata = ptkernel;

                    fixed(PixelType *ptres = res.Pixels) 
                    {
                        res._pdata = ptres;

                        int colOffset = kernel.Width / 2;
                        int rowOffset = kernel.Height / 2;

                        double sum;

                        for(int resRow = 0; resRow < res.Height; ++resRow) 
                        {
                            for (int resCol = 0; resCol < res.Width; ++resCol) 
                            {
                                int oneCol = (resCol * ncol_skip) - colOffset;
                                int oneRow = (resRow * nrow_skip) - rowOffset;
                                sum = 0;
                                for(int kernelRow = 0; kernelRow < kernel.Height; ++kernelRow) 
                                {
                                    for (int kernelCol = 0; kernelCol < kernel.Width; ++kernelCol) 
                                    {
                                        sum += kernel.GetPixelFast(kernelCol, kernelRow) 
                                            * one.GetPixelReflect(oneCol + kernelCol, oneRow + kernelRow);
                                    }
                                }
                                res.SetPixelFast(resCol, resRow, (float) sum);
                            }
                        }
                    }
                }
            }
        }


        public static void      Convolve      (Image one, Image kernel, Image res)
        {
            Convolution_Generic(one, 1, 1, kernel, 1, res);
        }
        /// <summary>
        /// For each pixel compute the absolute value of teh vertical derivative.
        /// </summary>
        public static Image     AbsDerivVert  (Image one, Image res)
        {
            if (res == null) 
            {
                res = new Image(one.NumCols, one.Height);
            }

            Debug.Assert(Compatible(one, res),
                "Image sizes do not match"
                );
            
            for(int nr = 0; nr < one.NumRows - 1; ++nr)  
            {
                for(int nc = 0; nc < one.NumCols; ++nc) 
                {
                    res.SetPixel(nc, nr, Math.Abs(one.GetPixel(nc, nr+1) - one.GetPixel(nc, nr)));
                }
            }
            return res;
        }

        public static Image     AbsDerivHoriz (Image one, Image res)
        {
            if (res == null) 
            {
                res = new Image(one.NumCols, one.NumRows);
            }

            Debug.Assert(Compatible(one, res),
                "Image sizes do not match"
                );
            
            for(int nr = 0; nr < one.NumRows; ++nr)  
            {
                for(int nc = 0; nc < one.NumCols - 1; ++nc) 
                {
                    res.SetPixel(nc, nr, Math.Abs(one.GetPixel(nc+1, nr) - one.GetPixel(nc, nr)));
                }
            }
            return res;
        }

        #region IList Members

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public object this[int index]
        {
            get
            {
                return this.Pixels[index];
            }
            set
            {
                Pixels[index] = (PixelType) value;
            }
        }

        public void RemoveAt(int index)
        {
            Debug.Assert(true, "Not implemented.");
        }

        public void Insert(int index, object value)
        {
            Debug.Assert(true, "Not implemented.");
        }

        public void Remove(object value)
        {
            Debug.Assert(true, "Not implemented.");
        }

        public bool Contains(object value)
        {
            Debug.Assert(true, "Not implemented.");
            return false;
        }

        public void Clear()
        {
            Init(this, 0);
        }

        public int IndexOf(object value)
        {
            Debug.Assert(true, "Not implemented.");
            return 0;
        }

        int System.Collections.IList.Add(object value)
        {
            Debug.Assert(true, "Not implemented.");
            return 0;
        }

        public bool IsFixedSize
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                return Width * Height;
            }
        }

        public void CopyTo(Array array, int index)
        {
            //FUTURE-2005/06/14-DPU -- Add Image.CopyTo implementation
        }

        public object SyncRoot
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new ImagePixelEnumerator(this);
        }

        #endregion
    }
}
