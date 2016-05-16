using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LibFaceData
{
    public class FaceData
    {
        public enum FaceDataTypeEnum {EyeDetect, FaceReco, Unknown};

        public string Filename;
        public string Directory;

        public System.Windows.Point TrueLeftEye;
        public System.Windows.Point TrueRightEye;
        public System.Windows.Point RecoLeftEye;
        public System.Windows.Point RecoRightEye;

        public System.Windows.Point Nose;
        public System.Windows.Point LeftMouth;
        public System.Windows.Point RightMouth;
        public System.Windows.Point NormNose;
        public System.Windows.Point NormLeftMouth;
        public System.Windows.Point NormRightMouth;

        public System.Drawing.Rectangle TrueLeftRect;
        public System.Drawing.Rectangle TrueRightRect;
        public System.Drawing.Rectangle RecoLeftRect;
        public System.Drawing.Rectangle RecoRightRect;

        public System.Drawing.Rectangle NoseRect;
        public System.Drawing.Rectangle NormNoseRect;
        public System.Drawing.Rectangle LeftMouthRect;
        public System.Drawing.Rectangle RightMouthRect;
        public System.Drawing.Rectangle MouthRect;
        public System.Drawing.Rectangle NormLeftMouthRect;
        public System.Drawing.Rectangle NormRightMouthRect;
        public System.Drawing.Rectangle NormMouthRect;

        public System.Drawing.Rectangle faceRect;
        private double[,] affineMat;

        public FaceDataTypeEnum InformationType;

        public int PhotoId;


        private LibFaceData.TransformSample _transform = new LibFaceData.TransformSample();
        private bool doTransform = false;

        public FaceData(System.ValueType leftEye, System.ValueType rightEye, System.ValueType nose, System.ValueType leftMouth, System.ValueType rightMouth, System.Windows.Rect rect)
        {

            TrueLeftEye.X = ((System.Drawing.PointF)leftEye).X;
            TrueLeftEye.Y = ((System.Drawing.PointF)leftEye).Y;

            TrueRightEye.X = ((System.Drawing.PointF)rightEye).X;
            TrueRightEye.Y = ((System.Drawing.PointF)rightEye).Y;

            Nose.X = ((System.Drawing.PointF)nose).X;
            Nose.Y = ((System.Drawing.PointF)nose).Y;

            LeftMouth.X = ((System.Drawing.PointF)leftMouth).X;
            LeftMouth.Y = ((System.Drawing.PointF)leftMouth).Y;

            RightMouth.X = ((System.Drawing.PointF)rightMouth).X;
            RightMouth.Y = ((System.Drawing.PointF)rightMouth).Y;

            faceRect.Height = (int)rect.Height;
            faceRect.Width = (int)rect.Width;
            faceRect.X = (int)rect.X;
            faceRect.Y = (int)rect.Y;
        }
        public FaceData(System.Windows.Point leftEye, System.Windows.Point rightEye, System.Windows.Point nose, System.Windows.Point leftMouth, System.Windows.Point rightMouth)
        {

            TrueLeftEye = leftEye;
            TrueRightEye = rightEye;
            Nose = nose;
            LeftMouth = leftMouth;
            RightMouth = rightMouth;
        }
        public FaceData(string line1, string line2, FaceDataTypeEnum infoType)
        {
            InformationType = infoType;

            if (line1 == null || line1.Length <= 0)
            {
                throw new Exception("FaceData No fileName specified");
            }

            Filename = line1;
            char [] cc = {'\\', '/'};
            string[] fields = line1.Split(cc, System.StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length > 1)
            {
                Directory = fields[fields.Length - 2];
            }
            else
            {
                Directory = null;
            }

            string[] splitChar = null;
            fields = line2.Split(splitChar,  System.StringSplitOptions.RemoveEmptyEntries);

            switch (InformationType)
            {
                case FaceDataTypeEnum.EyeDetect:
                    ReadEyeDetectFile(fields);
                    break;

                case FaceDataTypeEnum.FaceReco:
                    ReadFaceRecoFile(fields);
                    break;

                case FaceDataTypeEnum.Unknown:
                    if (fields.Length <= 13)
                    {
                        ReadEyeDetectFile(fields);
                    }
                    else
                    {
                        ReadFaceRecoFile(fields);
                    }
                    break;

                default:
                    throw new Exception("FaceDdata unrecognized tyep");
            }

        }

        private void ReadEyeDetectFile(string[] fields)
        {
            if (fields.Length < 12)
            {
                throw new Exception("Invalid entry Expected at least 12 fields found " + fields.Length.ToString());
            }
            int iField = 0;
            this.faceRect.X = Convert.ToInt32(fields[iField++]);
            this.faceRect.Y = Convert.ToInt32(fields[iField++]);
            this.faceRect.Width = Convert.ToInt32(fields[iField++]);
            this.faceRect.Height = Convert.ToInt32(fields[iField++]);
            this.TrueLeftEye.X = Convert.ToDouble(fields[iField++]);
            this.TrueLeftEye.Y = Convert.ToDouble(fields[iField++]);
            this.TrueRightEye.X = Convert.ToDouble(fields[iField++]);
            this.TrueRightEye.Y = Convert.ToDouble(fields[iField++]);
            this.RecoLeftEye.X = Convert.ToDouble(fields[iField++]);
            this.RecoLeftEye.Y = Convert.ToDouble(fields[iField++]);
            this.RecoRightEye.X = Convert.ToDouble(fields[iField++]);
            this.RecoRightEye.Y = Convert.ToDouble(fields[iField++]);

            if (fields.Length <= iField)
            {
                return;
            }

            if (iField + 1 == fields.Length && fields[iField].Trim().Length > 0)
            {
                PhotoId = Convert.ToInt32(fields[iField++]);
            }
            else
            {
                if (fields.Length > iField && fields[iField].Trim().Length > 0)
                {
                    this.Transform.Theta = Convert.ToDouble(fields[iField++]);
                    this.Transform.SetImageSize(this.ConvertDrawingToRect(this.faceRect));
                    this.DoTransform = true;
                }
                if (fields.Length > iField && fields[iField].Trim().Length > 0)
                {
                    this.Transform.X = Convert.ToDouble(fields[iField++]); ;
                }
                if (fields.Length > iField && fields[iField].Trim().Length > 0)
                {
                    this.Transform.Y = Convert.ToDouble(fields[iField++]); ;
                }
            }
        }

        private void ReadFaceRecoFile(string[] fields)
        {
            if (fields.Length < 14)
            {
                throw new Exception("Invalid entry Expected at least 8 fields found " + fields.Length.ToString());
            }
            int iField = 0;
            this.faceRect.X = Convert.ToInt32(fields[iField++]);
            this.faceRect.Y = Convert.ToInt32(fields[iField++]);
            this.faceRect.Width = Convert.ToInt32(fields[iField++]);
            this.faceRect.Height = Convert.ToInt32(fields[iField++]);

            this.TrueLeftEye.X = Convert.ToDouble(fields[iField++]);
            this.TrueLeftEye.Y = Convert.ToDouble(fields[iField++]);
            this.TrueRightEye.X = Convert.ToDouble(fields[iField++]);
            this.TrueRightEye.Y = Convert.ToDouble(fields[iField++]);

            this.RecoLeftEye.X = Convert.ToDouble(fields[iField++]) * this.faceRect.Width;
            this.RecoLeftEye.Y = Convert.ToDouble(fields[iField++]) * this.faceRect.Height;
            this.RecoRightEye.X = Convert.ToDouble(fields[iField++]) * this.faceRect.Width;
            this.RecoRightEye.Y = Convert.ToDouble(fields[iField++]) * this.faceRect.Height;

            if (fields.Length > iField + 6)
            {
                // This indicates a newer file format includes the True nose and mouth pos. 
                this.Nose.X = Convert.ToDouble(fields[iField++]);
                this.Nose.Y = Convert.ToDouble(fields[iField++]);
                this.LeftMouth.X = Convert.ToDouble(fields[iField++]);
                this.LeftMouth.Y = Convert.ToDouble(fields[iField++]);
                this.RightMouth.X = Convert.ToDouble(fields[iField++]);
                this.RightMouth.Y = Convert.ToDouble(fields[iField++]);
            }
            this.NormNose.X = Convert.ToDouble(fields[iField++]) * this.faceRect.Width;
            this.NormNose.Y = Convert.ToDouble(fields[iField++]) * this.faceRect.Height;
            this.NormLeftMouth.X = Convert.ToDouble(fields[iField++]) * this.faceRect.Width;
            this.NormLeftMouth.Y = Convert.ToDouble(fields[iField++]) * this.faceRect.Height;
            this.NormRightMouth.X = Convert.ToDouble(fields[iField++]) * this.faceRect.Width;
            this.NormRightMouth.Y = Convert.ToDouble(fields[iField++]) * this.faceRect.Height;

            if (iField < fields.Length)
            {
                PhotoId = Convert.ToInt32(fields[iField++]);
            }
        }

        public System.Windows.Rect FaceWindowsRect
        {
            get
            {
                return ConvertDrawingToRect(faceRect);
            }
        }

        public LibFaceData.TransformSample Transform
        {
            get
            {
                return _transform;
            }
        }

        public double [,] AffineMat
        {
            get
            {
                if (null == affineMat)
                {
                    affineMat = new double[2,3];
                    affineMat[0,1] = 1.0;
                    affineMat[1,1] = 1.0;
                }

                return affineMat;
            }

            set
            {
                affineMat = value;
            }
        }

        public bool DoTransform
        {
            get
            {
                return doTransform;
            }
            set
            {
                doTransform = value;
            }
        }
        /// <summary>
        /// Convert a System.Drawing.Rectangle to a System.Windows.Rect
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public System.Windows.Rect ConvertDrawingToRect(System.Drawing.Rectangle src)
        {
            System.Windows.Rect ret = new System.Windows.Rect(src.X, src.Y, src.Width, src.Height);
            return ret;
        }
    }

    /// <summary>
    /// Simple class for enumerating a faceData formated file 
    /// The format has two lines per entry
    ///     Pathname
    ///     FaceRect TrueEye  RecoEye [optional other Data]
    /// </summary>
    public class FaceDataFile : IDisposable
    {
        FaceData.FaceDataTypeEnum _fileType;
        StreamReader _sr;
        int _lineCount;

        public FaceDataFile(string suiteFile)
        {
            Init(suiteFile, FaceData.FaceDataTypeEnum.Unknown);
        }

        public FaceDataFile(string suiteFile, FaceData.FaceDataTypeEnum fileType)
        {
            Init(suiteFile, FaceData.FaceDataTypeEnum.Unknown);
        }

        private void Init(string suiteFile, FaceData.FaceDataTypeEnum fileType)
        {
            FileInfo fileInfo = new FileInfo(suiteFile);
            _fileType = fileType;

            if (false == fileInfo.Exists)
            {
                throw new Exception("FaceDataFileEnum: Cannot find file " + suiteFile);
            }

            _lineCount = 0;
            _sr = File.OpenText(suiteFile);

            if (null == _sr)
            {
                throw new Exception("FaceDataFileEnum: Cannot find file " + suiteFile);
            }
        }

        public int LineNumber
        {
            get
            {
                return _lineCount;
            }
        }

        public FaceData GetNext()
        {
            FaceData faceData = null;

            if (null != _sr)
            {
                string imageFile = _sr.ReadLine();
                string line = null;
                if (imageFile != null)
                {
                    ++_lineCount;
                    line = _sr.ReadLine();

                }

                if (null != line && null != imageFile)
                {
                    ++_lineCount;
                    faceData = new FaceData(imageFile, line, _fileType);
                }
            }

            return faceData;
        }

        public void Dispose()
        {
            if (null != _sr)
            {
                _sr.Close();
                _sr = null;
            }
        }
    }
}
