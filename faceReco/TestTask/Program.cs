using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using ShoNS.Array;
using System.Reflection;

namespace TestTask
{
    class Program
    {
        public enum TestAction { Base, Filtered, GenerateTrainPairs, GenerateTrainTriplets, TestPairs, GenerateDepth, ReportTrainData };

        static void Main(string[] args)
        {
            int iArg = 0;
            int verbose = 0;
            StreamWriter sw = null;
            List<int> kVals = new List<int>();
            int kMax = 5;
            List<double> rejectThresh = new List<double>();
            List<TestCollection.DistanceComputeDelegate> distanceComputer = new List<TestCollection.DistanceComputeDelegate>();
            bool dataFilesFound = false;
            bool doTrainLabel = true;
            bool doDistances = true;
            string optionalDllString = null;
            string dllLoadPath = null;
            string dataFileName = null;
            string dllMethodName = "Distance";

            TestAction action = TestAction.Base;
            int iDistanceCompute = 0;

            if (args.Length < 2)
            {
                Usage();
                return;
            }

            while (dataFilesFound == false && iArg < args.Length - 2)
            {
                switch (args[iArg].ToLower())
                {
                    case "-k":
                        ++iArg;
                        kVals.Add(Convert.ToInt32(args[iArg]));
                        break;

                    case "-verbose":
                        ++iArg;
                        verbose = Convert.ToInt32(args[iArg]);
                        break;

                    case "-algo":
                        ++iArg;
                        distanceComputer.Add(ParseAlgo(args[iArg]));
                        break;

                    case "-dllalgo":
                        ++iArg;
                        dllLoadPath= args[iArg];
                        break;

                    case "-dllinfo":
                        optionalDllString = args[++iArg];
                        break;

                    case "-out":
                        ++iArg;
                        FileStream fs = new FileStream(args[iArg], FileMode.Create);
                        sw = new StreamWriter(fs);
                        break;
                
                    case "-filter":
                        action = TestAction.Filtered;
                        break;

                    case "-rejectthresh":
                        ++iArg;
                        rejectThresh.Add(Convert.ToDouble(args[iArg]));
                        break;

                    case "-gentraintriplets":
                        action = TestAction.GenerateTrainTriplets;
                        //doTrainLabel = false;
                        doDistances = false;
                        break;

                    case "-gentrainpairs":
                        action = TestAction.GenerateTrainPairs;
                        doTrainLabel = false;
                        doDistances = false;
                        break;

                    case "-generatedepth":
                        action = TestAction.GenerateDepth;
                        doTrainLabel = false;
                        doDistances = false;
                        break;

                    case "-testpairs":
                        action = TestAction.TestPairs;
                        break;

                    case "-reporttrain":
                        action = TestAction.ReportTrainData;
                        dataFileName = args[++iArg];
                        doTrainLabel = false;
                        doDistances = false;
                        break;

                    case "-dllmethod":
                        ++iArg;
                        dllMethodName = args[iArg];
                        break;

                    case "-datafiles":
                        dataFilesFound = true;
                        break;

                }
                ++iArg;
            }

            if (null != dllLoadPath)
            {
                TestCollection.DistanceComputeDelegate distanceDelegate = LoadDistanceAlgoFromDll(dllLoadPath, dllMethodName, optionalDllString);
                if (null != distanceDelegate)
                {
                    distanceComputer.Add(distanceDelegate);
                }
                else
                {
                    return;
                }
            }

            if (0 == distanceComputer.Count || distanceComputer[0] == null)
            {
                return;
            }

            if (kVals.Count == 0)
            {
                kVals.Add(5);
            }
            if (rejectThresh.Count == 0)
            {
                rejectThresh.Add(0);
            }

            List<TestCollection> testUnLabels = new List<TestCollection>();
            TestCollection testLabel = null;

            while (iArg < args.Length - 1)
            {
                string fileName = args[iArg++];

                if (true == doTrainLabel)
                {
                    testLabel = new TestCollection(fileName);
                    if (testLabel.Faces.Count <= 0)
                    {
                        Console.WriteLine("No faces loaded from {0}", fileName);
                        return;
                    }
                }

                fileName = args[iArg++];
                TestCollection testUnLabel = new TestCollection(fileName);
                if (testUnLabel.Faces.Count <= 0)
                {
                    Console.WriteLine("No faces loaded from {0}", fileName);
                    return;
                }

                //testUnLabel.distanceComputer = EuclidianDistance;
                testUnLabel.DistanceComputer = distanceComputer[iDistanceCompute];
                if (true == doDistances)
                if (action == TestAction.Base || action == TestAction.Filtered || action == TestAction.TestPairs)
                {
                    testUnLabel.DoDistances(testLabel);
                    testUnLabel.Sort();
                }

                testUnLabels.Add(testUnLabel);
            }

            ++iDistanceCompute;

            if (null != sw)
            {
                Console.SetOut(sw);
            }

            if (testUnLabels.Count > 0)
            {
                switch (action)
                {
                    case TestAction.Base:
                        DoReport(kVals, rejectThresh, testUnLabels, verbose);
                        break;

                    case TestAction.Filtered:
                        RunFiltered(kMax, kVals, testUnLabels[0], distanceComputer[iDistanceCompute], verbose);
                        DoReport(kVals, rejectThresh, testUnLabels, verbose);
                        break;

                    case TestAction.TestPairs:
                        testUnLabels[0].DistanceComputer = distanceComputer[iDistanceCompute];
                        TestPairs(testUnLabels[0], -1, kVals);
                        break;

                    case TestAction.GenerateTrainTriplets:
                        GenerateTrainDataTriplets(kVals, rejectThresh, testLabel, testUnLabels[0], verbose);
                        break;

                    case TestAction.GenerateDepth:
                        ReportDepth(kVals, testUnLabels[0], verbose);
                        break;

                    case TestAction.GenerateTrainPairs:
                        if (kVals.Count < 2)
                        {
                            throw new Exception("GenerateTrainPairs need to specify kVal for both diff and same classes");
                        }
                        GenerateTrainDataPairs(kVals[0], kVals[1] , testUnLabels[0], verbose);
                        break;

                    case TestAction.ReportTrainData:
                        ReportTrainData(kVals, testUnLabels[0], dataFileName);
                        break;
                }

            }

            if (null != sw)
            {
                sw.Close();
            }

        }

