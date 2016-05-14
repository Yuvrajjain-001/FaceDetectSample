using System;
using System.Collections.Generic;
using System.Text;
using FaceSortUI;
using DetectionManagedLib;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.LiveLabs;

namespace GenPositionData
{
    class Program : BaseReco
    {
        enum ProgActions { GenerateEyeFaceData, FaceDetect, GenerateEyePatchData, GenerateRecoData };

        static private FaceDetector _detector = null;
        static private double _eyePatchHeight = 5;
        static private double _eyePatchWidth = 5;
        static private int _nonEyePatchCount = 50;
        static private ProgActions _action;
        static private EyeDetect.AlgorithmEnum _algo;
        static private string _algoData;
        static private int _randSeed = 16783;
        static private int _maxTransformCount;
        static private TransformSample _transform = new TransformSample();
        static private double[] _meanEye = { 0.29, 0.27, 0.72, 0.27 };
        static private TrainDataFileWriter _outWriter = null;
        static private StreamWriter _outStream = null;
        static private int _numPatchFeatures = 4;   // Eye, Nose, LeftMouth, RightMouth

        static void Main(string[] args)
        {
            float detectionThresh = 0.0F;
            string classifierPath = "classifier.txt";
            int  iArg = 0;
            string outFile = "out.Txt";
            bool preDetect = false;

            _normalizeAction = NormalizeActionsEnum.None;

            _action = ProgActions.GenerateEyeFaceData;
            _normalizeSum = 211676;
            _defaultNormalizedLen = 41 * 41;
            _maxTransformCount = 0;

            _algo = EyeDetect.AlgorithmEnum.MSRA;
            _algoData = "";

            if (args.Length < 1)
            {
                Usage();
                return;
            }

            while (iArg < args.Length - 1)
            {
                switch (args[iArg++].ToLower())
                {

                    case "-width":
                        _faceDisplayWidth = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-height":
                        _faceDisplayHeight = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-eyewidth":
                        _eyePatchWidth = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-eyeheight":
                        _eyePatchHeight = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-noneyepatchcount":
                        _nonEyePatchCount = Convert.ToInt32(args[iArg++]);
                        break;
                        
                    case "-detectpath":
                        classifierPath = args[iArg++];
                        break;

                    case "-detect":
                        _action = ProgActions.FaceDetect;
                        break;

                    case "-generateeyeface":
                        _action = ProgActions.GenerateEyeFaceData;
                        break;


                    case "-generateeyepatch":
                        _action = ProgActions.GenerateEyePatchData;
                        break;

                    case "-generatereco":
                        FaceRecoGen gen = new FaceRecoGen(args, iArg);
                        return;

                    case "-normsum":
                        _normalizeAction = NormalizeActionsEnum.ConstantSum;
                        break;

                    case "-normblur":
                        _normalizeAction = NormalizeActionsEnum.BlurSubtract;
                        break;

                    case "-normblur3":
                        _normalizeAction = NormalizeActionsEnum.BlurSubtract3;
                        break;

                    case "-blurvar":
                        _blurVar = Convert.ToSingle(args[iArg++]);
                        break;

                    case "-blurkernel":
                        _blurKernelSize = Convert.ToInt32(args[iArg++]);
                        break;

                    case "-normsift":
                        _normalizeAction = NormalizeActionsEnum.SIFT;
                        break;

                    case "-siftpartnum":
                        _partitionNumber = Convert.ToInt32(args[iArg++]);
                        break;

                    case "-msra":
                        _algo = EyeDetect.AlgorithmEnum.MSRA;
                        break;

                    case "-nn":
                        _algo = EyeDetect.AlgorithmEnum.NN;
                        _algoData = args[iArg++];
                        break;

                    case "-predetect":
                        preDetect = true;
                        break;

                    case "-maxtransformcount":
                        _maxTransformCount = Convert.ToInt32(args[iArg++]); ;
                        break;

                    case "-maxtheta":
                        _transform.MaxTheta = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-maxx":
                        _transform.MaxX = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-maxy":
                        _transform.MaxY = Convert.ToDouble(args[iArg++]);
                        break;

                    case "-writebinary":
                        _dataFileMode = TrainDataFileWriter.ModeEnum.Binary;
                        break;

                    case "-writetext":
                        _dataFileMode = TrainDataFileWriter.ModeEnum.Text;
                        break;

                    case "-out":
                        outFile = args[iArg++];
                        break;

                    case "-h":
                        Usage();
                        return;

                    default:
                        Console.WriteLine("Unrecognized option {0}", args[iArg - 1]);
                        break;
                }
            }

            if (_action == ProgActions.GenerateEyeFaceData ||
                _action == ProgActions.GenerateEyePatchData)
            {
                _outWriter = new TrainDataFileWriter(outFile, _dataFileMode);
            }
            else
            {
                _outStream = new StreamWriter(outFile);
            }

            string suiteFile = args[iArg];

            try
            {
                if (System.IO.File.Exists(suiteFile) == false)
                {
                    Console.WriteLine("Cannot open suiteFile {0}\n", suiteFile);
                    Usage();
                    return;
                }
                if (true == preDetect)
                {
                    DoPreDetectSuiteFile(suiteFile);
                }
                else
                {
                    _detector = new FaceDetector(classifierPath, true, detectionThresh);
                    DoSuiteFile(suiteFile);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error {0}", e.Message);
                if (null != e.InnerException)
                {
                    Console.WriteLine("{0}", e.InnerException.Message);
                }
            }

            if (null != _outWriter)
            {
                _outWriter.Dispose();
            }
            if (null != _outStream)
            {
                _outStream.Close();
            }
        }

        static private void Usage()
        {
            Console.WriteLine("GenPositionData <options> SuiteFile - Runs face detector or uses predetected suite files to:");
            Console.WriteLine("   1) Generate training data for eyePos detector");
            Console.WriteLine("   2) Generate training data for faceReco");
            Console.WriteLine("Note: When using the face deector, must provide a \"classifier.txt\" file");
            Console.WriteLine();


            Console.WriteLine("-width\tWidth of generated face (default 41)");
            Console.WriteLine("-height\tHeight of generated face (default 41)");
            Console.WriteLine("-eyeheight\tHeight of eye Patches (default 5)");
            Console.WriteLine("-eyewidth\tWidth of eye Patches (default 5)");
            Console.WriteLine("-noneyepatchcount\tNumber of non eyepatches generates (default 50)");
            Console.WriteLine("-detectPath\tPath to detector file (default .\\classifier.txt");
            Console.WriteLine("-detect - Run (test) eye detector and report results");
            Console.WriteLine("-generateFace - Generate face training data with Id as target");
            Console.WriteLine("-generateEyeFace - Generate face training data with eyes as targets");
            Console.WriteLine("-generateEyePatch - Generate eye patch training data");
            Console.WriteLine("-generateReco - Generate training data for face recognition");
            Console.WriteLine("-preDetect Input file is in form of previous run which has the detected rec info");
            Console.WriteLine("-msra - Use msra eye detection");
            Console.WriteLine("-nn <dataFile>: Use NN detection");
            Console.WriteLine("-normSum Normalize face to a constant value");
            Console.WriteLine("-normBlur - Normalize the image by bluring then subtract");
            Console.WriteLine("-normBlur3 - Normalize the image using channel ratios G/R, B/R");
            Console.WriteLine("-blurVar - Set the blur variaince (deafult 5)");
            Console.WriteLine("-blurKernel Set the blur kernel size (default 9)");

            Console.WriteLine("-normSIFT Extract the sift discriptors out of the image");
            Console.WriteLine("-siftPartNum number of partiion of the image (default 4)");

            Console.WriteLine("-maxTransformCount Maximum number of rotations to generate per face");
            Console.WriteLine("-maxTheta Max absolute value of sample rotation degress");
            Console.WriteLine("-maxX Max absolute value of sample X translation (fraction)");
            Console.WriteLine("-maxY Max absolute value of sample Y translation (fraction)");
            Console.WriteLine("-out - Set the output file name");
            Console.WriteLine("-writeText - Write text data files (default)");
            Console.WriteLine("-writeBinary - Write Binary format data files");
            Console.WriteLine();
            Console.WriteLine("-eyedetectPath - Optional path to eydetect NN module's data file (default use builtin)");
            Console.WriteLine("-detectPhoto - Suite file is list of photos, so first run facedetection");
            Console.WriteLine("-preDetectPhoto - Suite file is list of photos and detected rect with eyePositions");
            Console.WriteLine("-reportUsingFeats - Take as input a Chauu label file and report Eye, nose and mouth positions");
            Console.WriteLine("-skipFaceDetect - Input contains correct face bounding rect");
            Console.WriteLine("-dataFilePrefix - Prefix applied to output fileNames");
            Console.WriteLine("-generatePatch - Genarte eye nose and mouth training data files");
            Console.WriteLine("-writeBinary - Generate binary training data file");
            Console.WriteLine("-writeText - Generate text training data file");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("SuiteFile - One line per face: pathName, lefteye, RightEye, <Nose>, <Mouuth>");
            Console.WriteLine("Expects file classifier.txt in current director");
        }


        static private void DoSuiteFile(string suiteFile)
        {
            FileInfo fileInfo = new FileInfo(suiteFile);

            if (false == fileInfo.Exists)
            {
                return;
            }

            LabeledImageCollection labelCol = new LabeledImageCollection(suiteFile);

            int imageCount = 0;
            int processedCount = 0;

            foreach (LabeledImg image in labelCol.ImgList)
            {
                imageCount += GenerateData(image.FileName, image.FeaturePtsList);
                ++processedCount;
                Console.Write("\rGenerated {0} Images. Processed {1} / {2} Images", imageCount, processedCount, labelCol.ImgList.Count);
            }
            Console.WriteLine("");
        }

        static private int DoPreDetectSuiteFile(string suiteFile)
        {
            FaceDisp.FaceDataFile suiteReader = null;

            try
            {
                suiteReader = new FaceDisp.FaceDataFile(suiteFile, FaceDisp.FaceData.FaceDataTypeEnum.EyeDetect);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }

            if (null == suiteReader)
            {
                return 0;
            }

            FaceDisp.FaceData faceData = null;
            int imageCount = 0;
            while ((faceData = suiteReader.GetNext()) != null)
            {
                Rect faceRect = faceData.FaceWindowsRect;
                if (true == ProcessFace(faceData.Filename, faceRect, faceData, faceData.PhotoId))
                {
                    ++imageCount;
                    Console.Write("\rGenerated {0} Images.", imageCount);
                }
                else
                {
                    Console.WriteLine("Failed {0}", faceData.Filename);
                }

            }
            Console.WriteLine();
            return imageCount;
        }
        static private int GenerateData(string imageFileName, List<FeaturePts> featureList)
        {
            if (null == imageFileName)
            {
                return 0;
            }

            DetectionResult detectionResult = _detector.DetectObject(imageFileName);
            List<ScoredRect> scoredResultList = detectionResult.GetMergedRectList(0.0F);
            int imageCount = 0;

            if (null != featureList && scoredResultList.Count > 0)
            {
                foreach (FeaturePts features in featureList)
                {
                    System.Drawing.PointF point = (System.Drawing.PointF)features.ptLeftEye;


                    Point leftEye = new Point(point.X, point.Y);
                    point = (System.Drawing.PointF)features.ptRightEye;
                    Point rightEye = new Point(point.X, point.Y);

                    foreach (ScoredRect scoredRect in scoredResultList)
                    {
                        Rect rect = new Rect();

                        rect.X = scoredRect.X;
                        rect.Y = scoredRect.Y;
                        rect.Width = scoredRect.Width;
                        rect.Height = scoredRect.Height;

                        if (rect.Contains(leftEye) && rect.Contains(rightEye))
                        {
                            leftEye.X = (leftEye.X - rect.X) / rect.Width;
                            leftEye.Y = (leftEye.Y - rect.Y) / rect.Width; ;
                            rightEye.X = (rightEye.X - rect.X) / rect.Width; ;
                            rightEye.Y = (rightEye.Y - rect.Y) / rect.Width; ;
                            FaceDisp.FaceData faceData = new FaceDisp.FaceData(features.ptLeftEye, 
                                                                                features.ptRightEye, 
                                                                                features.ptNose, 
                                                                                features.ptLeftMouth, 
                                                                                features.ptRightMouth,
                                                                                rect);

                            //ProcessFace(imageFileName, rect, leftEye, rightEye, 0);
                            ProcessFace(imageFileName, rect, faceData, 0);
                            ++imageCount;
                            break;
                        }
                    }
                }
            }

            return imageCount;
        }



        //static private bool ProcessFace(string filename, Rect rect, Point leftEye, Point rightEye, int photoId)
        static private bool ProcessFace(string filename, Rect rect, FaceDisp.FaceData faceData, int photoId)
        {
            byte[] dataPixs = null;

            try
            {
                dataPixs = CreateMainBitMap(filename);
            }
            catch (Exception)
            {
            }

            if (null == dataPixs)
            {
                Console.WriteLine("\nFailed to createBitmap for {0}", filename);
                return false;
            }

            byte[] facePix = SelectAndNormalizePatch(dataPixs,
                           new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y),
                           new Point(0, 0), new Point(_faceDisplayWidth, 0),new Rect(0, 0, _faceDisplayWidth, _faceDisplayHeight));


            if (null == facePix)
            {
                Console.WriteLine("\nFailed to extract face for {0}", filename);
                return false;
            }

            Rect faceRect = new Rect(0, 0, _faceDisplayWidth, _faceDisplayHeight);

            _transform.Reset();
            _transform.SetImageSize(rect);
            //bool retVal = ProcessFaceInstance(facePix, filename, rect, leftEye, rightEye, faceRect, photoId);
            bool retVal = ProcessFaceInstance(facePix, filename, rect, faceData, faceRect, photoId);

            if (_maxTransformCount > 0)
            {
                Random randGen = new Random(_randSeed);
                int rotateCount = (int)Math.Round(randGen.NextDouble() * _maxTransformCount);

                for (int iRot = 0; iRot < rotateCount; ++iRot)
                {
                    _transform.Generate();
                    facePix = SelectTransformPatch(dataPixs, rect, _transform);
                    //retVal &= ProcessFaceInstance(facePix, filename, rect, RotateEye(leftEye, _transform), RotateEye(rightEye, _transform), faceRect,
                    //    photoId);
                    retVal &= ProcessFaceInstance(facePix, filename, rect, RotateFaceFeats(faceData, _transform), faceRect, photoId);
                }
            }

            return retVal;
        }

