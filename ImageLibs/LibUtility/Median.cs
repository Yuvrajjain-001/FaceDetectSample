using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Dpu.Utility
{
    using WEIGHT = System.Single;
    using DATA = System.Single;
    
    /// <summary>
    /// Computes the weighted median using sorting...  which is very simple,  but potentially slow.
    /// </summary>
    /// <typeparam name="DATA">Type of data to arrange</typeparam>
    public class Median // <DATA>
    {
        DATA[] _data;
        WEIGHT[] _weight;

        public Median(DATA[] dataVector, WEIGHT[] weightVector)
        {
            _data = dataVector;
            _weight = weightVector;
        }

        /// <summary>
        /// Compute the weighted median of the dataVector/weightVector
        /// </summary>
        /// <param name="index">Index of the weighted</param>
        /// <param name="below"></param>
        /// <param name="above"></param>
        /// <returns></returns>
        public static DATA Find(WEIGHT quantile, DATA[] dataVector, WEIGHT[] weightVector, out int index, out WEIGHT below, out WEIGHT above)
        {
            // Median<DATA> med = new Median<DATA>(dataVector, weightVector);
            Median med = new Median(dataVector, weightVector);
            return med.Compute(quantile, out index, out below, out above);
        }

        public DATA Compute(WEIGHT quantile, out int index, out WEIGHT below, out WEIGHT above)
        {
            if (_data.Length == 1)
            {
                below = 0;
                above = 0;
                index = 0;
                return _data[0];
            }

            System.Array.Sort(_data, _weight);

            WEIGHT totalSum = 0;
            foreach(WEIGHT w in _weight)
            {
                totalSum += w;
            }

            WEIGHT belowSum = 0;
            int i = 0;
            for (;  i < _data.Length; ++i)
            {
                belowSum += _weight[i];
                if ((belowSum / totalSum) > quantile)
                {
                    break;
                }
            }
            above = totalSum - belowSum;
            below = belowSum - _weight[i];
            index = i;
            return _data[i];
        }
    }

    public class FastMedian // n<DATA>
        // where DATA : IComparable
    {
        DATA[] _data;
        WEIGHT[] _weight;
        int[] _index;
        // WEIGHT _sumWeight = 0;

        public static void InitIndex(int[] res)
        {
            int count = res.Length;
            for (int i = 0; i < count; ++i)
            {
                res[i] = i;
            }
        }

        public static int[] IndexVector(int count)
        {
            int[] res = new int[count];
            InitIndex(res);
            return res;
        }


        public FastMedian(DATA[] dataVector, WEIGHT[] weightVector)
            : this(dataVector, weightVector, IndexVector(dataVector.Length))
        {
        }

        public FastMedian(DATA[] dataVector, WEIGHT[] weightVector, int[] indexVector)
        {
            _data = dataVector;
            _weight = weightVector;
            InitIndex(indexVector);
            _index = indexVector;
        }

        public int ElementIndex(int index)
        {
            return _index[index];
        }

        public DATA ElementValue(int index)
        {
            return _data[_index[index]];
        }

        public WEIGHT ElementWeight(int index)
        {
            return _weight[_index[index]];
        }

        /// <summary>
        /// Swap both the example and the weight.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private void ELEM_SWAP(int i, int j)
        {
            Debug.Assert(i < j);

            int tmp = _index[i];
            _index[i] = _index[j];
            _index[j] = tmp;

        }

        /// <summary>
        /// Selects a value from the set and separates the elements into those which are smaller and those which are larger.  
        /// Smaller are first, then the selected elements, then the larger.
        /// </summary>
        /// <param name="middleValue">Value selected.</param>
        /// <param name="position">Final position of the value.</param>
        /// <param name="belowWeight">Weight of the examples which are smaller</param>
        /// <param name="aboveWeight">Weight of the exmaples which are larger (including selected element)</param>

        public void Separate(int begin, int end, out DATA middleValue, out int position, out WEIGHT belowWeight, out WEIGHT aboveWeight)
        {
            // Invariants
            //  begin < end
            //  if (i >= end) then middle <= val[i]
            //  if (i <= begin) then middle > val[i]
            aboveWeight = 0;  // Weight of all examples which are after and including end
            belowWeight = 0;  // Weight of all examples which are before and including begin

            DATA beginValue = ElementValue(begin);
            DATA endValue = ElementValue(end);
            middleValue = (beginValue + endValue) / 2;

            if (begin == end)
            {
                position = end;
                aboveWeight = ElementWeight(position);
                return;
            }

            if (beginValue > endValue)
            {
               ELEM_SWAP(begin, end);
            }


            aboveWeight = ElementWeight(end);
            belowWeight = ElementWeight(begin);

            if (begin + 1 == end )
            {
                position = end;
                middleValue = ElementValue(position);
                return;
            }

            // Loop until a point is found where all element smaller than middleValue are before 
            // and all larger are after.  Elements are swapped until the condition is reached.
            while (true)
            {
                // Console.WriteLine("Begin {0} {1}", begin, end);
                // Search from the beginning looking for the first element which is not smaller than 
                // middleValue
                while (begin+1 < end)
                {
                    beginValue = ElementValue(begin+1);
                    if (middleValue.CompareTo(beginValue) <= 0)
                        break;
                    // Console.WriteLine("  begskip {0} -> {1}", begin, ElementValue(begin));
                    begin++;
                    belowWeight += ElementWeight(begin);
                }
                // Debug.Assert((begin + 1 == end) || (middleValue.CompareTo(ElementValue(begin+1)) <= 0));

                // Console.WriteLine("End  {0} {1}", begin, end);
                // Search from the end looking for the first element which is not larger than
                // middlevalue
                while (begin < end-1)
                {
                    endValue = ElementValue(end-1);
                    if (middleValue.CompareTo(endValue) >= 0)
                        break;
                    // Console.WriteLine("  endskip {0} -> {1}", end, ElementValue(end));
                    end--;
                    aboveWeight += ElementWeight(end);
                }
                // Debug.Assert((begin + 1 == end) || ((middleValue.CompareTo(ElementValue(begin + 1)) <= 0) && (middleValue.CompareTo(ElementValue(end - 1)) >= 0)));
                
                // eiler (begin+1) == end || (middleValue < val[begin+1] && middleValue > val[end-1] )

                if (begin + 1 == end)
                {
                    position = end;
                    middleValue = ElementValue(position);
                    return;
                }
                else if (begin + 1 == end - 1)
                {
                    end--;
                    aboveWeight += ElementWeight(end);
                    position = end;
                    middleValue = ElementValue(position);
                    return;
                }
                else // if ((middleValue.CompareTo(ElementValue(begin + 1)) <= 0) && (middleValue.CompareTo(ElementValue(end - 1)) >= 0))
                {
                    ELEM_SWAP(begin + 1, end - 1);
                    if (begin + 1 == end - 1)
                    {
                        end--;
                        aboveWeight += ElementWeight(end);
                    }
                    else
                    {
                        begin++;
                        end--;
                        belowWeight += ElementWeight(begin);
                        aboveWeight += ElementWeight(end);
                    }
                }
                // else
                //     throw new ApplicationException("Should not have begin > end.");
                // Console.WriteLine("Weight below {0} above {1}", belowWeight, aboveWeight);
            }
            // Never get here
        }


        public void Compute(WEIGHT quantile, out DATA medianValue, out int medianPosition, out WEIGHT weightBelow, out WEIGHT weightAbove)
        {
            Compute(0, Length - 1, quantile, out medianValue, out medianPosition, out weightBelow, out weightAbove);
        }

        /// <summary>
        /// Compute the median.  
        /// </summary>
        public void Compute(int beg, int end, WEIGHT quantile, out DATA medianValue, out int medianPosition, out WEIGHT weightBelow, out WEIGHT weightAbove)
        {
            // Works by recursively separating the set of values into two sets:  those that are smaller than middleValue and those 
            // that are larger.  Along the way keeps track of the sum of the weights above and below the split.  
            WEIGHT totAbove = 0;
            WEIGHT totBelow = 0;
            WEIGHT ratio = (1 - quantile) / quantile;
            while (true)
            {
                DATA middleValue;
                WEIGHT below;
                WEIGHT above;
                int index;
                Separate(beg, end, out middleValue, out index, out below, out above);
                if (ratio * (totBelow + below) > (totAbove + above))
                {
                    end = index - 1;
                    totAbove += above;
                    if (beg == end)
                    {
                        totAbove += below;
                        break;
                    }
                    // # print "BB below %f middle %f above %f sum %f" % (totBelow, below, totAbove, totBelow + below + totAbove)
                }
                else 
                {
                    beg = index;
                    totBelow += below;
                    // # print "AB below %f middle %f above %f sum %f" % (totBelow, above, totAbove, totBelow + above + totAbove)
                    if (beg == end)
                    {
                        totAbove += above;
                        break;
                    }
                }
            }
            // # print (value, index, below, above)
            // # print # mmm.ToString()
            medianValue = ElementValue(beg);
            medianPosition = beg;
            weightBelow = totBelow;
            weightAbove = totAbove;
            return;
        }

        public int Length
        {
            get { return _data.Length; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Length; ++i)
            {
                sb.Append(String.Format("{0}, ", ElementValue(i)));
            }
            sb.AppendLine();
            for (int i = 0; i < Length; ++i)
            {
                sb.Append(String.Format("{0}, ", ElementWeight(i)));
            }
            sb.AppendLine();
            return sb.ToString();
        }

#if NEVER 
        public void Select(WEIGHT quantile, out int index, out elem_type value)
        {
            int low, high;
            int middle, ll, hh;
            int n = arr.Length;
            low = 0; high = n - 1; 
            middle = (low + high) / 2;
            _partitionWeight = ComputeWeightSum(low, high);

            for (; ; )
            {
                // The weighted median is somewhere between LOW, MIDDLE, HIGH 
                if (high == low) /* One element only */
                {
                    index = ElementIndex(low);
                    value = ElementValue(low);
                    return;
                }
                if (high == low + 1) // Two elements only 
                { 
                    // Take the one with higher weight
                    if (ElementWeight(low) < ElementWeight(high))
                        ELEM_SWAP(low, high);
                    index = ElementIndex(low);
                    value = ElementValue(low);
                    return;
                }

                _belowWeight = ComputeWeightSum(low, middle);
                WEIGHT middleWeight = ElementWeight(middle);


                /* Find median of low, middle and high items; swap into position low */
                middle = (low + high) / 2;
                if (ElementValue(middle) > ElementValue(high))
                    ELEM_SWAP(middle, high);
                
                if (ElementValue(low) > ElementValue(high))
                    ELEM_SWAP(low, high);

                if (ElementValue(middle) > ElementValue(low))
                    ELEM_SWAP(middle, low);

                elem_type medianValue = ElementValue(low);

                /* Swap low item (now in position middle) into position (low+1) */
                ELEM_SWAP(middle, low + 1);

                /* Nibble from each end towards middle, swapping items when stuck */
                ll = low + 1;
                hh = high;
                WEIGHT belowWeight = 0;
                WEIGHT aboveWeight = 0;
                for (; ; )
                {
                    do
                    {
                        belowWeight += ElementWeight(ll++);
                    }
                    while (medianValue > ElementValue(ll));
                    do
                    {
                        aboveWeight += ElementWeight(hh++);
                    }
                    while (medianValue < ElementValue(hh));
                    if (hh < ll)
                        break;
                    ELEM_SWAP(ll, hh);
                }
                /* Swap middle item (in position low) back into correct position */
                ELEM_SWAP(low, hh);
                /* Re-set active partition */
                if (hh <= median)
                    low = ll;
                if (hh >= median)
                    high = hh - 1;
            }
        }
#endif
    }
}