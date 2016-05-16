using LibFaceData;
using System;
using System.Collections.Generic;
using System.Text;
using DetectionManagedLib;
using System.Windows;
using System.IO;
using Dpu.ImageProcessing;
using ShoNS.Array;
using NNTrainer;

namespace GenPositionData
{
    /// <summary>
    /// MIsc reading reporting data for FaceReco experiments
    /// </summary>
    class FaceRecoGen : BaseReco
    {
        enum ProgActions { ReportPositions, DetectPhotos, PreDetectPhotos, None };
        private FaceDetector _detector = null;
        private EyeDetect _eyeDetect;
        private StreamWriter _outStream;
        private bool _doPatchGeneration = false;
        private Rect _targetRect = new Rect(0, 0, 1.0, 1.0);
        private string _eyeDetectPath;
        private Rect _eyeDetectFaceRect = new Rect(0, 0, 41, 41);
        private int _faceDetectPixCount;
        private System.Windows.Point _normLeftEye = new System.Windows.Point(25.0 / 80, 30.0 / 80.0);
        private System.Windows.Point _normRightEye = new System.Windows.Point(55.0 / 80, 30.0 / 80.0);
        private System.Windows.Point _normCenter = new System.Windows.Point(40.0 / 80, 40.0 / 80.0);
        private System.Windows.Point _normNose = new System.Windows.Point(0.50F, 0.60F);
        private System.Windows.Point _leftNormNose = new System.Windows.Point(0.50F - 5.0F / 41.0F, 0.60F);
        private System.Windows.Point _rightNormNose = new System.Windows.Point(0.50F + 5.0F / 41.0F, 0.60F);
        private System.Windows.Point _normLeftMouth = new System.Windows.Point(0.33F, 0.76F);
        private System.Windows.Point _normRightMouth = new System.Windows.Point(0.66F, 0.76F);
        private bool _skipFaceDetect = false;
        private int _dumpJpgCount = -1;
        private byte[] _dataPixs = null;        // Current photo
        private byte[] _facePixs = null;        // Current greyscale face
        private byte[] _normFacePixs = null;    // Current Normaloized face (greyscale)
        private TrainDataFileWriter _normFaceWriter;
        private TrainDataFileWriter _leftEyeWriter;
        private TrainDataFileWriter _rightEyeWriter;
        private TrainDataFileWriter _leftNoseWriter;
        private TrainDataFileWriter _rightNoseWriter;
        private TrainDataFileWriter _leftMouthWriter;
        private TrainDataFileWriter _rightMouthWriter;
        private TrainDataFileWriter _featureWriter;

