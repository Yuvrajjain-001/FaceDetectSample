using System;
using System.Collections.Generic;
using System.Text;
using LibNN;

namespace NetDistance
{
    public class PairedNet
    {
        private string _netFile;
        private Classifier _classifier;

        public PairedNet(string netFile)
        {
            _netFile = netFile;
            _classifier = new Classifier();
            _classifier.AlgorithmFileName = _netFile;

        }

        public double PairComparator(double[] data1, double[] data2)
        {
            if (null != data2 || data1.Length != _classifier.InputCount)
            {
                throw new Exception("PairNet.PairComparator: Expected input length " + _classifier.InputCount.ToString() + " found " +
                                        data1.Length.ToString());
            }

            float[] allData = new float[data1.Length];

            for (int i1 = 0; i1 < data1.Length; ++i1)
            {
                allData[i1] = (float)data1[i1];
            }


            _classifier.Classify(allData);

            return _classifier.Result[1];
        }
        public double Distance(double[] data1, double[] data2)
        {
            if (data1.Length != data2.Length)
            {
                throw new Exception("PairedNet.Distance data1 and data2 dimensions do not match");
            }

            int combinedLen = data1.Length + data2.Length;
            if (combinedLen != _classifier.InputCount)
            {
                throw new Exception("PairedNet.Distance: Expected input length " + _classifier.InputCount.ToString() + " found " +
                                        combinedLen.ToString());
            }
            float [] allData = new float[combinedLen];

            int i1 = 0;
            for (; i1 < data1.Length; ++i1)
            {
                allData[i1] = (float)data1[i1];
            }

            for (int i2 = 0; i2 < data2.Length; ++i2, ++i1)
            {
                allData[i1] = (float)data2[i2];
            }


            _classifier.Classify(allData);


            float result = _classifier.Result[0];
            result = 1.0F - result;
            float result1 = _classifier.Result[0];
            result1 = 1.0F - result1;
            return (result + result1) / 2.0;

            //float klDist1 = KLDistance(_classifier.Result);
            //float[] allDataReverse = new float[combinedLen];
            //Array.Copy(allData, data2.Length, allDataReverse, 0, data1.Length);
            //Array.Copy(allData, 0, allDataReverse, data1.Length, data1.Length);
            //_classifier.Classify(allDataReverse);
            //float[] result1 = _classifier.Result;
            //float klDist2 = KLDistance(result1);


            //return (double)(klDist1 + klDist2) / 2.0;
        }

        private float KLDistance(float[] data)
        {
            if (data.Length / 2 * 2 != data.Length)
            {
                throw new Exception("PairedNet KLDistance data length must be even");
            }

            int len2 = data.Length / 2;
            float[] target = new float[len2];
            float[] output = new float[len2];

            Array.Copy(data, 0, output, 0, len2);
            Array.Copy(data, len2, target, 0, len2);
            float klDist = CrossEntropy(target, output);

            Array.Copy(data, 0, target, 0, len2);
            Array.Copy(data, len2, output, 0, len2);
            klDist += CrossEntropy(target, output);

            return klDist;
        }

        private float CrossEntropy(float [] target, float [] output)
        {
            if (target.Length != output.Length)
            {
                throw new Exception("PairedNet KLDistance targets and output length must be equal");
            }

            float ret = 0.0F;

            for (int i = 0; i < target.Length; ++i)
            {
                if (output[i] <= 0.0F)
                {
                    output[i] = 1.0e-05F;
                }
                if (target[i] <= 0.0F)
                {
                    target[i] = 1.0e-05F;
                }

                ret -= target[i] * (float)Math.Log(output[i] / target[i]);

            }

            return ret;
        }
    }
}