        static void Usage()
        {
            Console.WriteLine("<options> testLabel_1 testUnlabel_1 {testLabel_2 ttestUnlabel_2}");
            Console.WriteLine("-k - K for K NN");
            Console.WriteLine("-out - Set OutputFile");
            Console.WriteLine("-verbose <int> - Larger values indicate more output");
            Console.WriteLine("\t0 Index TrueCategory PredictCategory  (default)");
            Console.WriteLine("\t1 [FaceId FaceClass] [NearestClass Count] [SecondNearestClass Count] ...");
            Console.WriteLine("\t2 [FaceId FaceClass] [NearestLabelSampId Class Distance] [SecondNearest ...]");
            Console.WriteLine("-algo - Set Distance measure (corr l1 l2)");
            Console.WriteLine("-filter - First filter using the first supplied algo");
            Console.WriteLine("-genTrainTriplets - Generate triple training data");
            Console.WriteLine("-genTrainPairs - Generate training pair data");
            Console.WriteLine("-generateDepth - Report Depth need to go to find top k correct answers");
            Console.WriteLine("-reportTrain <fileName> - Report training data plus distance ratios");
            Console.WriteLine("-testPairs - Test pairs");
            Console.WriteLine("-rejectthresh - Set fractional reject threshold (default 0 - no Reject)");
            Console.WriteLine("-dll path - Specify a dll to implement the distance metric.");
            Console.WriteLine("         The dll must implemen: double Distance(double [] v, double [] v2)");
            Console.WriteLine("         If the method is no-static the class must have a constructor Class(string name)");
            Console.WriteLine("-dllInfo string - The string pased to the constructor (se -dll option");
            Console.WriteLine("-out filename - Specify an output File");
            Console.WriteLine("-dataFiles Indicates end of option list and the remaining arguments are {testLabel testUnlabel} pairs");
            Console.WriteLine("           Can omit if only a single pairis used");
        }

