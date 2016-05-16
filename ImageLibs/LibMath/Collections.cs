// <copyright file="Collections.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;

namespace System.Windows.Ink.Analysis.MathLibrary
{
    #region ArrayBase
    public abstract class ArrayBase
    {
        protected int _count; // Automatically gets initialized to 0
        // Derived class must override the following methods and properties
        protected abstract Array CurrentArray
        {
            get;
            set;
        }
        protected abstract Array NewBuffer(int count);
        public ArrayBase() { }
        // Returns the number of elements in the array
        public int Count
        {
            get { return _count; }
        }
        // Returns true if the array is empty
        public bool Empty
        {
            get { return (0 == _count); }
        }

        // Returns the capacity of the array
        public int Capacity
        {
            get
            {
                return (null != this.CurrentArray) ? this.CurrentArray.Length : 0;
            }
            set
            {
                if (value < _count)
                    throw new ArgumentOutOfRangeException("value");

                ReAlloc(value);
            }
        }

        /**
         * ToArray returns a new Object array containing the contents of the ArrayList.
         * This requires copying the ArrayList, which is an O(n) operation.
         */
        public Array ToArray()
        {
            Array dest = NewBuffer(_count);
            Array.Copy(this.CurrentArray, 0, dest, 0, _count);
            return dest;
        }

//        public void Sort(int index, int count, IComparer comparer) 
//        {
//            if ((0 > index) || (0 > count))
//                throw new ArgumentOutOfRangeException((0 > index) ? "index" : "count");
//            if (_count - index < count)
//                throw new ArgumentOutOfRangeException("index");
//            Array.Sort(this.CurrentArray, index, count, comparer);
//        }

        public void Sort() 
        {
            Array.Sort(this.CurrentArray, 0, _count, Comparer.Default);
        }

        // Throws OutOfMemoryException if running out of memory
        protected virtual void ManageMemory(int nNewCount, bool bFreeExtra)
        {
            // Grow the array by 50% or total element count
            if (this.Capacity < nNewCount)
            {
                int nNewCapacity = this.Capacity + (this.Capacity / 2);
                if (nNewCapacity > nNewCount)
                {
                    nNewCount = nNewCapacity;
                }
                ReAlloc(nNewCount);
            }
            else if (bFreeExtra && (nNewCount < (this.Count / 4))) 
            {
                // Deallocate if the size drops below 25%
                nNewCount = (0 < nNewCount) ? (this.Count / 4) : 0;
                ReAlloc(nNewCount);
            }
        }
        protected void ManageMemory(int nNewCount)
        {
            ManageMemory(nNewCount, false);
        }
        // Throws OutOfMemoryException if running out of memory
        protected void ReAlloc(int nNewCount)
        {
            // If the array length is exactly the same, no need to reallocate
            if ((null == this.CurrentArray) || (this.Capacity != nNewCount))
            {
                // Caller requested to de/allocate packets
                if (0 < nNewCount)
                {
                    int nCountToCopy = (nNewCount < _count) ? nNewCount : _count;
                    Array aT = NewBuffer(nNewCount);

                    if (0 < nCountToCopy)
                    {
                        Array.Copy(this.CurrentArray, aT, nCountToCopy);
                    }
                    // Release the old arrays
                    this.CurrentArray = aT;
                }
                else
                {
                    //ASSERT(0 == m_cPacketCount);
                    this.CurrentArray = null;
                }
            }
        }
        protected void InsertSpaceForItems(int index, int count)
        {
            if ((0 > index) || (0 > count))
                throw new ArgumentOutOfRangeException((0 > index) ? "index" : "count");

            ManageMemory(_count + count);
            if (index < _count)
            {
                // Destination index
                int iItem = _count + count - 1;

                // Destination packet index should stop moving data here
                int iEndItem = index + count;

                // Where the data should be copied from 
                int iSrcItem = _count - 1;
                Array arr = this.CurrentArray;

                // Copy the data
                for (; iItem >= iEndItem; --iItem, --iSrcItem)
                    arr.SetValue(arr.GetValue(iSrcItem), iItem);
            }

            _count += count;
        }

