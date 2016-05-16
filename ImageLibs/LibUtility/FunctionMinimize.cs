// FunctionMinimize.cs
//
// cscargs:  /unsafe /target:library /debug /r:c:/MSR/LiveCode/private/research/private/CollaborativeLibs_01/Sho/Bin/LibUtility.dll 


using System;

namespace Dpu.Utility
{
	using RangeType = System.Double;
	using ValueType = System.Double;
	/// <summary>
	/// Summary description for FunctionMinimize.
	/// </summary>
	public class FunctionMinimize
	{
		public delegate ValueType Function(RangeType r);

		public static RangeType ProbePoint(RangeType lo, RangeType hi)
		{
			return lo + (hi - lo) / 1.7;
		}

        public static RangeType Minimize(
            RangeType lo, RangeType hi, RangeType epsilonRange, ValueType epsilonValue,
            Function func
            )
        {
            double dummy;
            return MinimizeOut(lo, hi, epsilonRange, epsilonValue, func, out dummy);
        }

        public static RangeType Minimize(
        RangeType lo, RangeType hi, RangeType epsilonRange, ValueType epsilonValue,
        Function func, out double minVal
        )
        {
            return MinimizeOut(lo, hi, epsilonRange, epsilonValue, func, out minVal);
        }

        
        //void BOOST::FindLambda(CLASSIFIER *pC, int num)
//{
//    SampleFramesAndComputeScores(pC, num); 

//    // below is my implementation of the golden section search 
//    //   |----------------|-----------|-----------------|
//    // dLow            dMed1       dMed2              dHigh
//    float dLow=-20, dHigh=20, dMed1, dMed2; 
//    double dLowVal, dHighVal, dMed1Val, dMed2Val; 
//    double epsRange = 0.01, epsValue = 0.1; 

//    dMed2 = GetGoldPoint(dLow, dHigh);
//    dMed1 = GetGoldPoint(dLow, dMed2); 
//    dLowVal = ComputeLogLikelihood(dLow); 
//    dHighVal = ComputeLogLikelihood(dHigh); 
//    dMed1Val = ComputeLogLikelihood(dMed1); 
//    dMed2Val = ComputeLogLikelihood(dMed2); 
//    double diffRange = fabs(dMed1 - dMed2); 
//    double diffValue = fabs(dMed1Val - dMed2Val); 
    
//    // to start the golden section search, it must be satisfied that dMedVal < dLowVal and dMedVal < dHighVal
//    while (diffRange > epsRange || diffValue > epsValue) 
//    {
//        if (dMed1Val > dMed2Val) 
//        {
//            dLow = dMed1; dLowVal = dMed1Val;       // dLow as dMed1
//            dMed1 = dMed2; dMed1Val = dMed2Val;     // dMed1 as dMed2
//            dMed2 = GetGoldPoint(dLow, dHigh); 
//            dMed2Val = ComputeLogLikelihood(dMed2); 
//        }
//        else
//        {
//            dHigh = dMed2; dHighVal = dMed2Val;     // dHigh as dMed2
//            dMed2 = dMed1; dMed2Val = dMed1Val;     // dMed2 as dMed1
//            dMed1 = GetGoldPoint(dLow, dMed2); 
//            dMed1Val = ComputeLogLikelihood(dMed1); 
//        }
//        diffRange = fabs(dMed1 - dMed2); 
//        diffValue = fabs(dMed1Val - dMed2Val); 
//    }
//    if (dMed1Val > dMed2Val) 
//    {
//        pC[num-1].SetAlpha(float(-1.0f*dMed2));
//        pC[num-1].SetBeta(float(dMed2)); 
//    }
//    else
//    {
//        pC[num-1].SetAlpha(float(-1.0f*dMed1));
//        pC[num-1].SetBeta(float(dMed1)); 
//    }
//}
		public static RangeType MinimizeOut(
			RangeType lo, RangeType hi, RangeType epsilonRange, ValueType epsilonValue,
			Function func,
            out RangeType minValue
			)
		{
			RangeType med = ProbePoint(lo, hi);
			ValueType loVal = func(lo);
			ValueType hiVal = func(hi);
			ValueType medVal = func(med);

            ValueType diffValue = ValueType.MaxValue;
            RangeType diffRange = RangeType.MaxValue;

			while( diffValue > epsilonValue ||  diffRange > epsilonRange) 
			{
				// Console.WriteLine("Testing {0} < {1} < {2}", lo, med, hi);
				// Console.WriteLine("Testing {0}, {1}, {2}", loVal, medVal, hiVal);

				if (medVal > loVal) 
				{
					hi = med;
					hiVal = medVal;
					med = ProbePoint(lo, hi);
					medVal = func(med);
				}
				else if (medVal > hiVal) 
				{
					lo = med;
					loVal = medVal;
					med = ProbePoint(lo, hi);
					medVal = func(med);
				}
				else 
				{
					// Console.WriteLine("medVal below both ends.");
					RangeType probeLo = ProbePoint(lo, med);
					RangeType probeHi = ProbePoint(med, hi);
					ValueType probeLoVal = func(probeLo);
					if (probeLoVal < medVal) 
					{
						hi = med;
						hiVal = medVal;
						med = probeLo;
						medVal = probeLoVal;
						continue;
					}
					ValueType probeHiVal = func(probeHi);
					if (probeHiVal < medVal)
					{
						lo = med;
						loVal = medVal;
						med = probeHi;
						medVal = probeHiVal;
					}
					else 
					{
						lo = probeLo;
						loVal = probeLoVal;
						hi = probeHi;
						hiVal = probeHiVal;
					}
				}
                diffValue = Math.Max(Math.Abs(hiVal - medVal), Math.Abs(loVal - medVal));
                diffRange = hi - lo;
            }
            minValue = medVal;
			return med;
		}

		private static ValueType _TestFunc(RangeType r) 
		{
			return - Math.Cos(r);
		}

		public static void Test()
		{
			RangeType lo = -0.1;
			RangeType hi = 0.7;
			RangeType epRange = 0.001;
            ValueType epValue = 0.001;
			Console.WriteLine("Minimizing -cos between {0} and {1} with epsilon {2}", 
				lo, hi, epRange);

			Console.WriteLine("Minimum found at {0}", 
				FunctionMinimize.Minimize(lo, hi, epRange, epValue, new Function(_TestFunc))
				);
		}
	}
}
