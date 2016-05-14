using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.LiveLabs
{
    class Program
    {


        Detect _det = new Detect();

        string _imageBase = "";
        string _imageSrc = "";
        string _imageRes = "res";
        string _imageMask = "mask01.png";
        string _maskPath = ".";
        List<int> _basePoints = new List<int>();
        List<int> _srcPoints = new List<int>();
        List<int> _maskPoints = Detect.MakeList<int>(400, 400, 500, 400, 450, 500);
        int _thumbnailSize = 150;
        bool _dontRun = false;
        
        static void Usage()
        {
//            Console.WriteLine("Usage: [options] CommandLineDemo.exe imageFile");
//            Console.WriteLine("Runs face and eye detection on imageFile and writes locations to the console");
//            Console.WriteLine("-nnFile file\tUse file as the faceFeaturedetector rather than the deafult");
        }

        static void Main(string[] args)
        {
            int iArg = 0;
            Program prog = new Program();

            switch (args[iArg].ToLower())
            {

                case "-single":
                    prog.MatePair(args, 1);
                    return;

                case "-gallery":
                    prog.Gallery(args, 1);
                    return;
                    
                default:
                    Console.Error.WriteLine("Unrecognized option {0}", args[iArg]);
                    Usage();
                    return;
            }
        }

        public void MatePair(string[] args, int iArg)
        {
            ReadArgs(args, iArg);
            Detect.Blend(_imageBase, _basePoints, _imageSrc, _srcPoints, _imageMask, _maskPoints, _imageRes, _dontRun);
        }

        public void Gallery(string[] args, int iArg)
        {
            ReadArgs(args, iArg);
            string[] maskList = Directory.GetFiles(_maskPath, "mask*.png");

            List<string> resultImages = new List<string>();
            resultImages.Add(_imageSrc);
            resultImages.AddRange(_det.ProduceGallery(_imageBase, _basePoints, _imageSrc, _srcPoints, maskList, _maskPoints, String.Format("res_00"), _dontRun));

            resultImages.Add(_imageBase);
            resultImages.AddRange(_det.ProduceGallery(_imageSrc, _srcPoints, _imageBase, _basePoints, maskList, _maskPoints, String.Format("res_01"), _dontRun));

            Detect.CollectGallery(resultImages, _imageRes, _thumbnailSize);
        }

        public int ReadArgs(string[] args, int iArg)
        {

            List<int> coords = new List<int>();

            try
            {
                while (iArg < args.Length)
                {
                    switch (args[iArg].ToLower())
                    {
                        case "-base":
                            _imageBase = args[++iArg];
                            break;

                        case "-dontrun":
                            _dontRun = true;
                            break;

                        case "-basepts4":
                            coords.Clear();
                            for (int i = 0; i < 4; i++)
                            {
                                int val = (int)Math.Round(Convert.ToSingle(args[++iArg]));
                                coords.Add(val);
                                Console.Write("{0} ", val);
                            }
                            Console.WriteLine();
                            _basePoints = Detect.FaceAnchorPoints(coords[0], coords[1], coords[2], coords[3]);
                            break;

                        case "-basepts6":
                            coords.Clear();
                            for (int i = 0; i < 6; i++)
                            {
                                int val = (int)Math.Round(Convert.ToSingle(args[++iArg]));
                                coords.Add(val);
                                Console.Write("{0} ", val);
                            }
                            Console.WriteLine();
                            _basePoints.AddRange(coords);
                            break;

                        case "-srcpts4":
                            coords.Clear();
                            for (int i = 0; i < 4; i++)
                            {
                                int val = (int)Math.Round(Convert.ToSingle(args[++iArg]));
                                coords.Add(val);
                                Console.Write("{0} ", val);
                            }
                            Console.WriteLine();
                            _srcPoints = Detect.FaceAnchorPoints(coords[0], coords[1], coords[2], coords[3]);
                            break;

                        case "-srcpts6":
                            coords.Clear();
                            for (int i = 0; i < 6; i++)
                            {
                                int val = (int)Math.Round(Convert.ToSingle(args[++iArg]));
                                coords.Add(val);
                                Console.Write("{0} ", val);
                            }
                            Console.WriteLine();
                            _srcPoints.AddRange(coords);
                            break;

                        case "-src":
                            _imageSrc = args[++iArg];
                            break;

                        case "-thumbnailsize":
                            _thumbnailSize = Convert.ToInt32(args[++iArg]);
                            break;


                        case "-res":
                            _imageRes = args[++iArg];
                            break;

                        case "-mask":
                            _imageMask = args[++iArg];
                            break;

                        case "-maskpath":
                            _maskPath = args[++iArg];
                            break;


                        case "-nn":
                            _det.SetFaceFeatureFile(args[++iArg]);
                            break;

                        default:
                            Usage();
                            throw new Exception(String.Format("Unrecognized option |{0}|", args[iArg]));
                    }
                    ++iArg;
                }


                if (_basePoints.Count == 0)
                {
                    _basePoints = _det.FindFacePoints(_imageBase);
                }
                if (_srcPoints.Count == 0)
                {
                    _srcPoints = _det.FindFacePoints(_imageSrc);
                }

            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Error processing input {0}", e.Message));
            }
            return iArg;
        }
    }
}