        static void DoReport(List<int> kVals, List<double> rejectThresh, List<TestCollection> testUnLabels, int verbose)
        {
            foreach (int k in kVals)
            {
                foreach (double reject in rejectThresh)
                {
                    if (testUnLabels.Count > 1)
                    {
                        ReportResultComb(k, reject, testUnLabels, verbose);
                    }
                    else
                    {
                        ReportResult(k, reject, testUnLabels[0], verbose);
                        //ReportResultOnline(k, reject, testLabel, testUnLabels[0], verbose);
                    }
                }
            }
        }
        static public TestCollection.DistanceComputeDelegate ParseAlgo(string algo)
        {
            TestCollection.DistanceComputeDelegate ret = Correlation;

            switch (algo.ToLower())
            {
                case "corr":
                    ret = Correlation;
                    break;

                case "l1":
                    ret = L1Distance;
                    break;

                case "l2":
                    ret = L2Distance;
                    break;
                case "lde":
                    //LDEDistance ldeDist = new LDEDistance("Projection.bin", 100);
                    //ret = ldeDist.Distance;
                    break;
                default:
                    Console.WriteLine("Unrecognized algorithm {0}", algo);
                    ret = null;
                    break;
            }

            return ret;
        }

        static public TestCollection.DistanceComputeDelegate LoadDistanceAlgoFromDll(string dllPath, string methodName, string optionalFileName)
        {
            TestCollection.DistanceComputeDelegate distDelegate = null;
            Assembly dll = null;

            try
            {
                dll = Assembly.LoadFrom(dllPath);
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot load dll {0}", dllPath);
                return null;
            }

            Type[] args = new Type[2];

            int iArg = 0;

            args[iArg++] = typeof(double[]);
            args[iArg++] = typeof(double[]);

            //BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

            foreach (Type type in dll.GetTypes())
            {
                MethodInfo methodInfo = type.GetMethod(methodName, args);

                if (null != methodInfo)
                {
                    Object[] parms = new object[1];
                    parms[0] = optionalFileName;
                    Object obj = null;
                    try
                    {
                        if (false == methodInfo.IsStatic)
                        {
                            obj = Activator.CreateInstance(type, parms);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to create an object instance from {0} ", dllPath);
                        if (null != optionalFileName)
                        {
                            Console.WriteLine("with  parameter {0}", optionalFileName);
                        }
                        Console.WriteLine("{0}", e.Message);
                        return null;
                    }
                    try
                    {
                        distDelegate = Delegate.CreateDelegate(typeof(TestCollection.DistanceComputeDelegate), 
                                                        obj, methodInfo, true) as TestCollection.DistanceComputeDelegate;
                        Console.WriteLine("Found {0} in {1}", methodName, dllPath);
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to bind a delegate to {0}.{1} because of {2}",
                            type.FullName, methodName, e.Message);
                    }
                }
            }

            if (null == distDelegate)
            {
                Console.WriteLine("Cannot find method {0} in {1}", methodName, dllPath);
            }
            return distDelegate;
        }

