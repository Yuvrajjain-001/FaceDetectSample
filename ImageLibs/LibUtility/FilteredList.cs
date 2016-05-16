using System;
using System.Collections;

namespace Dpu.Utility
{
    public abstract class ReadOnlyList : IList
    {
        //subclasses should fill this in
        public abstract int Count { get; }
        public abstract object this[int i] { get; set; }
        public abstract IEnumerator GetEnumerator();

        public object SyncRoot { get { return this; } }
        public virtual bool IsSynchronized { get { return false; } }
        public virtual int Add(object o) { throw new NotSupportedException("Add"); }
        public virtual void Clear() { throw new NotSupportedException("Clear"); }
        public virtual bool Contains(object o) { throw new NotSupportedException("Contains"); }
        public virtual int IndexOf(object o) { throw new NotSupportedException("IndexOf"); }
        public virtual void Insert(int index, object o) { throw new NotSupportedException("Insert"); }
        public virtual void Remove(object o) { throw new NotSupportedException("Remove"); }
        public virtual void RemoveAt(int index) { throw new NotSupportedException("RemoveAt"); }
        public virtual bool IsFixedSize { get { return true; } }
        public virtual bool IsReadOnly { get { return true; } }
        public virtual void CopyTo(System.Array array, int position)
        {
            for(int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], position+i);
            }
        }
    }

	/// <summary>
	/// A filtered view on a list that just reveals an
	/// interval portion of that list.
	/// </summary>
	public class IntervalFilteredList : ICollection
	{
        #region Fields
        int _start;
        int _end;
        IList _target;
        #endregion

        #region Constructor
		public IntervalFilteredList(int start, int end, IList target)
		{
            _start = start;
            _end = end;
            _target = target;
        }
        #endregion

        #region Methods
        public bool IsSynchronized { get { return _target.IsSynchronized; } }
        public int Count { get { return _end - _start; } }
        public void CopyTo(Array array, int index)
        {
            for(int i = 0; i < Count; i++)
            {
                array.SetValue(_target[_start+i], index+i);
            }
        }
        public object SyncRoot { get { return _target.SyncRoot; } }
        public IEnumerator GetEnumerator()
        {
            return new FilterEnumerator(this);
        }

        class FilterEnumerator : IEnumerator
        {
            int _cursor;
            IntervalFilteredList _list;
            public FilterEnumerator(IntervalFilteredList list)
            {
                _list = list;
                _cursor = -1;
            }
            public void Reset()
            {
                _cursor = -1;
            }
            public bool MoveNext()
            {
                _cursor++;
                return _cursor < _list.Count;
            }
            public object Current { get { return _list._target[_list._start + _cursor]; } }
        }
        #endregion
    }

    /// <summary>
    /// A filtered view on a list that uses a
    /// bit mask to determine membership in the view
    /// </summary>
    public class BitMaskFilteredList : ICollection
    {
        #region Fields
        bool _useInverse;
        IBitVector _mask;
        IList _target;
        #endregion

        #region Constructor
        public BitMaskFilteredList(IBitVector mask, IList target, bool useInverse)
        {
            _mask = mask;
            _target = target;
            _useInverse = useInverse;
        }
        #endregion

        #region Methods
        public bool IsSynchronized { get { return _target.IsSynchronized; } }
        public int Count { get { return _mask.OnCount; } }
        public void CopyTo(Array array, int index)
        {
            int i = index;
            IEnumerator cursor = GetEnumerator();
            cursor.Reset();
            while(cursor.MoveNext())
            {
                array.SetValue(cursor.Current, i++);
            }
        }
        public object SyncRoot { get { return _target.SyncRoot; } }
        public IEnumerator GetEnumerator()
        {
            return new FilterEnumerator(this);
        }

        class FilterEnumerator : IEnumerator
        {
            int _cursor;
            BitMaskFilteredList _list;
            public FilterEnumerator(BitMaskFilteredList list)
            {
                _list = list;
                _cursor = -1;
            }
            public void Reset()
            {
                _cursor = -1;
            }
            public bool MoveNext()
            {
                while(++_cursor < _list._target.Count && !(_list._useInverse ^ _list._mask[_cursor])) 
                {
                }
                return _cursor < _list._target.Count;
            }
            public object Current { get { return _list._target[_cursor]; } }
        }
        #endregion
    }
}
