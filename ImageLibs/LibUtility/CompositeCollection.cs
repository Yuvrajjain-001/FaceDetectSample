using System;
using System.Collections;

namespace Dpu.Utility
{
	/// <summary>
	/// A collection of ICollections
	/// </summary>
    public class CompositeCollection : ICollection
    {
        #region Constructors
        public CompositeCollection(params ICollection[] contents)
        {
            _contents = contents;
        }

        public CompositeCollection(IList contents)
        {
            _contents = contents;
        }
        #endregion

        #region Fields
        IList _contents;
        int _count = -1;
        #endregion

        #region Properties
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                if(_count < 0)
                {
                    _count = CountElements();
                }
                return _count;
            }
        }

        public object SyncRoot
        {
            get
            {
                return null;
            }
        }
        #endregion

        #region Methods
        int CountElements()
        {
            int count = 0;
            for(int i = 0; i < _contents.Count; i++)
            {
                count += ((ICollection)_contents[i]).Count;
            }
            return count;
        }

        public void CopyTo(Array array, int index)
        {
            for(int i = 0; i < _contents.Count; i++)
            {
                ((ICollection)_contents[i]).CopyTo(array, index);
                index += ((ICollection)_contents[i]).Count;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new CompositeEnumerator(this);
        }

        public static void UnitTest()
        {
            ICollection[] contents = new ICollection[10];
            for(int i = 0; i < contents.Length; i++)
            {
                contents[i] = new ArrayList();
            }
            //leave the first one, last one, and two consecutive in the middle
            //empty to test corner cases
            int middle = 6;
            int id = 0; // generate some unique id's to put in the composite
            for(int i = 1; i < middle; i++)
            {
                int rand = (int)(10*SharedRandom.Generator.NextDouble());
                ArrayList arr = (ArrayList)contents[i];
                for(int j = 0; j < rand; j++)
                {
                    arr.Add(id++);
                }
            }
            for(int i = middle+2; i < contents.Length-1; i++)
            {
                int rand = (int)(10*SharedRandom.Generator.NextDouble());
                ArrayList arr = (ArrayList)contents[i];
                for(int j = 0; j < rand; j++)
                {
                    arr.Add(id++);
                }
            }

            CompositeCollection comp = new CompositeCollection(contents);

            //now enumerate over the whole thing
            ArrayList concat = new ArrayList();
            int cursor = 0;
            foreach(object o in comp)
            {
                concat.Add(o);
                cursor++;
            }
            if(concat.Count != comp.Count)
            {
                throw new TestException("CompositeCollection test failed");
            }
        }
        #endregion

        class CompositeEnumerator : IEnumerator
        {
            #region Constructor
            public CompositeEnumerator(CompositeCollection composite)
            {
                _composite = composite;
                Reset();
            }
            #endregion

            #region Fields
            CompositeCollection _composite;
            int _cursor;
            IEnumerator _enum;
            #endregion

            #region Properties
            public object Current { get { return _enum.Current; } }
            #endregion

            #region Methods
            public void Reset()
            {
                _cursor = 0;
                _enum = null;
            }

            public bool MoveNext()
            {
                bool curEnumHasMore = false;
                while((_cursor < _composite._contents.Count-1) &&
                    (_enum == null || !(curEnumHasMore = _enum.MoveNext())))
                {
                    _cursor++;
                    _enum = ((ICollection)_composite._contents[_cursor]).GetEnumerator();
                    _enum.Reset();
                }
                return curEnumHasMore;
            }
            #endregion
        }
    }
}