        //static private bool ProcessFaceInstance(byte[] facePix, string filename, Rect rect, Point leftEye, Point rightEye, Rect faceRect, int photoId)
        static private bool ProcessFaceInstance(byte[] facePix, string filename, Rect rect, FaceDisp.FaceData faceData, Rect faceRect, int photoId)
        {
            facePix = ConvertToGreyScale(facePix);

            //facePix = NormalizeBySum(facePix);
            facePix = Normalize(facePix, faceRect, 1);

            if (_action == ProgActions.GenerateEyeFaceData)
            {
                byte[] data = MakeFaceData(facePix);
                WriteTrainDataAndTargets(data, rect, faceData);
            }
            else if (_action == ProgActions.FaceDetect)
            {
                //RunDetection(filename, rect, FaceFeatureToScaledPoint(faceData.TrueLeftEye, rect),
                //                            FaceFeatureToScaledPoint(faceData.TrueRightEye, rect), ref facePix, faceRect);
                RunDetection(filename, rect, faceData, ref facePix, faceRect);
            }
            else if (_action == ProgActions.GenerateEyePatchData)
            {
                WriteFeaturePatches(rect, faceData, ref facePix);
            }
            //_outStream.Flush();
            return true;
        }

        static private FaceDisp.FaceData RotateFaceFeats(FaceDisp.FaceData faceData, TransformSample trans)
        {
            Rect faceRect = new Rect(faceData.faceRect.X, faceData.faceRect.Y, faceData.faceRect.Width, faceData.faceRect.Height);

            return new FaceDisp.FaceData(RotateRawEye(faceData.TrueLeftEye, faceRect, trans),
                                            RotateRawEye(faceData.TrueRightEye, faceRect, trans),
                                            RotateRawEye(faceData.Nose, faceRect, trans),
                                            RotateRawEye(faceData.LeftMouth, faceRect, trans),
                                            RotateRawEye(faceData.RightMouth, faceRect, trans));
        }

