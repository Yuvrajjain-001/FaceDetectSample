using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.LiveLabs
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage: [options] CommandLineDemo.exe imageFile");
            Console.WriteLine("Runs face and eye detection on imageFile and writes locations to the console");
            Console.WriteLine("-nnFile file\tUse file as the faceFeaturedetector rather than the deafult");
        }

        static void Main(string[] args)
        {
            int iArg = 0;
            Detect det = new Detect();
            string imageFile = @"G:\src\BingApi\images\happy\030408_happy.jpg";

            try
            {
                while (iArg < args.Length - 1)
                {
                    switch (args[iArg].ToLower())
                    {
                        case "-nnfile":
                            det.SetFaceFeatureFile(args[++iArg]);
                            break;

                        case "-help":
                        case "-h":
                            Usage();
                            return;

                        default:
                            Console.WriteLine("Unrecognized option {0}", args[iArg]);
                            Usage();
                            return;
                    }
                    ++iArg;
                }

                if (iArg < args.Length)
                {
                    imageFile = args[iArg];
                }

                det.DetectFile(imageFile);
                det.PrintResults();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error processing input {0}", e.Message);
            }
        }
    }
}
