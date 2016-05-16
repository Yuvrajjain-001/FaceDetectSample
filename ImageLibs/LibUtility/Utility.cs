// Utility.cs
//
// cscargs:  /unsafe /target:library

using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Dpu 
{
    namespace Utility 
    {
        using Real = System.Single;

        public class ListUtil
        {
            public static object[] Create(params object[] list)
            {
                return list;
            }
        }

        /// <summary>
        /// Exception to throw when code tests fail (so as to
        /// be potentially distinguished from other runtime exceptions)
        /// </summary>
        public class TestException : Exception
        {
            public TestException(string msg) : base(msg) {}
            public TestException() : base() {}
        }

        public class EpsilonTest
        {
            /// <summary>
            /// Epsilon to compare double value.
            /// </summary>
            public static double Epsilon = 1e-6;
            public static float FloatEpsilon = 1e-6f;
            public static Real RealEpsilon = FloatEpsilon;

            public static bool IsWithinEpsilon(double value1, double value2)
            {
                return Math.Abs(value1 - value2) < Epsilon;
            }
        }

//#if INTERNAL_DPU
        /// <summary>
        /// Generate unique id's
        /// </summary>
        public class SharedIdGenerator
        {
            public static SharedIdGenerator Gen = new SharedIdGenerator();

            private int _id = 0;
            public SharedIdGenerator() {}
            public int NextId() { return _id++; }
            public void Reset() { _id = 0; }
        }


		/// <summary>
		/// Useful for timing functions on the order of seconds or minutes.
		/// </summary>
		public class ExecTimer
		{
			public ExecTimer()
			{
				Start();
			}

			public void Start()
			{
				startTime = Environment.TickCount;
			}

			public ExecTimer Stop()
			{
				stopTime = Environment.TickCount;
				return this;
			}

			public override string ToString()
			{
				int s = (int)Seconds;
				int m = s / 60; s %= 60;
				int h = m / 60; m %= 60;
				StringBuilder sb = new StringBuilder();
				if(h > 0) sb.Append(h + "h");
				if(h > 0 || m > 0) sb.Append(m + "m");
				sb.Append(s + "s");
				return sb.ToString();
			}

			public double Seconds { get { return (stopTime-startTime)/1000.0; } }

			int startTime;
			int stopTime;
		}
//#endif // INTERNAL_DPU

        /// <summary>
        /// Used to minimize the number of random number generators needed in code.
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(false)]
        public sealed class SharedRandom 
        {
            private SharedRandom () {}

            private static Random _rand = new Random((int) System.DateTime.Now.Ticks);

            public static void Init(int seed)
            {
                _rand = new Random(seed);
            }

            public static Random Generator 
            {
                get { return _rand; }
            }


            static double _normCached = double.MinValue;

            /// <summary>
            /// Normal with mean 0 and stdev 1.0 (see Numerical Recipes)
            /// </summary>
            /// <returns></returns>
            public static double NextNormal() 
            {
                double r1;
                double r2;
                double len;

                if (_normCached != double.MinValue) // if cached
                {
                    r1 = _normCached;
                    _normCached = double.MinValue;
                    return r1;
                }
                else 
                {
                    do 
                    { 
                        r1 = (2.0 * Generator.NextDouble()) - 1.0;
                        r2 = (2.0 * Generator.NextDouble()) - 1.0;
                        len = r1*r1 + r2*r2;
                    } while (len >= 1.0 || len == 0.0);
                    double fac = Math.Sqrt(-2.0 * Math.Log(len) / len);

                    _normCached = r2 * fac;
                    return r1 * fac;
                }
            }


            /// <summary>
            /// Generates a random permutation of the [0...numElems-1]
            /// </summary>
            /// <param name="numElems">The number of elements in the set to permute</param>
            /// <returns></returns>
            public static int[] NextPermutation(int numElems)
            {
                int[] ordering = new int[numElems];

                // Start with the identity permutation.
                for (int pos = 0; pos < numElems; ++pos) ordering[pos] = pos;

                for (int pos = numElems - 1; pos > 0; --pos)
                {
                    // The element at position 'pos' is randomly chosen from 
                    // the elements in position [0 ... pos]
                    int randomPos = Dpu.Utility.SharedRandom.Generator.Next(pos);
                    int randomElem = ordering[randomPos];
                    ordering[randomPos] = ordering[pos];
                    ordering[pos] = randomElem;
                }
                return ordering;
            }
        }

//#if INTERNAL_DPU
        /// <summary>
        /// Computes a cheap hash key for a sequence of integers.  
        /// Based on the random number generator by Knuth.
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(false)]
        public class CheapHash 
        {
            ulong key;

            /// <summary>
            ///  Cast to an int in a reasonable way.
            /// </summary>
            public int Key 
            {
                get { return (int)(key & (ulong) 0x7fffffff); }
            }

            public CheapHash( long init) 
            {
                key = (ulong) init;
            }
 
            /// <summary>
            /// Add another element to the key
            /// </summary>
            public void Add ( int n ) 
            {
                key ^= (1664525L * key * (ulong)((int) n)) + 1013904223L;
            }

            /// <summary>
            /// Add another element to the key
            /// </summary>
            public void Add ( double n ) 
            {
                key ^= (1664525L * key * (ulong)((int) n)) + 1013904223L;
            }
        }


        /// <summary>
        /// Estimate properties of a distribution
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(false)]
        public class DistributionEstimate 
        {
            int   _nexample;
            double _expect;
            double _expect_sq;
          
            public DistributionEstimate() 
            {
                _nexample = 0;
                _expect   = 0.0;
                _expect_sq = 0.0;
            }

            public double Mean 
            {
                get { return _expect; }
            }

            public double Variance 
            {
                get { return _expect_sq - _expect * _expect; }
            }

            public int Count 
            {
                get { return _nexample; }
            }

            public double Stdev 
            {
                get { return Math.Sqrt(this.Variance); }
            }
          
            /// <summary>
            /// Add another sample to the distribution
            /// </summary>
            /// <param name="newValue"></param>
            public void Add(double newValue) 
            {
                // Uses recursive estimation of these params (from Gauss!)
                _expect = ((_nexample / (double) (_nexample + 1)) * _expect) + (newValue / (double) (_nexample + 1));
                _expect_sq = (_nexample / (double) (_nexample + 1)) * _expect_sq + newValue*newValue / (double) (_nexample + 1);
                ++_nexample;
            }

        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public sealed class Permutation
        {
            // Fxcop
            private Permutation() {}

            public static int[] Generate(int size)
            {
                int[] res = new int[size];
                Generate(res);
                return res;
            }


            public static void Generate(int[] res)
            {
                int size = res.Length;
                Identity(res);

                for (int n = 0; n < size; ++n)
                {
                    int n1 = SharedRandom.Generator.Next(size-n);
                    int val1 = res[n];
                    int val2 = res[n+n1];
                    res[n] = val2;
                    res[n+n1] = val1;
                }
            }

            public static void Identity(int[] perm)
            {
                int size = perm.Length;

                for(int n = 0; n < size; ++n)
                {
                    perm[n] = n;
                }
            }

            public static int[] Identity(int size)
            {
                int[] res = new int[size];
                Identity(res);
                return res;
            }

            public static int[] Reverse(int size)
            {
                int[] res = new int[size];
                for(int n = size-1; n >= 0; --n)
                {
                    res[n] = n;
                }

                return res;
            }

		}
//#endif // INTERNAL_DPU
    }
}