        /// <summary>
        /// Rotate eyes in raw co-ords. First normalize then rotate then nunormalize
        /// </summary>
        /// <param name="eye"></param>
        /// <param name="faceRect"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        static private Point RotateRawEye(System.Windows.Point eye, Rect faceRect, TransformSample trans)
        {
            Point point = FaceFeatureToScaledPoint(eye, faceRect);
            point = RotateEye(point, trans);
            return ScalePointToFaceFeature(point, faceRect);


        }
        static private Point RotateEye(System.Windows.Point eye, TransformSample trans)
        {
            double theta = trans.ThetaRad;
            System.Windows.Point newCentre = new System.Windows.Point();
            newCentre.X = (eye.X - 0.5) * Math.Cos(theta) - (0.5 - eye.Y) * Math.Sin(theta) + 0.5 - trans.X;
            newCentre.Y = 0.5 - ((0.5 - eye.Y) * Math.Cos(theta) + (eye.X - 0.5) * Math.Sin(theta)) - trans.Y;
            return newCentre;
        }

        static void WriteTrainDataAndTargets(byte[] facePix, Rect rect, FaceDisp.FaceData faceData)
        {
            Point leftEye = FaceFeatureToScaledPoint(faceData.TrueLeftEye, rect);
            Point rightEye = FaceFeatureToScaledPoint(faceData.TrueRightEye, rect);
            Point nose = FaceFeatureToScaledPoint(faceData.Nose, rect);
            Point leftMouth = FaceFeatureToScaledPoint(faceData.LeftMouth, rect);
            Point rightMouth = FaceFeatureToScaledPoint(faceData.RightMouth, rect);

            double[] eyes = { leftEye.X, leftEye.Y, rightEye.X, rightEye.Y, nose.X, nose.Y, leftMouth.X, leftMouth.Y, rightMouth.X, rightMouth.Y};
            _outWriter.WriteSample(facePix, eyes);
        }

