//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // Debug functionalities.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    /// <summary>
    /// Class to represent the distribution of a sample set of doubles and provide some
    /// basic statistics about the distribution, i.e., min, max, mean, variance,
    /// standard deviation and median.
    /// </summary>
    public class Distribution
    {
        #region Fields
        protected double _minValue;
        protected double _maxValue;
        protected double _sum;
        protected double _squaredSum;
        protected DoubleArray _data;

        // Double.MinValue indicates the median is not computed yet.
        private double _median = Double.MinValue;

        #endregion

        #region Properties
        /// <summary>
        /// Number of samples in the distribution
        /// </summary>
        public int Count
        {
            get { return this._data.Count; }
        }

//#if INTERNAL_PARSER
        /// <summary>
        /// The largest sample in the distribution
        /// </summary>
        public double Max
        {
            get
            {
                Debug.Assert(Count > 0, "Distribution is empty");
                return _maxValue;
            }
        }

        /// <summary>
        /// The smallest sample in the distribution
        /// </summary>
        public double Min
        {
            get
            {
                Debug.Assert(Count > 0, "Distribution is empty");
                return _minValue;
            }
        }
//#endif // INTERNAL_PARSER

        /// <summary>
        /// The mean or average of the distribution
        /// </summary>
        public double Mean
        {
            get
            {
                Debug.Assert(Count > 0, "Distribution is empty");
                return _sum / Count;
            }
        }

        /// <summary>
        /// The floor(n/2)-th smallest sample in a distribution of size n.
        /// </summary>
        public double Median
        {
            get
            {
                Debug.Assert(Count > 0, "Distribution is empty");

                if ( this._median == Double.MinValue )
                {
                    this._median = ComputeMedian( (double[])this._data.ToArray() );
                }

                Debug.Assert( this._median != Double.MinValue,
                    "All samples in the distribution are Double.MinValue. " +
                    "Are you sure it is by design?" );

                return this._median;
            }
        }

        /// <summary>
        /// The sample variance of the distribution
        /// </summary>
        public double Variance
        {
            get
            {
                Debug.Assert(Count > 0, "Distribution is empty");
                // A single number has zero variance.
                // Avoid dividing by 0 below.
                if (Count == 1)
                {
                    return 0;
                }

                double average = Mean;
                int n = Count;
                return (_squaredSum - n * average * average) / (n - 1);
            }
        }

//#if INTERNAL_PARSER
        /// <summary>
        /// The sample standard deviation of the distribution (squared root of the variance)
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                return Math.Sqrt( Variance );
            }
        }


		/// <summary>
        /// The index-th sample in the distribution
        /// </summary>
        public double this[int index]
        {    
            get
            {
                return (double) this._data[index];
            }
        }
//#endif // INTERNAL_PARSER
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public Distribution()
        {
            Clear();
        }

        /// <summary>
        /// Build a distribution with an initial capacity
        /// </summary>
        public Distribution(int capacity)
        {
            this._minValue = Double.MaxValue;
            this._maxValue = Double.MinValue;
            //this._sum = 0.0;
            //this._squaredSum = 0.0;
            this._data = new DoubleArray(capacity);
            this._median = Double.MinValue;
        }
        #endregion Constructor

        #region Methods

        /// <summary>
        /// Reset to an empty distribution
        /// </summary>
        public void Clear()
        {
            this._minValue = Double.MaxValue;
            this._maxValue = Double.MinValue;
            this._sum = 0.0;
            this._squaredSum = 0.0;
            this._data = new DoubleArray();
            this._median = Double.MinValue;
        }

        /// <summary>
        /// Add a sample to the distribution
        /// </summary>
        /// <param name="newValue">The sample value to add.</param>
        public void Add(double newValue)
        {
            if (_minValue > newValue)
            {
                _minValue = newValue;
            }
            if (_maxValue < newValue)
            {
                _maxValue = newValue;
            }
            _sum += newValue;
            _squaredSum += newValue * newValue;
            this._data.Add(newValue);

            this._median = Double.MinValue;

        }

        /// <summary>
        /// Compute the median of a double array.
        /// </summary>
        /// <remarks>The median is defined as the [arr.Length / 2] smallest
        /// element. The input array will be rearranged to have the return value
        /// in location arr[arr.Length / 2], with all smaller elements moved to
        /// the first half and all larger elements moved to the second half,
        /// both in arbitrary order. </remarks>
        public static double ComputeMedian( double[] arr )
        {
            return ComputeKthSmallest( arr.Length / 2, arr );
        }

        /// <summary>
        /// Return the Kth smallest value in the double Array arr.
        /// Adapted from Numerical Recipes in C.
        /// </summary>
        /// <remarks> The input array will be rearranged to have the return value
        /// in location arr[k-1], with all smaller elements moved to arr[0..k-2] and
        /// all larger elements to arr[k..n-1], both in arbitrary order. </remarks>
        public static double ComputeKthSmallest( int k, double[] arr )
        {
            Debug.Assert( arr.Length > 0,
                          "Cannot call the function with an empty array." );

            Debug.Assert( k >= 0 && k < arr.Length,
                          "The K value is out of the bound of the array." );

            int l = 0;
            int ir = arr.Length-1;

            // Special case: single-element array
            if ( ir == 0)
            {
                return arr[0];
            }

            for (;;) 
            {
                // Active partition contains 1 or 2 elements
                if (ir <= l+1)
                {
                    // Case of 2 elements
                    if (ir == l+1 && arr[ir] < arr[l])
                    {
                        Utility.SwapDouble( ref arr[l], ref arr[ir]);
                    }
                    return arr[k-1];  // exit point
                } 
                else 
                {
                    // Choose median of left, center, and right elements as
                    // partitioning element a. Also rearrange so that
                    // arr[l] <= arr[l+1], arr[ir] >= arr[l+1]
                    int mid = (l+ir) >> 1;
                    Utility.SwapDouble( ref arr[mid], ref arr[l+1]);
                    if (arr[l] > arr[ir]) 
                    {
                        Utility.SwapDouble( ref arr[l], ref arr[ir]);
                    }
                    if (arr[l+1] > arr[ir]) 
                    {
                        Utility.SwapDouble( ref arr[l+1], ref arr[ir]);
                    }
                    if (arr[l] > arr[l+1]) 
                    {
                        Utility.SwapDouble( ref arr[l], ref arr[l+1]);
                    }
                    // Initialize pointers for partitioning
                    int i = l + 1;
                    int j = ir;
                    double a = arr[l+1];  // Partitioning element
                    for (;;)
                    {
                        // Scan up to find an element > a
                        do
                        {
                            i++;
                        }
                        while ( arr[i] < a );
                        // Scan down to find an element < a
                        do
                        {
                            j--;
                        }
                        while ( arr[j] > a );
                        // Pointers crossed. Partitioning complete.
                        if (j < i)
                        {
                            break;
                        }
                        Utility.SwapDouble( ref arr[i], ref arr[j]);
                    }
                    // Insert partitioning element
                    arr[l+1] = arr[j];
                    arr[j] = a;
                    // Keep active the partition that contains the Kth element
                    if (j >= k-1)
                    {
                        ir = j-1;
                    }
                    if (j <= k-1)
                    {
                        l = i;
                    }
                }
            }
        }


        #endregion
    }
}
