//------------------------------------------------------------------------------
// <copyright from='2004' to='2004' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   

namespace System.Windows.Ink.Analysis.MathLibrary
{
	public class Sampler 
	{
		private static Random _random = new Random((int)System.DateTime.Now.Ticks);
		private static double _cachedNormalSample = double.MinValue;

		public static void Initialize(int seed)
		{
			_random = new Random(seed);
		}

		/// <summary>
		/// Sample from the uniform distribution in [0, 1].
		/// </summary>
		public static double GetUniformDistributionZeroToOneSample() 
		{
			return _random.NextDouble();
		}

		/// <summary>
		/// Sample from the standard normal distribution with mean 0.0 and standard deviation 1.0
		/// </summary>
		public static double GetStandardNormalDistributionSample()
		{
			if (_cachedNormalSample != double.MinValue) 
			{
				double r1 = _cachedNormalSample;
				_cachedNormalSample = double.MinValue;
				return r1;
			}
			else 
			{
				double r1, r2, length;
				do 
				{ 
					r1 = 2.0 * GetUniformDistributionZeroToOneSample() - 1.0;
					r2 = 2.0 * GetUniformDistributionZeroToOneSample() - 1.0;
					length = r1 * r1 + r2 * r2;
				} while (length >= 1.0 || length == 0.0);

				double factor = Math.Sqrt(-2.0 * Math.Log(length) / length);
				_cachedNormalSample = r2 * factor;
				return r1 * factor;
			}
		}

		/// <summary>
		/// Sample from the normal distribution with a given mean and standard deviation.
		/// </summary>
		public static double GetNormalDistributionSample(double mean, double standardDeviation) 
		{
			Debug.Assert(standardDeviation > 0);
			return GetStandardNormalDistributionSample() * standardDeviation + mean;
		}
	}	
}