        static void WriteTrainDataIdTarget(byte[] facePix, int id)
        {
            double[] targets = { (double)id };
            _outWriter.WriteSample(facePix, targets);
        }

        static void WriteEyePatches(Point leftEye, Point rightEye, ref byte[] facePix)
        {
            WriteOneEye(leftEye, 0, _numPatchFeatures, ref facePix);
            WriteOneEye(rightEye, 0, _numPatchFeatures, ref facePix);

            Rect leftRect = new Rect(Math.Max(0, leftEye.X * _faceDisplayWidth - 3 * _eyePatchWidth / 2 + 1),
                                    Math.Max(0, leftEye.Y * _faceDisplayHeight - 3 * _eyePatchHeight / 2 + 1),
                                    2 * _eyePatchWidth - 1,
                                    2 * _eyePatchHeight - 1);

            Rect rightRect = new Rect(Math.Max(0, rightEye.X * _faceDisplayWidth - 3 * _eyePatchWidth / 2 + 1),
                                    Math.Max(0, rightEye.Y * _faceDisplayHeight - 3 * _eyePatchHeight / 2 + 1),
                                    2 * _eyePatchWidth - 1,
                                    2 * _eyePatchHeight - 1);

            Random rand = new Random(_randSeed);
            Point nonEye = new Point();
            double validFaceWidth = _faceDisplayWidth - _eyePatchWidth;
            double validFaceHeight = _faceDisplayHeight - _eyePatchHeight;

            double[] targets = new double[_numPatchFeatures];       // Non positions are all zero

            for (int i = 0; i < _nonEyePatchCount; ++i)
            {
                do
                {
                    nonEye.Y = rand.NextDouble() * validFaceHeight;
                    nonEye.X = rand.NextDouble() * validFaceWidth;
                } while (true == leftRect.Contains(nonEye) || true == rightRect.Contains(nonEye));
                Rect patchPos = new Rect(nonEye.X, nonEye.Y, _eyePatchWidth, _eyePatchHeight);
                WriteEyePatch(patchPos, targets, ref facePix);
            }

        }

