using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Dpu.ImageProcessing;

namespace GenPositionData
{
    /// <summary>
    /// Represents a sample affine transformation (Rotation and translation
    /// </summary>
    public class TransformSample
    {
        private double _x;
        private double _y;
        private double _theta;      // In degress
        private double _maxTheta = 10.0;
        private double _maxX = 0.1;
        private double _maxY = 0.1;
        private double _width;      // Normalizer for X translation
        private double _height;     // Normalizer for Y translation
        private Random _randGen;
        private int _randSeed = 52592;

        public TransformSample()
        {
            _randGen = new Random(_randSeed);
            Reset();
        }
        /// <summary>
        /// Reset back to default
        /// </summary>
        public void Reset()
        {
            _x = 0.0;
            _y = 0.0;
            _theta = 0.0;
            _width = 1.0;
            _height = 1.0;
        }

        public void Generate()
        {
            GenerateTheta();
            GenerateX();
            GenerateY();
        }

        public void SetImageSize(Rect rect)
        {
            _width = (double)rect.Width;
            _height = (double)rect.Height;
        }

        public void GenerateTheta()
        {
            _theta = GenerateSamp(_maxTheta);
        }

        public void GenerateX()
        {
            _x = GenerateSamp(_maxX);
        }

        public void GenerateY()
        {
            _y = GenerateSamp(_maxY);
        }


        private double GenerateSamp(double maxVal)
        {
            return 2.0 * maxVal * (_randGen.NextDouble() - 0.5);
        }

        /// <summary>
        /// X translation in pixels
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        public double Theta
        {
            get
            {
                return _theta;
            }
            set
            {
                _theta = value;
            }
        }

        public double ThetaRad
        {
            get
            {
                return Theta * Math.PI / 180.0;
            }
        }

        /// <summary>
        /// X translation in pixels
        /// </summary>
        public double Xpix
        {
            get
            {
                return X * _width;
            }
        }
        /// <summary>
        /// Y translation in pixels
        /// </summary>
        public double Ypix
        {
            get
            {
                return Y * _height;
            }
        }

        public System.Windows.Point Translation
        {
            get
            {
                return new System.Windows.Point(X, Y);
            }
        }

        public double MaxTheta
        {
            get
            {
                return _maxTheta;
            }

            set
            {
                _maxTheta = value;
            }
        }

        public double MaxX
        {
            get
            {
                return _maxX;
            }

            set
            {
                _maxX = value;
            }
        }
        public double MaxY
        {
            get
            {
                return _maxY;
            }

            set
            {
                _maxY = value;
            }
        }


        static public byte[] ExtractTransformNormalizeFace(byte[] origImage, Rect origRect, int bytePerPix, int origStride,
            Rect mapFrom, Rect faceRect, int faceStride, TransformSample trans)
        {
            // Sanity check eye location
            double[,] affineMat = new double[2, 3];
            double scaleX = mapFrom.Width / faceRect.Width;
            double scaleY = mapFrom.Height / faceRect.Height;
            
            double theta = trans.ThetaRad;

            affineMat[0, 0] = scaleX * Math.Cos(theta);
            affineMat[0, 1] = -scaleY * Math.Sin(theta);
            affineMat[0, 2] = trans.Xpix;
            affineMat[1, 0] = scaleX * Math.Sin(theta);
            affineMat[1, 1] = scaleY * Math.Cos(theta);
            affineMat[1, 2] = trans.Ypix;

            // Use the Image library routines. These operate 1 channel at a time
            Image[] outImage = new Image[bytePerPix];
            double srcCentreX = mapFrom.X + mapFrom.Width / 2.0;
            double srcCentreY = mapFrom.Y + mapFrom.Height / 2.0;

            for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
            {
                Image inChannel = new Image(origImage, iChannel, (int)origRect.Width, (int)origRect.Height, bytePerPix, origStride);
                outImage[iChannel] = new Image((int)faceRect.Width, (int)faceRect.Height);

                //Image.AffineTransPad(inChannel, outImage[iChannel], affineMat);
                Image.AffineTransPadProvideCenter(inChannel, srcCentreX, srcCentreY, outImage[iChannel], affineMat);
            }

            byte[] faceBuffer = new byte[faceStride * (int)faceRect.Height];

            for (int row = 0; row < faceRect.Height; ++row)
            {
                int rowOffSet = row * faceStride;

                for (int iChannel = 0; iChannel < bytePerPix; ++iChannel)
                {
                    int iPix = row * (int)faceRect.Width;
                    int col = rowOffSet + iChannel;

                    for (int iCol = 0; iCol < faceRect.Width; ++iCol, col += bytePerPix)
                    {
                        faceBuffer[col] = (byte)Math.Min(Byte.MaxValue, outImage[iChannel].Pixels[iPix++]);
                    }
                }
            }

            return faceBuffer;
        }

    }
}
