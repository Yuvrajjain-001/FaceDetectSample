using System;
using System.Collections;

namespace Dpu.Utility
{
	/// <summary>
	/// Generic interface to algorithms which sample based on weight.
	/// </summary>
	public interface IWeightedSet 
	{
		int    Count 
		{
			get ;
		}

		double Weight(int exampleNum);
		bool IsSelected(int exampleNum);
	}

	/// <summary>
	/// Handy functions for sampling from weighted sets.
	/// </summary>
	public class Sampling
	{

		/// <summary>
		/// Sum of the weights in the selected set.
		/// </summary>
		public static double WeightedSum(IWeightedSet weightedSet)
		{
			double sum = 0;
			for (int nexample = 0; nexample < weightedSet.Count; ++nexample)
			{
				if (weightedSet.IsSelected(nexample)) 
				{
					// Update running sum.
					sum += weightedSet.Weight(nexample);
				}
			}
			return sum;
		}



		/// <summary>
		/// Randomly sample elements by weight (from a weighted set).
		/// </summary>
		/// <param name="weightedSet">Weighted set</param>
		/// <param name="total">Total number of examples to sample</param>
		public static int[] SampleByWeight(IWeightedSet weightedSet, int total)
		{
			return   SampleByWeight(weightedSet, total, WeightedSum(weightedSet));
		}

		/// <summary>
		/// Sample elements from a weighted set.  
		/// </summary>
		/// <param name="weightedSet">Weighted set</param>
		/// <param name="total">Total number of examples to sample</param>
		/// <param name="sum">Sum of the sampled examples.</param>
		public static int[] SampleByWeight(IWeightedSet weightedSet, int total, double sum)
		{
			int[] vnres = new int[total];

			// Simple algorithm sample from a set of examples based on the weight of each
			// example.  i.e. examples with twice the weight should get selected twice as
			// often.

			float[] fsample = new float[total];
			Random rand = SharedRandom.Generator;

			// Generate a set of random variable between 0 and total sum.
			for(int nsample = 0; nsample < total; ++nsample) 
			{
				fsample[nsample] = (float) (rand.NextDouble() * sum);
			}

			// Sort these smallest first
			Array.Sort(fsample);

			// Simultaneously sweep through the array of weights and the array of samples.
			double frunningSum = 0;

			int cexample = weightedSet.Count;

			int ncurr = 0;
			for (int nexample = 0; nexample < cexample && ncurr < total; ++nexample) 
			{
				if (weightedSet.IsSelected(nexample)) 
				{
					// Update running sum.
					frunningSum += weightedSet.Weight(nexample);

					// When the running sum becomes larger than the current biggest sample pick it.
					// And keep picking
					while (ncurr < total && frunningSum > fsample[ncurr]) 
					{
						vnres[ncurr] = nexample;
						++ncurr;
					}
				}
			}

			return vnres;
		}

		#region Testing
		/// <summary>
		/// Test class.
		/// </summary>
		private class TestWeightedSet : IWeightedSet
		{
			private double[] _weights;
			
			public TestWeightedSet(double[] weights) 
			{
				_weights = weights;
			}

			public int Count
			{
				get
				{
					return _weights.Length;
				}
			}

			public double Weight(int exampleNum)
			{
				return _weights[exampleNum];
			}

			public bool IsSelected(int exampleNum)
			{
				return true;
			}

		}


		public static void Test() 
		{
			int elementCount = 1000;
			int iterCount = 3;
			int sampleCount = 50;

			double[] weights = new double[elementCount];

			// First generate a set of weights...  
			double weightSum = 0;
			for(int n = 0; n < elementCount; ++n) 
			{
				weights[n] = Math.Pow(Math.Abs(Math.Sin(10 * n / (double) elementCount)), 4.0);
				weightSum += weights[n];
			}

			// Normalize weights
			for(int n = 0; n < elementCount; ++n) 
			{
				weights[n] = weights[n] / weightSum;
			}

			// Compute explicit integration over the range.
			double sum = 0;
			for(int n = 0; n < elementCount; ++n) 
			{
				sum += n * weights[n];
			}
			Console.WriteLine("Explicit integration {0}", sum);

			TestWeightedSet testSet = new TestWeightedSet(weights);

			for(int n = 0; n < iterCount; ++n) 
			{
				int[] samples = SampleByWeight(testSet, sampleCount);

				Console.WriteLine("Drew {0} samples.", samples.Length);

				// Compute sample integration.
				sum = 0;
				foreach(int num in samples) 
				{
					Console.WriteLine("{0} -> {1}", num, weights[num]);
					sum += num;
				}
				Console.WriteLine("Approximate integration using samples {0}", sum / (double) sampleCount);
			}
		}
		#endregion // Testing

	}
}