        static void WriteFeaturePatches(Rect rect, FaceDisp.FaceData faceData, ref byte[] facePix)
        {
            Point leftEye = FaceFeatureToScaledPoint(faceData.TrueLeftEye, rect);
            Point rightEye = FaceFeatureToScaledPoint(faceData.TrueRightEye, rect);
            Point nose = FaceFeatureToScaledPoint(faceData.Nose, rect);
            Point leftMouth = FaceFeatureToScaledPoint(faceData.LeftMouth, rect);
            Point rightMouth = FaceFeatureToScaledPoint(faceData.RightMouth, rect);

            WriteOneEye(leftEye, 0, _numPatchFeatures, ref facePix);
            WriteOneEye(rightEye, 0, _numPatchFeatures, ref facePix);
            WriteOneEye(nose, 1, _numPatchFeatures, ref facePix);
            WriteOneEye(leftMouth, 2, _numPatchFeatures, ref facePix);
            WriteOneEye(rightMouth, 3, _numPatchFeatures, ref facePix);

            Rect leftEyeRect = GetPatchRect(leftEye);
            Rect rightEyeRect = GetPatchRect(rightEye);
            Rect noseRect = GetPatchRect(nose);
            Rect leftMouthRect = GetPatchRect(leftMouth);
            Rect rightMouthRect = GetPatchRect(rightMouth);

            Rect rightRect = new Rect(Math.Max(0, rightEye.X * _faceDisplayWidth - 3 * _eyePatchWidth / 2 + 1),
                                    Math.Max(0, rightEye.Y * _faceDisplayHeight - 3 * _eyePatchHeight / 2 + 1),
                                    2 * _eyePatchWidth - 1,
                                    2 * _eyePatchHeight - 1);

            Random rand = new Random(_randSeed);
            Point nonFeat = new Point();
            double validFaceWidth = _faceDisplayWidth - _eyePatchWidth;
            double validFaceHeight = _faceDisplayHeight - _eyePatchHeight;

            double[] targets = new double[_numPatchFeatures];       // Non positions are all zero

            for (int i = 0; i < _nonEyePatchCount; ++i)
            {
                do
                {
                    nonFeat.Y = rand.NextDouble() * validFaceHeight;
                    nonFeat.X = rand.NextDouble() * validFaceWidth;
                } while (true == leftEyeRect.Contains(nonFeat) || true == rightEyeRect.Contains(nonFeat) ||
                    true == noseRect.Contains(nonFeat) ||
                    true == leftMouthRect.Contains(nonFeat) || true == rightMouthRect.Contains(nonFeat));
                Rect patchPos = new Rect(nonFeat.X, nonFeat.Y, _eyePatchWidth, _eyePatchHeight);
                WriteEyePatch(patchPos, targets, ref facePix);
            }

        }

