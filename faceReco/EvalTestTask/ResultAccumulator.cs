using System;
using System.Collections.Generic;
using System.Text;

namespace EvalTestTask
{
    /// <summary>
    /// Accumulate counts for category
    /// </summary>
    public class ResultAccumulator
    {
        int _totalCount;
        int _correctCount;
        int _rejectCount;
        int _id;
        double _sum;
        double _sum2;
        double _min;
        double _max;

        public ResultAccumulator(int id)
        {
            _id = id;
            Reset();
        }


        public void AddBinaryResult(int target, int value)
        {
            ++_totalCount;

            if (value == 0)
            {
                ++_rejectCount;
            }
            else if (target == value)
            {
                ++_correctCount;
            }
        }

        public void AddRealResult(double value)
        {
            _sum += value;
            _sum2 += value * value;

            if (_totalCount == 0)
            {
                _min = value;
                _max = value;
            }
            else
            {
                _min = Math.Min(_min, value);
                _max = Math.Max(_max, value);
            }

            ++_totalCount;
        }

        public int RejectCount
        {
            get
            {
                return _rejectCount;
            }
        }

        public double Reject
        {
            get
            {
                double ret = 0.0;
                if (_totalCount > 0)
                {
                    ret = (double)_rejectCount / (double)_totalCount;
                }
                return ret;
            }
        }

        public double Average
        {
            get
            {
                double ret = 0.0;

                if (_totalCount > 0)
                {
                    ret = _sum / _totalCount;
                }

                return ret;
            }
        }
        public int ID
        {
            get
            {
                return _id;
            }
        }

        public double StdDev
        {
            get
            {
                double ret = 1.0;

                if (_totalCount > 1)
                {
                    double avg = _sum / _totalCount;
                    ret = _sum2 / _totalCount - avg* avg;
                    ret *=  _totalCount / (_totalCount-1.0);
                    ret = Math.Sqrt(ret);
                }

                return ret;
            }
        }

        public double Min
        {
            get
            {
                return _min;
            }
        }
        public double Max
        {
            get
            {
                return _max;
            }
        }

        public int Total
        {
            get
            {
                return _totalCount;
            }
        }

        public double ErrorRate
        {
            get
            {
                int totPred = _totalCount - _rejectCount;
                if (totPred > 0)
                {
                    return (double)Error / (double)totPred;
                }
                else
                {
                    return 0.0;
                }
            }
        }

        public int Correct
        {
            get
            {
                return _correctCount;
            }
        }

        public int Error
        {
            get
            {
                return _totalCount - _rejectCount - _correctCount;
            }
        }
        private void Reset()
        {
            _totalCount = 0;
            _correctCount = 0;

            _sum = 0.0;
            _sum2 = 0.0;
        }
    }
}
