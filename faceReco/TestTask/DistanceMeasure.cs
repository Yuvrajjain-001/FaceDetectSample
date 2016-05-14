using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask
{
    public struct DistanceMeasure
    {
        public Face face;
        public double Distance;
    }

    public class DistanceMeasureSort : System.Collections.IComparer
    {
        public DistanceMeasureSort()
        {

        }

        virtual public int Compare(Object a, Object b)
        {
            DistanceMeasure a1 = (DistanceMeasure)a;
            DistanceMeasure b1 = (DistanceMeasure)b;

            double delt = a1.Distance - b1.Distance;

            if (delt == 0.0)
            {
                return 0;
            }
            if (delt > 0)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
    }
}
