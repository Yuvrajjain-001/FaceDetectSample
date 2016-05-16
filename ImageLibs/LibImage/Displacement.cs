using System;
using System.Drawing;
//using System.Windows.Ink.Analysis.MathLibrary;


namespace Dpu.ImageProcessing
{

    /// <summary>
    /// Used to model a continuous displacement field from the range [minx, maxx] X [miny, maxy]
    /// </summary>
    public class Displacement
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
        public float DownSample = 3;

        public float Shift;
        public float Smooth;

        Image imDx;
        Image imDy;
        Image imTmp;
        Image imBlur;

        public Displacement(int minx, int maxx, int miny, int maxy, float shift, float smooth)
        {
            MinX = (minx - shift)/DownSample;
            MaxX = (maxx + shift)/DownSample;
            MinY = (miny - shift)/DownSample;
            MaxY = (maxy + shift)/DownSample;
            Shift = shift;
            Smooth = smooth / DownSample;
            int nsmooth = (int) Math.Ceiling(Smooth * 5);
            imBlur = new Image(nsmooth, nsmooth);

            int sizeX = (int) Math.Ceiling(MaxX - MinX);
            int sizeY = (int) Math.Ceiling(MaxY - MinY);

            imTmp = new Image(sizeX + nsmooth - 1, sizeY + nsmooth - 1);
            imDx= new Image(sizeX, sizeY);
            imDy= new Image(sizeX, sizeY);
            
            Image.Blur_Kernel(imBlur, 1.0f, Smooth, Smooth, true);
            Reinitialize(0);
        }

        public void Reinitialize(int seed)
        {
            if (seed == 0) 
            {
                Image.Init(imDx, 0.0f);
                Image.Init(imDy, 0.0f);
            }
            else 
            {
                Dpu.Utility.SharedRandom.Init(seed);

                Image.RandomNormal(imTmp, 0, Shift);
                Image.Convolution_Generic(imTmp, 1, 1, imBlur, 1.0, imDx);

                Image.RandomNormal(imTmp, 0, Shift);
                Image.Convolution_Generic(imTmp, 1, 1, imBlur, 1.0, imDy);
            }
        }

        public float DX(float xIn, float yIn)
        {
            float x = xIn / DownSample;
            float y = yIn / DownSample;
            if ( x <= MinX) 
                return MinX;
            else if ( x >= MaxX - 1)
                return MaxX;
            else if ( y <= MinY || y >= MaxY - 1 )
                return x;
            else 
                return (float) Image.Bilinear(imDx, x-MinX, y-MinY);
        }        
        
        public float DY(float xIn, float yIn)
        {
            float x = xIn / DownSample;
            float y = yIn / DownSample;
            if ( y <= MinY) 
                return MinY;
            else if ( y >= MaxY - 1)
                return MaxY;
            else if ( x <= MinX || x >= MaxX - 1 )
                return y;
            else 
                return (float) Image.Bilinear(imDy, x-MinX, y-MinY);
        }

        public PointF Displace( PointF pt )
        {
            return new PointF( pt.X + DX(pt.X, pt.Y), pt.Y + DY(pt.X, pt.Y));
        }
    }
}