        public FaceRecoGen(string[] args, int iArg)
        {
            float detectionThresh = 0.0F;
            string classifierPath = @"G:\src\Face\FaceDetect\Classifier\classifier.txt";
            string outFile = "out.txt";
            _faceDetectPixCount = (int)(_eyeDetectFaceRect.Width * _eyeDetectFaceRect.Height);

            if (args.Length < 1)
            {
                return;
            }
            bool doExtra = false;


            ProgActions action = ProgActions.None;
            string dataFilePrefix = "";

            while (iArg < args.Length - 1)
            {
                switch (args[iArg++].ToLower())
                {
                    case "-detectpath":
                        classifierPath = args[iArg++];
                        break;

                    case "-eyedetectpath":
                        _eyeDetectPath = args[iArg++];
                        break;

                    case "-detectphoto":
                        action = ProgActions.DetectPhotos;
                        break;

                    case "-predetectphoto":
                        action = ProgActions.PreDetectPhotos;
                        break;

                    case "-generatepatch":
                        _doPatchGeneration = true;
                        break;

                    case "-generatejpg":
                        _dumpJpgCount = 0;
                        break;

                    case "-reportusingfeats":
                        action = ProgActions.ReportPositions;
                        break;
        
                    case "-writebinary":
                        _dataFileMode = TrainDataFileWriter.ModeEnum.Binary;
                        break;

                    case "-writetext":
                        _dataFileMode = TrainDataFileWriter.ModeEnum.Text;
                        break;

                    case "-datafileprefix":
                        dataFilePrefix = args[iArg++];
                        break;

                    case "-skipfacedetect":
                        _skipFaceDetect = true;
                        break;

                    case "-addextra":
                        doExtra = true;
                        break;

                    case "-out":
                        outFile = args[iArg++];
                        break;

                    default:
                        Console.WriteLine("Unrecognized option {0}", args[iArg - 1]);
                        return;

                }

            }
            string filename = @"G:\src\BingApi\images\tmp.txt";

            if (iArg < args.Length)
            {
                filename = args[iArg];
            }
            _detector = new FaceDetector(classifierPath, true, detectionThresh);
            _outStream = new StreamWriter(outFile);

            if (true == _doPatchGeneration)
            {
                _normFaceWriter = new TrainDataFileWriter(dataFilePrefix + "normFace.dat", _dataFileMode);

                if (doExtra == true)
                {
                    _featureWriter = new TrainDataFileWriter(dataFilePrefix + "featuresFace.dat", _dataFileMode);
                }

                //_leftEyeWriter = new TrainDataFileWriter(dataFilePrefix + "leftEye.dat", _dataFileMode);
                //_rightEyeWriter = new TrainDataFileWriter(dataFilePrefix + "rightEye.dat", _dataFileMode);
                //_leftMouthWriter = new TrainDataFileWriter(dataFilePrefix + "leftMouth.dat", _dataFileMode);
                //_rightMouthWriter = new TrainDataFileWriter(dataFilePrefix + "rightMouth.dat", _dataFileMode);
                //_leftNoseWriter = new TrainDataFileWriter(dataFilePrefix + "leftNose.dat", _dataFileMode);
                //_rightNoseWriter = new TrainDataFileWriter(dataFilePrefix + "rightNose.dat", _dataFileMode);
            }

            try
            {
                if (action == ProgActions.ReportPositions)
                {
                    ReportUsingFeatures(filename);
                }
                else if (action == ProgActions.DetectPhotos)
                {
                    ReportPhotos(filename);
                }
                else if (action == ProgActions.PreDetectPhotos)
                {
                    ReportPreDetectPhotos(filename);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }

            if (true == _doPatchGeneration)
            {
                if (null != _normFaceWriter)
                {
                    _normFaceWriter.Dispose();
                }

                if (null != _featureWriter)
                {
                    _featureWriter.Dispose();
                }

                if (null != _leftEyeWriter)
                {
                    _leftEyeWriter.Dispose();
                    _rightEyeWriter.Dispose();
                    _leftMouthWriter.Dispose();
                    _rightMouthWriter.Dispose();
                    _leftNoseWriter.Dispose();
                    _rightNoseWriter.Dispose();
                }
            }

            _outStream.Close();
        }

        private void ReportUsingFeatures(string suiteFile)
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
                imageCount += ReportFileUsingFeats(image.FileName, image.FeaturePtsList);
                ++processedCount;
                Console.Write("\rGenerated {0} Images. Processed {1} / {2} Images", imageCount, processedCount, labelCol.ImgList.Count);
            }
            Console.WriteLine("");
        }

