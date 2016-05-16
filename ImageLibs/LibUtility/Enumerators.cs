using System;
using System.Collections;

namespace Dpu.Utility
{
    /// <summary>
    /// Enumerate the non-null items in a list.
    /// </summary>
    public class NonNullEnumerator : IEnumerator
    {
        #region Constructor
        public NonNullEnumerator(IList list)
        {
            _list = list;
            Reset();
        }
        #endregion

        #region Fields
        private object _current;
        private int _cursor;
        private IList _list;
        #endregion

        #region Methods
        public void Reset()
        {
            _cursor = -1;
            _current = null;
        }

        public object Current { get { return _current; } }

        public bool MoveNext()
        {
            _cursor++;
            while(_cursor < _list.Count && _list[_cursor] == null)
            {
                _cursor++;
            }
            
            if(_cursor == _list.Count) return false;

            _current = _list[_cursor];
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Dumb enumerator that simply return a sequence of ints between min and max.
    /// </summary>
    public class IntEnumerator : IEnumerator
    {
        #region Constructor
        public IntEnumerator (int min, int max)
        {
            _minIndex = min;
            _current = _minIndex - 1;
            _maxIndex = max;
        }
        #endregion

        #region Fields
        private int _current = -1;
        private int _minIndex;
        private int _maxIndex;
        #endregion

        #region Methods
        public void Reset()
        {
            _current = _minIndex - 1;
        }

        public object Current
        {
            get { return _current; }
        }

        public bool MoveNext()
        {
            if (_current < _maxIndex) 
            {
                ++_current;
                return true;
            }
            else
                return false;
        }
        #endregion

    }

    /// <summary>
    /// A class that enumerates over enumerators.  For instance
    /// "( ( 0 1 ) ( 2 3 ) )" would enumerate to "0, 1, 2, 3"
    /// </summary>
    public class TwoLevelEnumerator : IEnumerator
    {
        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        public TwoLevelEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }
        #endregion

        #region Fields
        IEnumerator _enumerator;
        IEnumerator _subEnum;
        #endregion

        #region Methods
        public void Reset()
        {
            _enumerator.Reset();
            _subEnum = null;
        }

        public object Current { get { return _subEnum.Current; } }

        public bool MoveNext()
        {
            bool subEnumHasMore = false;
            while(_subEnum == null || !(subEnumHasMore = _subEnum.MoveNext()))
            {
                bool enumHasMore = _enumerator.MoveNext();
                if(!enumHasMore)
                {
                    break;
                }
                _subEnum = ((IEnumerable)_enumerator.Current).GetEnumerator();
                _subEnum.Reset();
            }
            return subEnumHasMore;
        }
        #endregion

        #region Unit Test
        public static void UnitTest()
        {
            ArrayList a1 = new ArrayList();
            ArrayList a2 = ArrayUtils.List("1", "2");
            ArrayList a3 = new ArrayList();
            ArrayList a4 = ArrayUtils.List("3");
            ArrayList a5 = new ArrayList();
            ArrayList test1 = ArrayUtils.List(a1, a2, a3, a4, a5);
            int test1Count = UnitTestCount("Test1", new TwoLevelEnumerator(test1.GetEnumerator()));
            UnitTestAssert("Test1", test1Count, 3);
            
            ArrayList a6 = ArrayUtils.List("1");
            ArrayList a7 = ArrayUtils.List("2", "3");
            ArrayList a8 = ArrayUtils.List("4");
            ArrayList test2 = ArrayUtils.List(a6, a7, a8);
            int test2Count = UnitTestCount("Test2", new TwoLevelEnumerator(test2.GetEnumerator()));
            UnitTestAssert("Test2", test2Count, 4);
        }

        private static void UnitTestAssert(string caption, int count, int desiredCount)
        {
            if(count != desiredCount)
            {
                string err = String.Format("{0}: Got {1} items, Wanted {2} items", caption, count, desiredCount);
                throw new TestException(err);
            }
        }

        private static int UnitTestCount(string caption, IEnumerator e)
        {
            int count = 0;

            Log.WriteLine(caption + " {");
            Log.Indent(1);
            while(e.MoveNext())
            {
                Log.WriteLine(e.Current.ToString());
                count++;
            }
            Log.Indent(-1);
            Log.WriteLine("}");

            return count;
        }
        #endregion
    }
}