        static Rect GetPatchRect(Point pt)
        {
            return new Rect(Math.Max(0, pt.X * _faceDisplayWidth - 3 * _eyePatchWidth / 2 + 1),
                            Math.Max(0, pt.Y * _faceDisplayHeight - 3 * _eyePatchHeight / 2 + 1),
                            2 * _eyePatchWidth - 1,
                            2 * _eyePatchHeight - 1);
        }

        /// <summary>
        /// Exhaustively writes patches that overlap with the patch centred around point eye.  
        /// Patches are of size {_eyePatchWidth x _eyePatchHeight}. Code assumes the
        /// patches are of odd dimension so to enable easy centreing
        /// </summary>
        /// <param name="eye">Center of data</param>
        /// <param name="facePix">Face data</param>
        static void WriteOneEye(Point eye, int patchId, int patchCount, ref byte[] facePix)
        {
            int eyeCenterX = (int)Math.Round(eye.X * _faceDisplayWidth);
            int eyeCenterY = (int)Math.Round(eye.Y * _faceDisplayHeight);

            int patchTop = (int)Math.Ceiling(eyeCenterY - 3 * _eyePatchHeight / 2 + 1);
            int maxPatchTop = (int)Math.Min(_faceDisplayHeight, eyeCenterY + _eyePatchHeight / 2);
            int maxPatchLeft = (int)Math.Min(_faceDisplayWidth, eyeCenterX + _eyePatchWidth / 2);
            int patchLeft = (int)Math.Ceiling(eyeCenterX - 3 * _eyePatchWidth / 2 + 1);
            int refPatchTop = (int)Math.Ceiling(eyeCenterY - _eyePatchHeight / 2);
            int refPatchLeft = (int)Math.Ceiling(eyeCenterX - _eyePatchWidth / 2);

            if (patchTop < 0 || patchLeft < 0 || 
                maxPatchTop > _faceDisplayHeight || maxPatchLeft > _faceDisplayWidth)
            {
                return;
            }

            Rect patchPos = new Rect();
            patchPos.Width = _eyePatchWidth;
            patchPos.Height = _eyePatchHeight;
            double[] eyeProbArray = new double[patchCount];

            for (; patchTop <= maxPatchTop; ++patchTop)
            {
                patchPos.Y = patchTop;
                int left = patchLeft;

                for (; left <= maxPatchLeft; ++left)
                {
                    double eyeProb = Overlap(patchTop, refPatchTop, (int)_eyePatchHeight) *
                                    Overlap(left, refPatchLeft, (int)_eyePatchWidth) /
                                    _eyePatchWidth / _eyePatchHeight;

                    patchPos.X = left;
                    eyeProbArray[patchId] = eyeProb;
                    WriteEyePatch(patchPos, eyeProbArray, ref facePix);
                }
            }

        }