        private void ReportPhotos(string suiteFile)
        {
            FileInfo fileInfo = new FileInfo(suiteFile);

            if (false == fileInfo.Exists)
            {
                return;
            }
            int imageCount = 0;
            int processedCount = 0;
            int iLine = 0;
            using (StreamReader sr = File.OpenText(suiteFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {

                    ++iLine;
                    try
                    {
                        imageCount += RunDetectAndReport(line, null);
                        ++processedCount;
                        Console.Write("\rGenerated {0} Images. Processed {1}  Images", imageCount, processedCount);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error processing  {0}: {1}", line, e.Message);
                    }
                }
            }
            Console.WriteLine("");

        }

        private void ReportPreDetectPhotos(string suiteFile)
        {
            FaceDataFile suiteReader = null;

            try
            {
                suiteReader = new FaceDataFile(suiteFile, LibFaceData.FaceData.FaceDataTypeEnum.EyeDetect);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }

            if (null == suiteReader)
            {
                return;
            }

            FaceData faceData = null;
            int imageCount = 0;
            int processedCount = 0;
            while ((faceData = suiteReader.GetNext()) != null)
            {
                if (true == _skipFaceDetect)
                {
                    imageCount += ReportDetectedFace(faceData.Filename, faceData.FaceWindowsRect, faceData);
                }
                else
                {
                    imageCount += RunDetectAndReport(faceData.Filename, faceData);
                }
                ++processedCount;
                Console.Write("\rGenerated {0} Images. Processed {1}  Images", imageCount, processedCount);
            }
            Console.WriteLine("");
        }

        private int ReportFileUsingFeats(string imageFileName, List<FeaturePts> featureList)
        {
            DetectionResult detectionResult = _detector.DetectObject(imageFileName);
            List<ScoredRect> scoredResultList = detectionResult.GetMergedRectList(0.0F);
            int imageCount = 0;

            if (null != featureList && scoredResultList.Count > 0)
            {
                if (true != ReadPhoto(imageFileName))
                {
                    return 0;
                }

                foreach (FeaturePts features in featureList)
                {
                    Point leftEye = FeaturePtToPoint((System.Drawing.PointF)features.ptLeftEye);
                    Point rightEye = FeaturePtToPoint((System.Drawing.PointF)features.ptRightEye);
                    Point nose = FeaturePtToPoint((System.Drawing.PointF)features.ptNose);
                    Point leftMouth = FeaturePtToPoint((System.Drawing.PointF)features.ptLeftMouth);
                    Point rightMouth = FeaturePtToPoint((System.Drawing.PointF)features.ptRightMouth);

                    foreach (ScoredRect scoredRect in scoredResultList)
                    {
                        Rect rect = new Rect();

                        rect.X = scoredRect.X;
                        rect.Y = scoredRect.Y;
                        rect.Width = scoredRect.Width;
                        rect.Height = scoredRect.Height;

                        if (rect.Contains(leftEye) && rect.Contains(rightEye))
                        {
                            _outStream.WriteLine("{0}", imageFileName);
                            _outStream.Write("{0} {1} {2} {3} ", (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                            _outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", leftEye.X, leftEye.Y, rightEye.X, rightEye.Y);

                            if (true == _doPatchGeneration)
                            {
                                if (false == NormalizeFaceWithEyePos(rect, leftEye, rightEye, null))
                                {
                                    continue;
                                }

                                byte[] target = new byte[1];
                                target[0] = 0;

                                _normFaceWriter.WriteSample(_normFacePixs, target);


                                Rect largeRect = new Rect(0, 0, 10.0, 10.0);
                                //Rect largeRect = new Rect(0, 0, 40.0, 40.0);

                                //WritePatch(_leftEyeWriter, _normLeftEye, largeRect, 0);
                                //WritePatch(_rightEyeWriter, _normRightEye, largeRect, 0);
                                //WritePatch(_leftMouthWriter, _normLeftMouth, largeRect, 0);
                                //WritePatch(_rightMouthWriter, _normRightMouth, largeRect, 0);
                                //WritePatch(_leftNoseWriter, _leftNormNose, largeRect, 0);
                                //WritePatch(_rightNoseWriter, _rightNormNose, largeRect, 0);

                            }

                            leftEye.X -= rect.X;
                            leftEye.Y -= rect.Y;
                            rightEye.X -= rect.X;
                            rightEye.Y -= rect.Y;

                            double[,] affine = GetFaceAffine(rect, leftEye, rightEye, _targetRect, _normLeftEye, _normRightEye);

                            ReportNormPoint(affine, (System.Drawing.PointF)features.ptLeftEye, rect, _targetRect);
                            ReportNormPoint(affine, (System.Drawing.PointF)features.ptRightEye, rect, _targetRect);
                            ReportNormPoint(affine, (System.Drawing.PointF)features.ptNose, rect, _targetRect);
                            ReportNormPoint(affine, (System.Drawing.PointF)features.ptLeftMouth, rect, _targetRect);
                            ReportNormPoint(affine, (System.Drawing.PointF)features.ptRightMouth, rect, _targetRect);

                            //ReportNormPoint(affine, _normLeftEye, rect, _targetRect);
                            //ReportNormPoint(affine, _normRightEye, rect, _targetRect);
                            //ReportNormPoint(affine, _normNose, rect, _targetRect);
                            //ReportNormPoint(affine, _normLeftMouth, rect, _targetRect);
                            //ReportNormPoint(affine, _normRightMouth, rect, _targetRect);

                            
                            _outStream.WriteLine();
                            _outStream.Flush();

                            ++imageCount;
                            break;
                        }
                    }
                }
            }

            return imageCount;
        }

        private Point FeaturePtToPoint(System.Drawing.PointF featurePt)
        {
            Point retPoint = new Point(featurePt.X, featurePt.Y);
            return retPoint;
        }
        private int RunDetectAndReport(string imageFileName, FaceData faceData)
        {
            DetectionResult detectionResult = null;
            List<ScoredRect> scoredResultList = null;

            try
            {
                _detector.SetTargetDimension(640, 480);
                detectionResult = _detector.DetectObject(imageFileName);
                scoredResultList = detectionResult.GetMergedRectList(0.0F);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("{0}: {1}", imageFileName, e.Message);
                return 0;
            }

            if (scoredResultList.Count <= 0)
            {
                return 0;
            }

            if (true != ReadPhoto(imageFileName))
            {
                return 0;
            }


            int imageCount = 0;
            foreach (ScoredRect scoredRect in scoredResultList)
            {
                Rect rect = new Rect();

                rect.X = scoredRect.X;
                rect.Y = scoredRect.Y;
                rect.Width = scoredRect.Width;
                rect.Height = scoredRect.Height;

                imageCount += ReportDetectedFace(imageFileName, rect, faceData);
            }

            return imageCount;
        }

        private int ReportDetectedFace(string imageFileName, Rect rect, FaceData faceData)
        {
            int imageCount = 0;

            if (true != ReadPhoto(imageFileName))
            {
                return 0;
            }

            if (true != ExtractFaceForEyeDetection(rect))
            {
                return imageCount;
            }

            _facePixs = NormalizeBySum(_facePixs);



            System.Windows.Point trueLeftEye;
            System.Windows.Point trueRightEye;


            System.Windows.Point leftEye;
            System.Windows.Point rightEye;

            if (false == DoEyeDetect(rect, out leftEye, out rightEye))
            {
                return imageCount;
            }

            if (null != faceData)
            {
                trueLeftEye = faceData.TrueLeftEye;
                trueRightEye = faceData.TrueRightEye;
            }
            else
            {
                trueLeftEye = leftEye;
                trueRightEye = rightEye;
            }

            if (true == _skipFaceDetect ||
                (rect.Contains(trueLeftEye) && rect.Contains(trueRightEye)))
            {
                _outStream.WriteLine("{0}", imageFileName);
                _outStream.Write("{0} {1} {2} {3} ", (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                //_outStream.Write("{0:F3} {1:F3} {2:F3} {3:F3} ", leftEye.X, leftEye.Y, rightEye.X, rightEye.Y);


                if (true == _doPatchGeneration)
                {
                    //if (false == NormalizeFaceWithEyePos(rect, NormalizePosToAbs(trueLeftEye, rect), NormalizePosToAbs(trueRightEye, rect), faceData))
                    //{
                    //    return imageCount;
                    //}
                    double[] target = new double[1];
                    if (null != faceData)
                    {
                        target[0] = faceData.PhotoId;
                    }
                    else
                    {
                        target[0] = 0;
                    }



                    if (false == NormalizeFaceWithEyePos(rect, leftEye, rightEye, faceData))
                    {
                        return imageCount;
                    }

                    _normFaceWriter.WriteSample(_normFacePixs, target);


                    //Rect largeRect = new Rect(0, 0, 10.0, 10.0);
                    Rect largeRect = new Rect(0, 0, 20.0, 20.0);
                    //Rect largeRect = new Rect(0, 0, 40.0, 40.0);

                    if (null != _featureWriter)
                    {
                        WritePatchCombined(_featureWriter, _normCenter, largeRect, target);
                        //WritePatchCombined(_featureWriter, _normLeftEye, largeRect, target);
                        //WritePatchCombined(_featureWriter, _normRightEye, largeRect, target);
                        //WritePatchCombined(_featureWriter, _normLeftMouth, largeRect, target);
                        //WritePatchCombined(_featureWriter, _normRightMouth, largeRect, target);
                        //WritePatchCombined(_featureWriter, _leftNormNose, largeRect, target);
                        //WritePatchCombined(_featureWriter, _rightNormNose, largeRect, target);
                        _featureWriter.WriteNewTarget(target);
                    }

                    if (null != _leftEyeWriter)
                    {
                        WritePatch(_leftEyeWriter, _normLeftEye, largeRect, target);
                        WritePatch(_rightEyeWriter, _normRightEye, largeRect, target);
                        WritePatch(_leftMouthWriter, _normLeftMouth, largeRect, target);
                        WritePatch(_rightMouthWriter, _normRightMouth, largeRect, target);
                        WritePatch(_leftNoseWriter, _leftNormNose, largeRect, target);
                        WritePatch(_rightNoseWriter, _rightNormNose, largeRect, target);
                    }
                }

                double[,] affine = GetFaceAffine(rect, leftEye, rightEye, _targetRect, _normLeftEye, _normRightEye);

                ReportPoint(affine, _normLeftEye, rect, _targetRect);
                ReportPoint(affine, _normRightEye, rect, _targetRect);
                ReportUnormalizedPoint(affine, leftEye, rect, _targetRect);
                ReportUnormalizedPoint(affine, rightEye, rect, _targetRect);


                //ReportNormPoint(affine, trueLeftEye, rect, _targetRect);
                //ReportNormPoint(affine, trueRightEye, rect, _targetRect);
                //ReportNormPoint(affine, leftEye, rect, _targetRect);
                //ReportNormPoint(affine, rightEye, rect, _targetRect);

                //ReportPoint(affine, _normLeftEye, rect, _targetRect);
                //ReportPoint(affine, _normRightEye, rect, _targetRect);
                //ReportPoint(affine, _normNose, rect, _targetRect);
                //ReportPoint(affine, _normLeftMouth, rect, _targetRect);
                //ReportPoint(affine, _normRightMouth, rect, _targetRect);

                if (faceData != null)
                {
                    _outStream.Write("{0}", faceData.PhotoId);
                }

                _outStream.WriteLine();
                _outStream.Flush();

                ++imageCount;
            }

            return imageCount;
        }

        /// <summary>
        /// Write single patch as an instance
        /// </summary>
        /// <param name="writer">Where to write</param>
        /// <param name="center">Patch centre</param>
        /// <param name="rect">Patch size</param>
        /// <param name="target">Target value</param>
        private void WritePatch(TrainDataFileWriter writer, Point center, Rect rect, double [] target)
        {
            rect.X = center.X*_eyeDetectFaceRect.Width - rect.Width / 2.0;
            rect.Y = center.Y*_eyeDetectFaceRect.Height - rect.Height / 2.0;
            Rect faceRect = new Rect(0, 0, _faceDisplayWidth, _faceDisplayHeight);

            byte[] pixs = ExtractImagePatch(_normFacePixs, faceRect, rect);

            if (null == pixs)
            {
                string extractString = string.Format("({0:F3}, {1:F3}, {2:F3}, {3:F3})", rect.Left, rect.Top, rect.Right, rect.Bottom);
                string srcRectString = string.Format("({0:F3}, {1:F3}, {2:F3}, {3:F3})", faceRect.Left, faceRect.Top, faceRect.Right, faceRect.Bottom);
                throw new Exception("WritePatch: cannot extract patch " + extractString + " from source rect " + srcRectString);
            }
            writer.WriteSample(pixs, target);
        }

        /// <summary>
        /// Write all patches to a single (combined file)
        /// </summary>
        /// <param name="writer">Where to write</param>
        /// <param name="center">Patch centre</param>
        /// <param name="rect">Patch size</param>
        /// <param name="target">Unused</param>
        private void WritePatchCombined(TrainDataFileWriter writer, Point center, Rect rect, double[] target)
        {
            rect.X = center.X * _faceDisplayWidth - rect.Width / 2.0;
            rect.Y = center.Y * _faceDisplayHeight - rect.Height / 2.0;
            Rect targetRect = new Rect(0, 0, _faceDisplayWidth, _faceDisplayHeight);

            byte[] pixs = ExtractImagePatch(_normFacePixs, targetRect, rect);

            //writer.WriteSample(pixs, target);
            if (writer.PartialCount > 1)
            {
                writer.WriteDataContinue(pixs);
            }
            else
            {
                writer.WriteNewInput(pixs);
            }
        }

        private bool ReadPhoto(string imageFileName)
        {

            try
            {
                _dataPixs = CreateMainBitMap(imageFileName);
            }
            catch (Exception)
            {
                Console.WriteLine("\nFailed to createBitmap for {0}", imageFileName);
                return false;
            }

            if (null != _dataPixs)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Extract a face for EyeDection. The extracted face is 
        /// of size _eyeDetectFaceRect
        /// </summary>
        /// <param name="faceRect">Original image rect containing the face</param>
        /// <returns>true if success</returns>
        private bool ExtractFaceForEyeDetection(Rect faceRect)
        {
            if (null == _dataPixs)
            {
                return false;
            }


            byte [] origFacePixs = SelectAndNormalizePatch(_dataPixs,
                   new Point(faceRect.X, faceRect.Y), new Point(faceRect.X + faceRect.Width, faceRect.Y),
                   new Point(0, 0), new Point(_eyeDetectFaceRect.Width, 0), _eyeDetectFaceRect);


            _facePixs = ConvertToGreyScale(origFacePixs, origFacePixs.Length / _faceDetectPixCount);

            return true;
        }

        private bool DoEyeDetect(Rect faceRect, out System.Windows.Point leftEye, out System.Windows.Point rightEye)
        {
            leftEye = new System.Windows.Point();
            rightEye = new System.Windows.Point();

            if (null == _facePixs)
            {
                return false;
            }

            if (null == _eyeDetect)
            {
                _eyeDetect = new EyeDetect();
            }
            

            EyeDetectResult eyeResult = _eyeDetect.Detect(_facePixs, (int)_eyeDetectFaceRect.Width, (int)_eyeDetectFaceRect.Height);

            leftEye.X = eyeResult.LeftEye.X * faceRect.Width / _eyeDetectFaceRect.Width + faceRect.X;
            leftEye.Y = eyeResult.LeftEye.Y * faceRect.Height / _eyeDetectFaceRect.Height + faceRect.Y;
            rightEye.X = eyeResult.RightEye.X * faceRect.Width / _eyeDetectFaceRect.Width + faceRect.X;
            rightEye.Y = eyeResult.RightEye.Y * faceRect.Height / _eyeDetectFaceRect.Height + faceRect.Y;

            return true;
        }
        /// <summary>
        /// Extract a face of size _faceDisplayWidth x _faceDisplayHeight, Norm with
        /// the supplied eye positions, convert to greyscale and normalize by sum
        /// </summary>
        /// <param name="faceRect">Original image rect containg face</param>
        /// <param name="leftEye">Detected left eye</param>
        /// <param name="rightEye">Detected right eye</param>
        /// <returns></returns>
        private bool NormalizeFaceWithEyePos(Rect faceRect, System.Windows.Point leftEye, System.Windows.Point rightEye, FaceData faceData)
        {
            Rect targetRect = new Rect(0, 0, _faceDisplayWidth, _faceDisplayHeight);
            int targetRectLen = (int)(targetRect.Width * targetRect.Height);

            Point targetLeftEye = new System.Windows.Point(_normLeftEye.X * _faceDisplayWidth, _normLeftEye.Y * _faceDisplayHeight);
            Point targetRightEye = new System.Windows.Point(_normRightEye.X * _faceDisplayWidth, _normRightEye.Y * _faceDisplayHeight);


            _normFacePixs = SelectAndNormalizePatch(_dataPixs, leftEye, rightEye, targetLeftEye, targetRightEye, targetRect);
            if (_dumpJpgCount >= 0 )
            {
                string faceFile;
                if (null != faceData)
                {
                    faceFile = "_" +faceData.Directory;
                }
                else
                {
                    faceFile = "_face";
                }

                SaveAsJpeg(_normFacePixs, targetRect, _dumpJpgCount.ToString() + faceFile + ".jpg", "keyword", System.Windows.Media.PixelFormats.Rgb24);
                ++_dumpJpgCount;
            }
            //_normFacePixs = ConvertToGreyScale(_normFacePixs, _normFacePixs.Length / targetRectLen);
            _normFacePixs = Normalize(_normFacePixs, targetRect, _normFacePixs.Length / targetRectLen);

            return true;
        }

        private byte[] ExtractImagePatch(byte [] srcPix, Rect srcRect, Rect extractPatch)
        {
            if (srcPix == null)
            {
                return null;
            }

            int bytePerPixel = (int)(srcPix.Length / (srcRect.Width * srcRect.Height));
            int pixCount = (int)(extractPatch.Width * extractPatch.Height) * bytePerPixel;
            if (pixCount <= 0 ||
                extractPatch.Left < srcRect.Left || extractPatch.Right > srcRect.Right ||
                extractPatch.Top < srcRect.Top || extractPatch.Bottom > srcRect.Bottom)
            {
                return null;
            }

            byte[] pixs = new byte[pixCount];

            int iPix = 0;
            for (int bt = 0; bt < bytePerPixel; ++bt)
            {
                int planeOff = bt * (int)(srcRect.Width * srcRect.Height);
                for (int row = (int)extractPatch.Y; row < (int)extractPatch.Bottom; ++row)
                {
                    int rowOff = row * (int)srcRect.Width;
                    for (int col = (int)extractPatch.X; col < (int)extractPatch.Right; ++col)
                    {
                        int colOff = rowOff + col;
                        pixs[iPix++] = srcPix[colOff + planeOff];
                    }
                }
            }

            return pixs;
        }
        /// <summary>
        /// Overload when src is of type System.Drawing.PointF 
        /// Convert to regular point
        /// </summary>
        /// <param name="affine"></param>
        /// <param name="pt"></param>
        /// <param name="srcRect"></param>
        /// <param name="destRect"></param>
        private void ReportNormPoint(double[,] affine, System.Drawing.PointF pt, Rect srcRect, Rect destRect)
        {
            System.Windows.Point src = new Point(pt.X, pt.Y);
            //src.X = (src.X - srcRect.X) / srcRect.Width;
            //src.Y = (src.Y - srcRect.Y) / srcRect.Height;
            ReportNormPoint(affine, src, srcRect, destRect);
        }

        /// <summary>
        /// Report when the src point is normalized [0-1] Unormalized and call
        /// the regular reporting
        /// </summary>
        /// <param name="affine"></param>
        /// <param name="normSrc"></param>
        /// <param name="srcRect"></param>
        /// <param name="destRect"></param>
        private void ReportNormPointNormSrc(double[,] affine, System.Windows.Point normSrc, Rect srcRect, Rect destRect)
        {
            System.Windows.Point src = new Point(normSrc.X * srcRect.Width + srcRect.X, normSrc.Y * srcRect.Height + srcRect.Y);
            ReportNormPoint(affine, src, srcRect, destRect);
        }

        private void ReportNormPoint(double[,] affine, System.Windows.Point src, Rect srcRect, Rect destRect)
        {
            Point dest = TransformPoint(affine, srcRect, src, destRect);
            
            //src.X = (src.X - srcRect.X) / srcRect.Width;
            //src.Y = (src.Y - srcRect.Y) / srcRect.Height;
            //Point dest = src;

            _outStream.Write("{0:F3} {1:F3} ", dest.X, dest.Y);
        }

        private void ReportUnormalizedPoint(double[,] affine, System.Windows.Point src, Rect srcRect, Rect destRect)
        {
            src.X = (src.X - srcRect.Left) / srcRect.Width;
            src.Y = (src.Y - srcRect.Top) / srcRect.Height;
            _outStream.Write("{0:F3} {1:F3} ", src.X, src.Y);
        }

        private void ReportPoint(double[,] affine, System.Windows.Point src, Rect srcRect, Rect destRect)
        {
            //Point dest = TransformPoint(affine, srcRect, src, destRect);
            Point dest = src;
            _outStream.Write("{0:F3} {1:F3} ", dest.X, dest.Y);
        }

        private double[,] GetFaceAffine(Rect origRect, Point origLeftEye, Point origRightEye, Rect targetRect, Point targetLeftEye, Point targetRightEye)
        {

            // Step 1 - Construct the affine transformation
            // Find mapping between orig and desired EyePosAsMatrix locations + a 
            // fake point located at right angles to the vector joing the two eyes
            //INumArray<float> origMat = ArrFactory.FloatArray(3, 2);
            //INumArray<float> targetMat = ArrFactory.FloatArray(3, 3);
            FloatArray targetMat = new FloatArray(3, 2);
            FloatArray origMat = new FloatArray(3, 3);

            LibFaceData.ImageUtils.EyePosAsMatrix(origRect, origLeftEye, origRightEye, ref origMat);
            LibFaceData.ImageUtils.EyePosAsMatrix(targetRect, targetLeftEye, targetRightEye, ref targetMat);
            //targetMat[0, 2] = 1.0F;
            //targetMat[1, 2] = 1.0F;
            //targetMat[2, 2] = 1.0F;
            //SVDFloat svd = new SVDFloat(targetMat);
            //INumArray<float> sss = svd.Solve(origMat);
            origMat[0, 2] = 1.0F;
            origMat[1, 2] = 1.0F;
            origMat[2, 2] = 1.0F; 
            SVDFloat svd = new SVDFloat(origMat);
            INumArray<float> sss = svd.Solve(targetMat);
            INumArray<float> mmm = (INumArray<float>)sss.Transpose();
            DoubleArray da = DoubleArray.From(mmm);
            double[,] affineMat = LibFaceData.ImageUtils.ConvertToDoubleArray(da);

            //double[,] affineMat = ArrFactory.DoubleArray(mmm).ToArray();

            return affineMat;

        }


        private Point TransformPoint(double[,] affine, Rect srcRect, Point src, Rect destRect)
        {
            double x = src.X - srcRect.X - srcRect.Width / 2.0;
            double y = src.Y - srcRect.Y - srcRect.Height / 2.0; ;

            Point ret = new Point();
            ret.X = affine[0, 0] * x+ affine[0, 1] * y + affine[0, 2] + destRect.Width/2.0;
            ret.Y = affine[1, 0] * x + affine[1, 1] * y + affine[1, 2] + destRect.Height / 2.0;
            return ret;
        }
    }
}