        public void RemoveAt(int index, int count)
        {
            if (((index + count) > _count) || (0 > index) || (0 > count))
                throw new ArgumentOutOfRangeException((0 > index) ? "index" : "count");

            if ((index + count) < _count)
            {
                Array arr = this.CurrentArray;

                // Copy the data
                int iEndItem = _count - count;
                int iSrcItem = index + count;

                for (int iItem = index; iItem < iEndItem; ++iItem, ++iSrcItem)
                    arr.SetValue(arr.GetValue(iSrcItem), iItem);
            }

            ManageMemory(_count - count);
            _count -= count;
        }

        public void RemoveAt(int index)
        {
            RemoveAt(index, 1);
        }

        // Removes all entries from the vector
        public void Clear()
        {
            _count = 0;
            ManageMemory(_count);
        }

        protected void AddRange(ArrayBase src)
        {
            this.ManageMemory(_count + src.Count);
            Array.Copy(src.CurrentArray, 0, this.CurrentArray, _count, src.Count);
            _count += src.Count;
        }
    };
    #endregion
    
    #region DoubleArray
    /// <summary>
    /// 
    /// </summary>
    public class DoubleArray: ArrayBase
    {
        private double[] _array;
        protected override Array CurrentArray
        {
            get { return _array; }
            set { _array = (double[])value; }
        }
        
        protected override Array NewBuffer(int nNewCount)
        {
            return (Array)(new double[nNewCount]);
        }

        public DoubleArray()
            :this(16)
        {
        }

        public DoubleArray(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            _array = new double[capacity];
        }

        public void Add(double val)
        {
            Insert(this.Count, val); 
        }

        public void AddRange(DoubleArray source)
        {
            base.AddRange(source);
        }

        public void Insert(int index, double val)
        {
            InsertSpaceForItems(index, 1);
            _array[index] = val;
        }

        public double this[int index]
        {
            get
            {
                if (index > this.Count)
                    throw new ArgumentOutOfRangeException("index");

                return _array[index];
            }
//#if INTERNAL_DPU
            set
            {
                if (index > this.Count)
                    throw new ArgumentOutOfRangeException("index");

                _array[index] = value;
            }
//#endif // INTERNAL_DPU
        }
    };
    #endregion
    
    #region IntArray
    public class IntArray: ArrayBase
    {
        private int[] _array;
        private bool fSorted;
        
        protected override Array CurrentArray
        {
            get { return _array; }
            set { _array = (int[])value; }
        }
        
        protected override Array NewBuffer(int nNewCount)
        {
            return (Array)(new int[nNewCount]);
        }
        
        public IntArray()
            :this(16)
        {
        }

        public IntArray(bool isSorted)
            :this(16)
        {
            fSorted = isSorted;
        }

        public IntArray(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            _array = new int[capacity];
        }

        public IntArray(int capacity, bool isSorted)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            _array = new int[capacity];
            fSorted = isSorted;
        }

        public IntArray(IntArray originalArray)
        {
            if (originalArray == null)
            {
                throw new ArgumentException("originalArray");
            }
            _array = ( int [ ] )originalArray.ToArray();
            _count = originalArray.Count;
        }

        public IntArray(int [] originalArray)
        {
            if (originalArray == null)
            {
                throw new ArgumentException("originalArray");
            }
            _array = new int[originalArray.Length];
            originalArray.CopyTo(_array, 0);
            _count = originalArray.Length;
        }

        public IntArray(int [] originalArray, bool isSorted):
            this(originalArray)
        {
            fSorted = isSorted;
            Array.Sort(_array, 0, _count, Comparer.Default);
        }

        public void Add(int val)
        {
            if (fSorted)
            {
                AddSort(val, false);
            }
            else
            {
                Insert(this.Count, val); 
            }
        }

        public void AddRange(IntArray arrayToAdd)
        {
            if (arrayToAdd == null)
            {
                throw new ArgumentException("arrayToAdd");
            }
            int index = this.Count;
            InsertSpaceForItems(index, arrayToAdd.Count);
            Array.Copy(arrayToAdd._array, 0, _array, index, arrayToAdd.Count);

            if (fSorted)
            {
                Sort( );
            }
        }

