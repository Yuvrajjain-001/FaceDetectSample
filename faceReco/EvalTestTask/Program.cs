using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EvalTestTask
{
    class Program
    {
        static void Main(string[] args)
        {
            int iArg = 0;
            int verbose = 0;

            if (args.Length < 1)
            {
                Usage();
                return;
            }

            while (iArg < args.Length - 1)
            {
                switch (args[iArg])
                {
                    case "-out":
                        ++iArg;
                        FileStream fs = new FileStream(args[iArg], FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        Console.SetOut(sw);
                        break;

                    case "-verbose":
                        ++iArg;
                        verbose = Convert.ToInt32(args[iArg]);
                        break;

                }
                ++iArg;
            }

            string filename = args[iArg];


            TestEval testEval = new TestEval(filename);


            StreamReader sr = null;
            while (true)
            {
                sr = testEval.ReadData(filename, sr);
                if (null == sr)
                {
                    break;
                }
                testEval.Report(verbose);
            }

        }

        static private void Usage()
        {
            Console.WriteLine("<options> resultFile");
            Console.WriteLine("-out output file (default is console");
            Console.WriteLine("-verbose  Set the level of verbosity (0 default)");
            Console.WriteLine("Deafult output: SampleCount CorrectCount ErrorRate RejectCount RejectRate ClassAvg Stdev Min Max");
        }

    }

    public class TestEval
    {
        Dictionary<int, ResultAccumulator> _classes;
        ResultAccumulator _summary;
        ResultAccumulator _outReject;
        int _summaryClass = -1;
        int _outRejectClass = -2;
        int _reject = 0;

        FileInfo fileInfo;

        public TestEval(string filename)
        {
            fileInfo = new FileInfo(filename);
            if (false == fileInfo.Exists)
            {
                throw new Exception("Cannot find file " + filename);
            }

        }
        public StreamReader ReadData(string filename, StreamReader sr)
        {

            _classes = new Dictionary<int, ResultAccumulator>();
            _summary = new ResultAccumulator(_summaryClass);
            _outReject = new ResultAccumulator(_outRejectClass);

            if (null == sr)
            {
                sr = File.OpenText(filename);
            }

            string line;
            while (true)
            {
                line = sr.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                int iField = 0; ;
                string[] fields = line.Split();

                int id = Convert.ToInt32(fields[iField++]);
                int target = Convert.ToInt32(fields[iField++]);
                int output = Convert.ToInt32(fields[iField++]);
                ResultAccumulator res;

                if (_classes.ContainsKey(target))
                {
                    res = _classes[target];
                }
                else
                {
                    res = new ResultAccumulator(target);
                    _classes.Add(target, res);
                }

                if (_reject == output)
                {
                    _outReject.AddBinaryResult(target, output);
                }

                res.AddBinaryResult(target, output);
                _summary.AddBinaryResult(target, output);
            }

            if (_summary.Total <= 0)
            {
                sr.Close();
                sr = null;
            }
            return sr;
        }

        public void Report(int verbose)
        {

            ResultAccumulator stats = new ResultAccumulator(0);
            ResultAccumulator statsRej = new ResultAccumulator(0);

            foreach (KeyValuePair<int, ResultAccumulator> kv in _classes)
            {
                if (verbose > 1)
                {
                    Console.WriteLine("{0} {1} {2:F3}", kv.Value.ID, kv.Value.Total, kv.Value.ErrorRate);
                }
                stats.AddRealResult(kv.Value.ErrorRate);
                statsRej.AddRealResult(kv.Value.Reject);
            }

            Console.Write("{0} {1} {2:F3} ", _summary.Total, _summary.Correct, _summary.ErrorRate);
            Console.Write("{0} {1:F3} ", _outReject.Total, statsRej.Average);
            Console.WriteLine("{0:F3} {1:F3} {2:F3} {3:F3} ", stats.Average, stats.StdDev, stats.Min, stats.Max);

        }
    }
}