        /// <summary>
        /// Return number of pixels overlap between two row/column of pixels, both of
        /// same length starting at two offsets
        /// </summary>
        /// <param name="start1">Start of column 1</param>
        /// <param name="start2">Start column 2</param>
        /// <param name="len">Length of both pixedls</param>
        /// <returns>Number of pixel overlap</returns>
        static private double Overlap(int start1, int start2, int len)
        {
            double ret = len - Math.Abs(start1 - start2);
            ret = Math.Max(0, ret);
            return ret;
        }

        /// <summary>
        /// Write a single train/test case for classifying patches
        /// as eye/NoEye. Target are writen as real values indicating the "probability"
        /// the patch is eye. The Prob is computed as the relative distance of eyePosition from
        /// patch centre
        /// </summary>
        /// <param name="patchPos">Patch location in the stream</param>
        /// <param name="eyeProb">"probability"  patch is an eye</param>
        /// <param name="facePix">Datastream representing the entire face</param>
        static void WriteEyePatch(Rect patchPos, double[] eyeProbArray, ref byte[] facePix)
        {
            if (patchPos.Y + patchPos.Height < _faceDisplayHeight &&
                patchPos.X + patchPos.Width < _faceDisplayWidth)
            {
                int cColorPlane = (int)(facePix.Length / _faceDisplayWidth / _faceDisplayHeight);
                int patchWidth = (int)Math.Round(patchPos.Width * _faceDisplayWidth);
                int patchHeight = (int)Math.Round(patchPos.Height * _faceDisplayHeight);
                int patchLeft = (int)Math.Round(patchPos.X * _faceDisplayWidth);
                int patchTop = (int)Math.Round(patchPos.Y * _faceDisplayHeight);

                byte[] data = new byte[(int)(cColorPlane * patchPos.Height * patchPos.Width)];
                int id = 0;

                for (int iColor = 0; iColor < cColorPlane; ++iColor)
                {
                    for (int iy = 0; iy < patchPos.Height; ++iy)
                    {
                        //int iPix = (int) Math.Round(iy * _faceDisplayWidth + patchLeft);
                        int iPix = (int)Math.Round((patchPos.Y + iy) * _faceDisplayWidth + patchPos.X);

                        for (int ix = 0; ix < patchPos.Width; ++ix)
                        {
                            //_outStream.Write("{0} ", facePix[iPix + iColor]);
                            data[id++] = facePix[iPix + iColor];
                            iPix += cColorPlane;
                        }
                    }
                }

                _outWriter.WriteSample(data, eyeProbArray);

            }
        }
        //static void RunDetection(string filename, Rect rect, Point leftEye, Point rightEye, ref byte [] facePix, Rect faceRect)
        static void RunDetection(string filename, Rect rect, FaceDisp.FaceData faceData, ref byte [] facePix, Rect faceRect)
        {
            EyeDetect eyeDetect = new EyeDetect();
            int byteCountPerPix = (int)(facePix.Length / faceRect.Width / faceRect.Height);

            bool isSuccess = eyeDetect.SetAlgorithm(_algo, _algoData);

            if (true == isSuccess)
            {
                EyeDetectResult eyeResult = eyeDetect.Detect(facePix, (int)_faceDisplayWidth, (int)_faceDisplayWidth);

                _outStream.WriteLine("{0}", filename);
                _outStream.Write("{0} {1} {2} {3} ", (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

                if (faceData.TrueLeftEye.X > 1.0)
                {
                    Point leftEye = FaceFeatureToScaledPoint(faceData.TrueLeftEye, rect);
                    Point rightEye = FaceFeatureToScaledPoint(faceData.TrueRightEye, rect);

                    _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", leftEye.X, leftEye.Y, rightEye.X, rightEye.Y);
                }
                else
                {
                    _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", faceData.TrueLeftEye.X, faceData.TrueLeftEye.Y, faceData.TrueRightEye.X, faceData.TrueRightEye.Y);
                }
                _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", eyeResult.LeftEye.X / _faceDisplayWidth, eyeResult.LeftEye.Y / _faceDisplayWidth,
                                        eyeResult.RightEye.X / _faceDisplayWidth, eyeResult.RightEye.Y / _faceDisplayWidth);

                FaceFeatureResult res = eyeResult as FaceFeatureResult;
                if (null != res)
                {
                    if (faceData.Nose.X > 1.0)
                    {
                        Point nose = FaceFeatureToScaledPoint(faceData.Nose, rect);
                        Point leftMouth = FaceFeatureToScaledPoint(faceData.LeftMouth, rect);
                        Point rightMouth = FaceFeatureToScaledPoint(faceData.RightMouth, rect);

                        _outStream.Write("{0:F3} {1:F3} ", nose.X, nose.Y);
                        _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", leftMouth.X, leftMouth.Y, rightMouth.X, rightMouth.Y);
                    }
                    else
                    {
                        _outStream.Write("{0:F3} {1:F3} ", faceData.Nose.X, faceData.Nose.Y);
                        _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", faceData.LeftMouth.X, faceData.LeftMouth.Y, faceData.RightMouth.X, faceData.RightMouth.Y);
                    }
                    _outStream.Write("{0:F3} {1:F3} ", res.Nose.X / _faceDisplayWidth, res.Nose.Y / _faceDisplayWidth);
                    _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", res.LeftMouth.X / _faceDisplayWidth, res.LeftMouth.Y / _faceDisplayWidth,
                                            res.RightMouth.X / _faceDisplayWidth, res.RightMouth.Y / _faceDisplayWidth);

                }
                if (_maxTransformCount > 0)
                {
                    _outStream.Write("{0:F3} {1:F3} {2:F3}", _transform.Theta, _transform.X, _transform.Y);
                }
                _outStream.WriteLine();
            }
            else
            {
                _outStream.WriteLine("Detection failed on {0}", filename);
            }
        }
        /// <summary>
        /// Select a transformed FacePatch from current image
        /// Amount of rotation specified in degrees
        /// </summary>
        /// <param name="dataPixs">Byte data from current image</param>
        /// <param name="faceRect">Where the faceRect is in the the image</param>
        /// <param name="theta">Amount to rotate (degrees)</param>
        /// <returns></returns>
        static private byte[] SelectTransformPatch(byte[] dataPixs, Rect faceRect, TransformSample trans)
        {

            int bytePerPixel = _bitmap.Format.BitsPerPixel / 8;
            int dataStride = _bitmap.PixelWidth * bytePerPixel;
            Rect sourceRect = new Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);
            Rect destRect = new Rect(0, 0, _faceDisplayWidth, _faceDisplayHeight);
            int destStride = (int)destRect.Width * bytePerPixel;

            byte[] facePixs = TransformSample.ExtractTransformNormalizeFace(dataPixs, sourceRect, bytePerPixel, dataStride,
             faceRect, destRect, destStride, trans);

            return facePixs;

        }


        static private Point FaceFeatureToScaledPoint(System.Windows.Point featLoc, Rect faceRect)
        {
            Point retPoint = new Point(featLoc.X, featLoc.Y);

            retPoint.X = (retPoint.X - faceRect.X) / faceRect.Width;
            retPoint.Y = (retPoint.Y - faceRect.Y) / faceRect.Height; ;

            return retPoint;
        }

        static private Point ScalePointToFaceFeature(System.Windows.Point featLoc, Rect faceRect)
        {
            Point retPoint = new Point(featLoc.X, featLoc.Y);

            retPoint.X = retPoint.X * faceRect.Width + faceRect.X;
            retPoint.Y = retPoint.Y * faceRect.Height + faceRect.Y;

            return retPoint;
        }
    }
}