        public void AddRange(int [] arrayToAdd)
        {
            if (arrayToAdd == null)
            {
                throw new ArgumentException("arrayToAdd");
            }
            int index = this.Count;
            InsertSpaceForItems(index, arrayToAdd.Length);
            Array.Copy(arrayToAdd, 0, _array, index, arrayToAdd.Length);

            if (fSorted)
            {
                Sort( );
            }
        }



        public int IndexOf(int val)
        {
            for(int i = 0; i < this.Count; ++i)
            {
                if(val == _array[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public bool ContainsSort(int val)
        {
            // Get the insertion point
            int insIndex = FindInsertionIndex(val);
            // Is it pointing to the element?
            return IsExactMatch(val, insIndex);
        }

        public bool Contains(int val)
        {
            if (fSorted)
            {
                return ContainsSort(val);
            }
            else
            {
                return IndexOf(val) != -1;
            }
        }

        protected int FindInsertionIndex(int val)
        {
            if (this.Empty || val < this[0])
            {
                return 0;
            }

            int index = this.Count;
            int nFloor = 0;
            int nMid = (this.Count - 1);

            do
            {
                if (val >= this[nMid])
                    nFloor = nMid;
                else
                    index = nMid;

                nMid = (nFloor + index) / 2;
            } while (nMid > nFloor);
            return index;
        }


        public bool RemoveIfExist(int val)
        {
            if (fSorted)
            {
                // Get the insertion point
                int insIndex = FindInsertionIndex(val);
                // Is it pointing to the element?
                if (!this.Empty && IsExactMatch(val, insIndex))
                {
                    RemoveAt((0 < insIndex) ? insIndex - 1 : insIndex);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                int pos = IndexOf(val);
                if (pos != -1)
                {
                    this.RemoveAt(pos, 1);
                    return true;
                }
                return false;
            }
        }

        public void Remove(int val)
        {
            RemoveIfExist(val);
        }

        protected bool IsExactMatch(int val, int insIndex)
        {
            int testIndex = (0 < insIndex) ? insIndex - 1 : insIndex;
            return this.Empty ? false : (val == this[testIndex]);
        }

        public int AddSort(int val, bool allowDuplicates)
        {
            int index = FindInsertionIndex(val);
            if (allowDuplicates || !IsExactMatch(val, index))
            {
                Insert(index, val);
            }
            return index;
        }

        public int BinarySearch(int val)
        {
            int insIndex = FindInsertionIndex(val);
            return this.IsExactMatch(val, insIndex) ? (insIndex - 1) : -1;
        }

        public void Insert(int index, int val)
        {
            InsertSpaceForItems(index, 1);
            _array[index] = val;
        }
        
        public int this[int index]
        {
            get
            {
                if ((0 > index) || (index >= this.Count))
                    throw new ArgumentOutOfRangeException("index");

                return _array[index];
            }
//            set
//            {
//                if ((0 > index) || (index >= this.Count))
//                    throw new ArgumentOutOfRangeException("index");
//
//                _array[index] = value;
//            }
        }
    };
    #endregion

    #region GuidArray
    /// <summary>
    /// 
    /// </summary>
    public class GuidArray: ArrayBase
    {
        private Guid[] _array;
        protected sealed override Array CurrentArray
        {
            get { return _array; }
            set { _array = (Guid[])value; }
        }
        
        protected override Array NewBuffer(int nNewCount)
        {
            return (Array)(new Guid[nNewCount]);
        }

        public GuidArray()
            :this(16)
        {
        }

        public GuidArray(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            _array = new Guid[capacity];
        }

        public void Add(Guid r)
        {
            Insert(this.Count, r); 
        }

        public void Insert(int index, Guid r)
        {
            InsertSpaceForItems(index, 1);
            _array[index] = r;
        }

        public int IndexOf(Guid r)
        {
            for (int ii = 0; ii < this.Count; ii++)
            {
                if (r == this._array[ ii ])
                {
                    return ii;
                }
            }
            return -1;
        }

        public bool Contains(Guid r)
        {
            if ( -1 == IndexOf ( r ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Guid this[int index]
        {
            get
            {
                if (index > this.Count)
                    throw new ArgumentOutOfRangeException("index");

                return _array[index];
            }
//            set
//            {
//                if (index > this.Count)
//                    throw new ArgumentOutOfRangeException("index");
//
//                _array[index] = value;
//            }
        }
    };
    #endregion

}