        static void RunFiltered(int kMax, List<int> kVals, TestCollection testUnLabel, TestCollection.DistanceComputeDelegate distanceComputer, int verbose)
        {
            List<Face> otherFaces = new List<Face>();

            foreach (Face face in testUnLabel.Faces)
            {
                otherFaces.Clear();
                kMax = Math.Min(kMax, face.Distances.Length);

                for (int k = 0 ; k < kMax ; ++k)
                {
                    if (face.ID != face.Distances[k].face.ID)
                    {
                        otherFaces.Add(face.Distances[k].face);
                    }
                    else
                    {
                        int tmp = 0;
                        ++tmp;
                    }
                }

                face.DoDistances(otherFaces, distanceComputer);

                face.Sort();

            }
        }
        static void ReportResult(int kVal, double rejectThresh, TestCollection testUnLabel, int verbose)
        {
            int winCount = (int)Math.Round(Math.Max(1.0, kVal*rejectThresh));
            foreach (Face face in testUnLabel.Faces)
            {
                ReportFaceResult(face, winCount, verbose, kVal);
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Does not cache distances for each face but computes them on the fly
        /// Used for large testLabel collections
        /// </summary>
        /// <param name="kVal">Number of neighbours to report</param>
        /// <param name="rejectThresh"></param>
        /// <param name="testUnLabel"></param>
        /// <param name="verbose"></param>
        static void ReportResultOnline(int kVal, double rejectThresh, TestCollection testLabel, TestCollection testUnLabel, int verbose)
        {
            int winCount = (int)Math.Round(Math.Max(1.0, kVal * rejectThresh));

            foreach (Face face in testUnLabel.Faces)
            {
                face.DoDistances(testLabel.Faces, testUnLabel.DistanceComputer);
                face.Sort();
                ReportFaceResult(face, winCount, verbose, kVal);
                face.Dispose();
            }
            Console.WriteLine();
        }

        static void ReportFaceResult(Face face, int winCount, int verbose, int kVal)
        {

            Dictionary<int, NeighbourCount> neigh = face.ClosestNeighbours(kVal);
            NeighbourCount[] neighCount = face.CountNeighbours(neigh);
            int pred = (neighCount[0].count >= winCount) ? neighCount[0].id : 0;
            int verboseThresh = 1;

            if (verbose < verboseThresh++)
            {
                Console.Write("{0} {1} {2}", face.ID, face.GroupId, pred);
            }
            else if (verbose < verboseThresh++)
            {
                Console.Write("[ {0} {1} {2} ]: ", face.GroupId, face.ID, pred);
                foreach (NeighbourCount n in neighCount)
                {
                    Console.Write("[ {0} {1} ] ", n.id, n.count);
                }
            }
            else
            {
                Console.Write("[ {0} {1} {2} ]: ", face.GroupId, face.ID, pred);
                int kMax = Math.Min(kVal, face.Distances.Length);
                for (int k = 0; k < face.Distances.Length; ++k)
                {
                    DistanceMeasure d = face.Distances[k];
                    if (k < kMax || d.face.GroupId == face.GroupId)
                    {
                        Console.Write("[ {0} {1} {2:F4} ] ", d.face.ID, d.face.GroupId, d.Distance);
                    }
                }
            }
            Console.WriteLine();
        }

        static void ReportResultComb(int kVal, double rejectThresh, List<TestCollection> testUnLabels, int verbose)
        {
            int winCount = (int)Math.Round(Math.Max(1.0, kVal * rejectThresh));
            Dictionary<int, NeighbourCount> accum = new Dictionary<int, NeighbourCount>();

            for (int iFace = 0; iFace < testUnLabels[0].Faces.Count; ++iFace)
            {
                accum.Clear();
                Face face = null;
                int idBest = -1;
                int agree = 1;

                foreach (TestCollection testUnlabel in testUnLabels)
                {
                    face = testUnlabel.Faces[iFace];
                    Dictionary<int, NeighbourCount> neigh = face.ClosestNeighbours(kVal);
                    int iid = face.Distances[0].face.GroupId;
                    if (idBest == -1)
                    {
                        idBest = iid;
                    }

                    if (iid != idBest)
                    {
                        agree = 0;
                    }

                    foreach (KeyValuePair<int, NeighbourCount> keyVal in neigh)
                    {
                        if (true == accum.ContainsKey(keyVal.Key))
                        {
                            NeighbourCount n = accum[keyVal.Key];
                            n.count += keyVal.Value.count;
                            n.distance += keyVal.Value.distance;
                            accum[keyVal.Key] = n;
                        }
                        else
                        {
                            NeighbourCount n = new NeighbourCount();
                            n.count = keyVal.Value.count;
                            n.distance = keyVal.Value.distance;
                            accum.Add(keyVal.Key, n);
                        }
                    }
                }


                NeighbourCount[] neighCount = face.CountNeighbours(accum);
                int pred = (neighCount[0].count >= winCount || agree == 1) ? neighCount[0].id : 0;
                int verboseThresh = 1;

                if (verbose < verboseThresh++)
                {
                    Console.WriteLine("{0} {1} {2}", face.ID, face.GroupId, pred);
                }
                else 
                {
                    Console.Write("{0} {1} {2} {3} ", face.ID, face.GroupId, pred, agree);
                    foreach (NeighbourCount n in neighCount)
                    {
                        Console.Write("[ {0} {1} ] ", n.id, n.count);
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine();
        }


        static double L2Distance(double[] data1, double[] data2)
        {
            if (data1.Length != data2.Length)
            {
                throw new Exception("EuclidianDistance Expect equal distances ");
            }

            double sum = 0;
            for (int i = 0; i < data1.Length; ++i)
            {
                double d = data1[i] - data2[i];
                sum += d * d;
            }
            return sum;
        }

        static double L1Distance(double[] data1, double[] data2)
        {
            if (data1.Length != data2.Length)
            {
                throw new Exception("EuclidianDistance Expect equal distances ");
            }

            double sum = 0;
            for (int i = 0; i < data1.Length; ++i)
            {
                double d = Math.Abs(data1[i] - data2[i]);
                sum += d;
            }
            return sum;
        }

        static double DotProduct(double[] data1, double[] data2)
        {
            double ret = 0.0;

            if (data1.Length != data2.Length)
            {
                return ret;
            }

            for (int i = 0; i < data1.Length; ++i)
            {
                ret += (data1[i] * data2[i]);
            }
            return ret;
        }

        static double Correlation(double[] data1, double[] data2)
        {
            double ret = DotProduct(data1, data2);

            ret /= System.Math.Sqrt(DotProduct(data1, data1));
            ret /= System.Math.Sqrt(DotProduct(data2, data2));

            return 1.0 - ret;
        }

        /// <summary>
        /// Top level to generate nearest samples in the set
        /// </summary>
        /// <param name="kVal"></param>
        /// <param name="kValSame"></param>
        /// <param name="testUnLabel"></param>
        /// <param name="verbose"></param>
        static void GenerateTrainDataPairs(int kVal, int kValSame, TestCollection testUnLabel, int verbose)
        {
            foreach (Face face in testUnLabel.Faces)
            {
                face.DoDistances(testUnLabel.Faces, testUnLabel.DistanceComputer);
                face.Sort();
                GenerateTrainPairs(face, -1, kVal, kValSame);
                face.Dispose();
            }

        }


        /// <summary>
        /// Generate nearest samples to this face. The idea is to pick
        /// closets kVal of different classes and kValSame sample of the same class
        /// </summary>
        /// <param name="face">Find best matches to this sample</param>
        /// <param name="kMax">How many closest neighbours to consider. If Negative consider all samples</param>
        /// <param name="kVal">How many of each other class to report</param>
        /// <param name="kValSame">Ho many of the same class to report</param>
        static void GenerateTrainPairs(Face face, int kMax, int kVal, int kValSame)
        {
            if (kMax <= 0)
            {
                kMax = face.Distances.Length;
            }
            else
            {
                kMax = Math.Min(kMax, face.Distances.Length);
            }

            // Find index of closest me class
            int myId = face.ID;
            int myGroup = face.GroupId;

            Dictionary<int, int> classesSeen = new Dictionary<int, int>();
            for (int k = 0; k < kMax; ++k)
            {
                DistanceMeasure d = face.Distances[k];
                Face otherFace = d.face;
                int kThresh = kVal;

                if (myId == otherFace.ID)
                {
                    continue;
                }

                if (otherFace.GroupId == myGroup)
                {
                    kThresh = kValSame;
                }

                if (false == classesSeen.ContainsKey(otherFace.GroupId))
                {
                    classesSeen.Add(otherFace.GroupId, 0);
                }
                else
                {
                    classesSeen[otherFace.GroupId]++;
                }

                if (classesSeen[otherFace.GroupId] < kThresh)
                {
                    Console.Write("{0} ", otherFace.ID);
                }
            }

            Console.WriteLine();
        }
        /// <summary>
        /// Train triple samples used by comparator
        /// </summary>
        /// <param name="kVals"></param>
        /// <param name="rejectThresh"></param>
        /// <param name="testUnLabel"></param>
        /// <param name="verbose"></param>
        static void GenerateTrainDataTriplets(List<int> kVals, List<double> rejectThresh, TestCollection testLabel, TestCollection testUnLabel, int verbose)
        {
            foreach (Face face in testUnLabel.Faces)
            {
                face.DoDistances(testLabel.Faces, testUnLabel.DistanceComputer);
                face.Sort();
                GenerateTrainTriplets(face, -1, kVals[0], kVals[1]);
                face.Dispose();
            }
        }

        /// <summary>
        /// Generate all valid training pairs for this face. The ide is to scan the sorted list of 
        /// neighbours and pick up to kVal of each class (so end up with kVAl *cClass) samples
        /// </summary>
        /// <param name="face">Find best matches to this sample</param>
        /// <param name="kMax">How many closest neighbours to consider. If Negative consider all samples</param>
        /// <param name="kVal">How many of each other class to report</param>
        static void GenerateTrainTriplets(Face face, int kMax, int myKval, int kVal)
        {
            if (kMax <= 0)
            {
                kMax = face.Distances.Length;
            }
            else
            {
                kMax = Math.Min(kMax, face.Distances.Length);
            }

            // Find index of closest me class
            int myId = face.ID;
            int myGroup = face.GroupId;
            //int myBestMatch = -1;
            List<int> myBestMatch = new List<int>();

            for (int k = 0; k < 100 || myBestMatch.Count <= 0; ++k)
            {
                DistanceMeasure d = face.Distances[k];

                if (d.face.ID != myId && d.face.GroupId == myGroup && d.Distance > 0.0)
                {
                    myBestMatch.Add(d.face.ID);
                    if (myBestMatch.Count >= myKval)
                    {
                        break;
                    }
                }
            }

            if (myBestMatch.Count <= 0)
            {
                throw new Exception("Did not find a best my match for face " + face.ID.ToString());
            }

            Dictionary<int, int> classesSeen = new Dictionary<int, int>();
            for (int k = 0; k < kMax; ++k)
            {
                DistanceMeasure d = face.Distances[k];
                Face otherFace = d.face;
                if (otherFace.GroupId != myGroup)
                {
                    if (false == classesSeen.ContainsKey(otherFace.GroupId))
                    {
                        classesSeen.Add(otherFace.GroupId, 0);
                    }
                    else
                    {
                        classesSeen[otherFace.GroupId]++;
                    }

                    if (classesSeen[otherFace.GroupId] < kVal)
                    {
                        foreach (int myVal in myBestMatch)
                        {
                            Console.Write("{0} {1} ", myVal+565, otherFace.ID+565);
                        }
                    }
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testLabel"></param>
        /// <param name="testUnlabel"></param>
        static void TestPairs(TestCollection testUnlabel, int kMax, List<int> kVals)
        {
            foreach (int kVal in kVals)
            {
                foreach (Face face in testUnlabel.Faces)
                {
                    TestFaceUsingSort(face, kMax, kVal, testUnlabel.DistanceComputer);
                }
            }

        }

        static void TestFaceUsingSort(Face face, int kMax, int kVal, TestCollection.DistanceComputeDelegate distanceComputer)
        {
            if (kMax <= 0)
            {
                kMax = face.Distances.Length;
            }
            else
            {
                kMax = Math.Min(kMax, face.Distances.Length);
            }

            Dictionary<int, int> classesSeen = new Dictionary<int, int>();
            Face[] faceArray = new Face[kMax];
            int iFace = 0;
            for (int k = 0; k < kMax; ++k)
            {
                DistanceMeasure d = face.Distances[k];
                Face otherFace = d.face;

                if (otherFace.ID == face.ID)
                {
                    continue;
                }

                if (false == classesSeen.ContainsKey(otherFace.GroupId))
                {
                    classesSeen.Add(otherFace.GroupId, 0);
                }
                else
                {
                    classesSeen[otherFace.GroupId]++;
                }

                if (classesSeen[otherFace.GroupId] < kVal)
                {
                    faceArray[iFace++] = otherFace;
                }
            }

            Array.Resize(ref faceArray, iFace);

            FacePairSorter facePairSorter = new FacePairSorter(distanceComputer);
            facePairSorter.RefFace = face;

            Array.Sort(faceArray, facePairSorter);
            Console.Write("[ {0} {1} {2} ]: ", face.GroupId, face.ID, faceArray[0].GroupId);
            Console.Write("{0}", faceArray[0].ID);
            Console.WriteLine();
        }

        /// <summary>
        /// Report the depth at which the k correct templates are found
        /// </summary>
        /// <param name="kVals"></param>
        /// <param name="rejectThresh"></param>
        /// <param name="testUnLabel"></param>
        /// <param name="verbose"></param>
        static void ReportDepth(List<int> kVals, TestCollection testUnLabel, int verbose)
        {
            foreach (int k in kVals)
            {
                foreach (Face face in testUnLabel.Faces)
                {
                    face.DoDistances(testUnLabel.Faces, testUnLabel.DistanceComputer);
                    face.Sort();
                    ReportDepth(face, -1, k);
                    face.Dispose();
                }

            }
        }

        static void ReportDepth(Face face, int kMax, int kVal)
        {
            if (kMax <= 0)
            {
                kMax = face.Distances.Length;
            }
            else
            {
                kMax = Math.Min(kMax, face.Distances.Length);
            }

            // Find index of closest me class
            int myId = face.ID;
            int myGroup = face.GroupId;
            int seenCount = 0;

            Console.Write("{0} {1}", myId, myGroup);
            for (int k = 0; k < kMax; ++k)
            {
                DistanceMeasure d = face.Distances[k];

                if (d.face.ID != myId && d.face.GroupId == myGroup)
                {
                    ++seenCount;
                    Console.Write(" {0}", k);
                    if (seenCount >= kVal)
                    {
                        break;
                    }
                }
            }

            Console.WriteLine();
        }


        /// <summary>
        /// Report the depth at which the k correct templates are found
        /// </summary>
        /// <param name="kVals"></param>
        /// <param name="rejectThresh"></param>
        /// <param name="testUnLabel"></param>
        /// <param name="verbose"></param>
        static void ReportTrainData(List<int> kVals, TestCollection testUnLabel, string dataFile)
        {
            using (Microsoft.LiveLabs.TrainDataFileWriter write = new Microsoft.LiveLabs.TrainDataFileWriter(dataFile))
            {
                foreach (int k in kVals)
                {
                    int iFace = 0;
                    foreach (Face face in testUnLabel.Faces)
                    {
                        face.DoDistances(testUnLabel.Faces, testUnLabel.DistanceComputer);
                        face.Sort();
                        ReportTrainData(face, -1, iFace, write);
                        face.Dispose();
                        ++iFace;
                    }

                }

                write.Dispose();
            }
        }

        static void ReportTrainData(Face face, int kMax, int iFace, Microsoft.LiveLabs.TrainDataFileWriter write)
        {
            if (kMax <= 0)
            {
                kMax = face.Distances.Length;
            }
            else
            {
                kMax = Math.Min(kMax, face.Distances.Length);
            }

            // Find index of closest me class
            int myId = face.ID;
            int myGroup = face.GroupId;
            int myBestMatch = -1;
            int otherBest = -1;
            int iFound = 0;

            for (int k = 0; k < kMax && iFound < 2; ++k)
            {
                DistanceMeasure d = face.Distances[k];

                if (d.face.GroupId == myGroup)
                {
                    if (d.face.ID != myId && myBestMatch < 0 && d.Distance > 1.0)
                    {
                        myBestMatch = k;
                        ++iFound;
                    }
                }
                else if (otherBest < 0)
                {
                    otherBest = k;
                    ++iFound;
                }
            }

            if (myBestMatch < 0)
            {
                throw new Exception("Did not find a best my match for face " + face.ID.ToString());
            }

            double[] extraData = new double[3];

            if (iFace % 2 == 0)
            {
                extraData[0] = L1Distance(face.Data, face.Distances[myBestMatch].face.Data);
                extraData[1] = L1Distance(face.Data, face.Distances[otherBest].face.Data);
                extraData[0] = Math.Max(1.0, extraData[0]);
                extraData[2] = (extraData[0] - extraData[1]) / extraData[0];
            }
            else
            {
                extraData[0] = L1Distance(face.Data, face.Distances[otherBest].face.Data);
                extraData[1] = L1Distance(face.Data, face.Distances[myBestMatch].face.Data);
                extraData[1] = Math.Max(1.0, extraData[1]);
                extraData[2] = (extraData[1] - extraData[0]) / extraData[1];
            }
            extraData[2] = Math.Max(-3.0, extraData[2]);
            extraData[2] = Math.Min(3.0, extraData[2]);
            extraData[2] *= 10000;

            write.WriteNewInput(face.Data);
            write.WriteDataContinue(extraData);
            double[] target = new double[1];
            target[0] = myGroup;
            write.WriteNewTarget(target);
        }

    }

}
