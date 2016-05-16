using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Dpu.Utility
{
    public interface IReadBitVector
    {
        /// <summary>
        /// Count of non-zero bits.
        /// </summary>
        int OnCount 
        {
            get;
        }

        /// <summary>
        /// Whether or not the i'th bit is set
        /// </summary>
        bool this[int i] { get; }

        /// <summary>
        /// Number of bits
        /// </summary>
        int Length 
        {
            get;
        }
    }

    public interface IBitVector : IReadBitVector, ICloneable, IEnumerable 
    {
        /// <summary>
        /// Whether or not the i'th bit is set
        /// </summary>
        new bool this[int i] { get; set; }
        
        /// <summary>
        /// Invert all the bits (SIdE EFFECT)
        /// </summary>
        /// <returns></returns>
        void Inverse();

        /// <summary>
        /// Take the inverse of this bit vector in the context of the given one. (SIdE EFFECT)
        /// Assume THIS is a subset of V.
        /// Essentially:   not(this) & v
        /// </summary>
        void InverseIn(IBitVector v);

        /// <summary>
        /// Construct a new bit vector which is:  this & v  (SIdE EFFECT)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        void And(IBitVector v);

        /// <summary>
        /// Index of the first non-zero bit. (CURRENTLY LINEAR TIME!)
        /// </summary>
        int FirstTrue();
    }


    /// <summary>
	/// An alternative bit vector implementation
	/// that provides some accelerated operations
	/// useful in parsing.
	/// </summary>
	[Serializable]
	public class CustomBitVector : IBitVector 
	{
        int _length;
        int _onCount;
        ulong[] _bits;

        public void Clear() 
        {
            Array.Clear(_bits, 0, _bits.Length);
        }

        public CustomBitVector(int length)
        {
            int numBuckets = 1 + ((length-1)/64);
            _bits = new ulong[numBuckets];
            _length = length;
            _onCount = -1;
        }

        public CustomBitVector(CustomBitVector other)
        {
            _bits = new ulong[other._bits.Length];
            Array.Copy(other._bits, _bits, _bits.Length);
            _length = other._length;
            _onCount = other._onCount;
        }

        public object Clone()
        {
            return new CustomBitVector(this);
        }

        /// <summary>
        /// Count of non-zero bits.
        /// </summary>
        public int OnCount 
        {
            get 
            { 
                if(_onCount < 0)
                {
                    _onCount = CountBits(); 
                }
                return _onCount;
            }
        }

        /// <summary>
        /// FIXME: Inefficient
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new BitVectorEnumerator(this);
        }

        /// <summary>
        /// Count the bits; linear in number of bits
        /// </summary>
        private int CountBits()
        {
            ulong onCount = 0;
            int numBuckets = NumBuckets;
            for(int i = 0; i < numBuckets; i++)
            {
                ulong x = _bits[i];
                int stop = (i == numBuckets-1 ? _length%64 : 64);
                for(int j = 0; j < stop; j++)
                {
                    onCount += ((x & (1UL << j)) >> j);
                }
            }
            return (int)onCount;
        }

        
        /// <summary>
        /// Number of bits
        /// </summary>
        public int Length 
        {
            get { return _length; }
        }

        int NumBuckets { get { return (_length-1)/64 + 1; } }

        /// <summary>
        /// Invert all the bits
        /// </summary>
        public void Inverse()
        {
            //FIXME: special case for len=0 ???
            _onCount = (_onCount == -1 ? _onCount : _length - _onCount);
            int numBuckets = NumBuckets;
            for(int i = 0; i < numBuckets; i++)
            {
                _bits[i] = ~_bits[i];
            }
            //mask out the end of the vector
            ulong mask1 = (1UL<<(_length%64)-1);
            ulong mask = ~(0xFFFFFFFFFFFFFFFF << (_length%64));
            _bits[numBuckets-1] &= mask;
        }

        /// <summary>
        /// Copy the contents of v into this
        /// </summary>
        public void CopyIn(IBitVector v)
        {
            CustomBitVector cv = (CustomBitVector)v;
            Debug.Assert(_length == cv._length);
            Array.Copy(cv._bits, _bits, cv._bits.Length);
            _onCount = cv._onCount;
        }

        /// <summary>
        /// Take the inverse of this bit vector in the context of the given one. (SIdE EFFECT)
        /// Assume THIS is a subset of V.
        /// Essentially:   not(this) & v
        /// </summary>
        public void InverseIn(IBitVector v)
        {
            CustomBitVector cv = (CustomBitVector)v;
            int numBuckets = NumBuckets;
            for(int i = 0; i < numBuckets; i++)
            {
                _bits[i] = ~_bits[i] & cv._bits[i];
            }
            //mask out the end of the vector
            ulong mask = ~(0xFFFFFFFFFFFFFFFF << (_length%64));
            _bits[numBuckets-1] &= mask;
            if(_onCount != -1)
            {
                _onCount = v.OnCount - _onCount;
            }
        }

        /// <summary>
        /// Construct a new bit vector which is:  this & v  (SIDE EFFECT)
        /// </summary>
        public void And(IBitVector v)
        {
            CustomBitVector cv = (CustomBitVector)v;
            int numBuckets = NumBuckets;
            for(int i = 0; i < numBuckets; i++)
            {
                _bits[i] &= cv._bits[i];
            }
            //mask out the end of the vector
            //FIXME: this is broken: _bits[numBuckets-1] &= (1UL<<(_length%64)-1);
            ulong mask = ~(0xFFFFFFFFFFFFFFFF << (_length%64));
            _bits[numBuckets-1] &= mask;

            _onCount = -1;
        }

        /// <summary>
        /// Construct a new bit vector which is:  this | v  (SIDE EFFECT)
        /// </summary>
        public void Or(IBitVector v)
        {
            CustomBitVector cv = (CustomBitVector)v;
            int numBuckets = NumBuckets;
            for(int i = 0; i < numBuckets; i++)
            {
                _bits[i] |= cv._bits[i];
            }
            //mask out the end of the vector
            //_bits[numBuckets-1] &= (1UL<<(_length%64)-1);

            ulong mask = ~(0xFFFFFFFFFFFFFFFF << (_length%64));
            _bits[numBuckets-1] &= mask;

            _onCount = -1;
        }


        public override int GetHashCode()
        {
            ulong hash = 0;
            for(int i = 0; i < _bits.Length; i++)
            {
                hash = hash ^ (_bits[i] << i);
            }
            hash = ((hash >> 32) ^ hash) & 0x0000000FFFFFFFF;
            return (int)hash;
        }

        public override bool Equals(object obj)
        {
            CustomBitVector v2 = (obj as CustomBitVector);
            if(v2 == null || _length != v2._length || //FIXME: default to FALSE?
                ((_onCount >= 0) && (v2._onCount >= 0) && (_onCount != v2._onCount))) return false;
            for(int i = 0; i < _bits.Length; i++)
            {
                if(_bits[i] != v2._bits[i]) return false;
            }
            return true;
        }

        public int FirstTrue()
        {
            int numBuckets = NumBuckets;
            for(int i = 0; i < numBuckets; i++)
            {
                ulong x = _bits[i];
                int stop = (i == numBuckets-1 ? _length%64 : 64);
                for(int j = 0; j < stop; j++)
                {
                    if((x & (1UL << j)) != 0)
                    {
                        return i*64+j;
                    }
                }
            }
            return -1;
        }

        public bool this[int index]
        {
            get 
            {
                if(index < _length) 
                {
                    return (_bits[index/64] & (1UL<<(index%64))) != 0;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                _length = Math.Max(_length, index);
                int bucketIndex = index/64;
                int bitIndex = index%64;
                if(bucketIndex >= _bits.Length)
                {
                    int newLen = Math.Max(bucketIndex+10, 2*_bits.Length);
                    ulong[] newBits = new ulong[newLen];
                    Array.Clear(newBits, 0, newLen);
                    Array.Copy(_bits, 0, newBits, 0, _bits.Length);
                }
                bool prevSet = ((_bits[bucketIndex] & (1UL << bitIndex)) != 0);
                if(value)
                {
                    if(!prevSet && _onCount != -1) _onCount++;
                    _bits[bucketIndex] |= (1UL << bitIndex);
                }
                else
                {
                    if(prevSet && _onCount != -1) _onCount--;
                    _bits[bucketIndex] &= ~(1UL << bitIndex);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.OnCount*3);
            sb.Append("<");
            for(int i = 0; i < Length; i++)
            {
                if(this[i])
                {
                    sb.Append(i.ToString());
                    sb.Append(" ");
                }
            }
            if(sb.Length == 1)
            {
                sb.Append('>');
            }
            else
            {
                sb[sb.Length-1] = '>';
            }

            return sb.ToString();
        }
	}

    
    /// <summary>
    /// A bit vector that uses the System.Collections
    /// implementation underlying.
    /// </summary>
    public struct BigBitVector : IBitVector
    {
        private BitArray _bits;
        private int      _count;
        
        /// <summary>
        /// Count of non-zero bits.
        /// </summary>
        public int OnCount 
        { 
            get 
            { 
                if (_count == -1)
                    _count = CountBits();
                return _count; 
            }
        }

        /// <summary>
        /// Number of bits
        /// </summary>
        public int Length 
        {
            get { return _bits.Length; }
        }

        public BigBitVector (BitArray ba)
        {
            _bits      = ba;
            _count     = -1;
        }

        public BigBitVector (BitArray ba, int count)
        {
            _bits      = ba;
            _count     = count;
            Debug.Assert(OnCount == CountBits());  
        }

        public BigBitVector (int length)
        {
            _bits      = new BitArray(length);
            _count     = 0;
        }

        /// <summary>
        /// FIXME: Inefficient
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new BitVectorEnumerator(this);
        }


        /// <summary>
        /// Invert all the bits (SIdE EFFECT)
        /// </summary>
        /// <returns></returns>
        public void Inverse()
        {
            // Not() side effects!
            _bits.Not();
            _count = (_count == -1 ? _count : Length - _count);
            Debug.Assert(OnCount == CountBits());
        }

        public void InverseIn(IBitVector v)
        {
            InverseIn((BigBitVector)v);
        }

        public void And(IBitVector v)
        {
            And((BigBitVector)v);
        }

        /// <summary>
        /// Take the inverse of this bit vector in the context of the given one. (SIdE EFFECT)
        /// Assume THIS is a subset of V.
        /// Essentially:   not(this) & v
        /// </summary>
        public void InverseIn(BigBitVector v)
        {
            _bits.Not().And(v._bits);
            _count = v.OnCount - OnCount;
            //Debug.Assert(Count == CountBits());
        }

        /// <summary>
        /// Construct a new bit vector which is:  this & v  (SIdE EFFECT)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public void And(BigBitVector v)
        {
            _bits.And(v._bits);
            _count = -1;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for(int i = 0; i < _bits.Length; i++)
            {
                hash = hash ^ (_bits[i] ? 0 : 1) << (i % 32);
            }
            return hash;
        }
        

        public override bool Equals(object obj)
        {
            BigBitVector v = (BigBitVector)obj;
            if(this.Length != v.Length || this.OnCount != v.OnCount)
            {
                return false;
            }
            for(int i = 0; i < Length; i++)
            { 
                if(_bits[i] != v._bits[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool this[int i]
        {
            get 
            {
                return _bits[i];
            }
            set
            {
                if (_count != -1) 
                {
                    if(value == true)
                    {
                        if (_bits[i] == false)
                            _count++;
                    }
                    else
                    {
                        if (_bits[i] == true)
                            _count--;
                    }
                }
                _bits[i] = value;

            }
        }


        public override string ToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for(int i = 0; i < this.Length; i++)
            {
                builder.Append(_bits[i]);
            }
            return builder.ToString();
        }

        public object Clone() 
        { 
            return new BigBitVector((BitArray)_bits.Clone(), OnCount);
        }

        /// <summary>
        /// Explicit computation of the number of nonzero bits (CURRENTLY LINEAR TIME!)
        /// </summary>
        private int CountBits()
        {
            int sum = 0;
            for(int i = 0; i < Length; i++)
                if (_bits[i] == true)
                    ++sum;
            return sum;
        }

        /// <summary>
        /// Index of the first non-zero bit.(CURRENTLY LINEAR TIME!)
        /// </summary>
        public int FirstTrue() 
        {
            for(int i = 0; i < Length; i++)
                if (_bits[i] == true)
                    return i;
            return Length;
        }
    }


    /// <summary>
    /// A packed bit vector.
    /// </summary>
    public class CompactBitVector : IBitVector
    {
        int    _length; // number of potential elements
        ulong  _bits;
        int    _count; // number of elements in the vector

        /// <summary>
        /// Length of the bit vector (number of valid bits).
        /// </summary>
        public int Length 
        {
            get { return _length; }
        }

        /// <summary>
        /// Count of non-zero bits.
        /// </summary>
        public int OnCount { get { return _count; } }

        /// <summary>
        /// Initial bit vector has all bits zero.
        /// </summary>
        /// <param name="_length"></param>
        public CompactBitVector(int length)
        {
            Debug.Assert(length <= 62);
            this._length = length;
            this._bits = 0;
            this._count = 0;
        }

        /// <summary>
        /// FIXME: Inefficient
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new BitVectorEnumerator(this);
        }

        private CompactBitVector(int length, ulong _bits, int _count)
        {
            this._length = length;
            this._bits = _bits;
            this._count = _count;
            // Check to see that we have it right.
            Debug.Assert(_count == CountBits());
        }

        public void Inverse()
        {
            // Invert but mask out the unused _bits.
            _bits = ~_bits & ((1UL<<_length)-1);
            _count = _length-_count;
        }

        public void InverseIn(IBitVector v)
        {
            InverseIn((CompactBitVector)v);
        }

        public void And(IBitVector v)
        {
            And((IBitVector)v);
        }

        /// <summary>
        /// Take the inverse of this bit vector in the context of the given one.
        /// Assume this bit vector is in the given one.
        /// </summary>
        public void InverseIn(CompactBitVector v)
        {
            // Flips _bits of this but only take those in v.
            // @@@@ Potential bug, the count of nonzero bits should be LE 
            // the number in v,  but this.count could be GE v.count.
            _bits = ~_bits & v._bits;
            _count = v._count-_count;
        }

        public CompactBitVector And(CompactBitVector v)
        {
            return new CompactBitVector(_length, ~_bits & ((1UL<<_length)-1), _length-_count);
        }

        /// <summary>
        /// Index of the first non-zero bit.  (CURRENTLY LINEAR TIME!) 
        /// </summary>
        public int FirstTrue()
        {
            // This isn't too fast
            for(int i = 0; i < _length; i++)
                if((_bits & (1UL<<i)) != 0)
                    return i;
            return -1;
        }

        /// <summary>
        /// Explicit computation of the number of nonzero bits (CURRENTLY LINEAR TIME!)
        /// </summary>
        public int CountBits()
        {
            int sum = 0;
            for(int i = 0; i < _length; i++)
                if((_bits & (1UL<<i)) != 0)
                    ++sum;
            return sum;
        }

        public override int GetHashCode()
        {
            return (int)(_bits >> 32) | (int)_bits * 7;
        }
        
        /// <summary>
        /// Get or set bit.  Keep count updated.
        /// </summary>
        public bool this[int i]
        {
            get 
            {
                return (_bits & (1UL << i)) != 0;
            }
            set
            {
                if(value)
                {
                    if((_bits & (1UL << i)) == 0) _count++;
                    _bits |= (1UL << i);
                }
                else
                {
                    if((_bits & (1UL << i)) != 0) _count--;
                    _bits &= ~(1UL << i);
                }
            }
        }

        public override bool Equals(object obj)
        {
            CompactBitVector v = (CompactBitVector)obj;
            return this._length == v._length && this._bits == v._bits;
        }

        public object Clone() { return new CompactBitVector(_length, _bits, _count); }
    }


    /// <summary>
    /// A hacked up bit vector enumerator for dense
    /// bit vector implementations
    /// </summary>
    class BitVectorEnumerator : IEnumerator
    {
        public BitVectorEnumerator(IBitVector v)
        {
            ArrayList onIds = new ArrayList(v.OnCount);
            for(int i = 0; i < v.Length; i++)
            {
                if(v[i]) { onIds.Add(i); }
            }
            _enum = onIds.GetEnumerator();
        }

        IEnumerator _enum;

        public void Reset()
        {
            _enum.Reset();
        }

        public object Current
        {
            get
            {
                return _enum.Current;
            }
        }

        public bool MoveNext()
        {
            return _enum.MoveNext();
        }
    }

    public class IntervalBitVector : IReadBitVector
    {
        #region Constructors
        public IntervalBitVector(int start, int end, int length)
        {
            Start = start;
            End = end;
            _length = length;
        }
        #endregion

        #region Properties
        public int OnCount { get { return End-Start; } }
        public bool this[int i] { get { return i >= Start && i < End; } }
        public int Length { get { return _length; } }
        public int Start;
        public int End;
        #endregion

        #region Fields
        private int _length;
        #endregion

        #region Methods
        public override int GetHashCode()
        {
            const int maxLen = 4096;
            return (Start + ((End * maxLen) % Int32.MaxValue));
        }

        public override bool Equals(object obj)
        {
            IntervalBitVector v = obj as IntervalBitVector;
            if(v == null) return false;
            return Start == v.Start && End == v.End;
        }
        #endregion
    }



    /// <summary>
    /// A sparse bit vector that is implemented as
    /// an arraylist of index Id's
    /// </summary>
    public class SparseBitVector : IBitVector
    {
        #region Constructors
        public SparseBitVector(int length)
        {
            _onBits = new ArrayList();
            _length = length;
        }
        public SparseBitVector(SparseBitVector vec)
        {
            _onBits = new ArrayList(vec._onBits);
            _length = vec._length;
        }
        #endregion

        #region Fields
        private int _length;
        private ArrayList _onBits;
        #endregion

        #region Properties
        public int OnCount { get { return _onBits.Count; } }
        public int Length { get { return _length; } }
        public bool this[int i]
        {
            get
            {
                int index = _onBits.BinarySearch(i);
                return index >= 0;
            }
            set
            {
                int index = _onBits.BinarySearch(i);
                if(!value && index >= 0)
                {
                    _onBits.RemoveAt(index);
                }
                else if(value && index < 0)
                {
                    _onBits.Insert(~index, i);
                }
            }
        }
        #endregion

        #region Methods
        public void Inverse()
        {
            ArrayList onBits = new ArrayList(_length-_onBits.Count);
            //FIXME: optimize this
            for(int i = 0; i < _length; i++)
            {
                if(!this[i])
                {
                    onBits.Add(i);
                }
            }
            _onBits = onBits;
        }

        public void InverseIn(IBitVector v)
        {
            SparseBitVector whole = (SparseBitVector)v;
            ArrayList onBits = new ArrayList(whole.OnCount-this.OnCount);
            foreach(int i in whole)
            {
                if(!this[i])
                {
                    onBits.Add(i);
                }
            }
        }

        public void And(IBitVector v)
        {
            SparseBitVector v1 = (SparseBitVector)v;
            ArrayList onBits = new ArrayList(_onBits.Count+v1._onBits.Count);
            int cursor0 = 0;
            int cursor1 = 0;
            while(cursor0 < _onBits.Count || cursor1 < v1._onBits.Count)
            {
                if(cursor0 >= _onBits.Count)
                {
                    while(cursor1 < v1._onBits.Count)
                    {
                        onBits.Add(v1._onBits[cursor1]);
                    }
                }
                else if(cursor1 >= v1._onBits.Count)
                {
                    while(cursor0 < this._onBits.Count)
                    {
                        onBits.Add(this._onBits[cursor0]);
                    }
                }
                else
                {
                    int i0 = (int)this._onBits[cursor0];
                    int i1 = (int)v1._onBits[cursor1];
                    if(i0 == i1)
                    {
                        onBits.Add(i0);
                        cursor0++;
                        cursor1++;
                    }
                    else if(i0 < i1)
                    {
                        onBits.Add(i0);
                        cursor0++;
                    }
                    else
                    {
                        onBits.Add(i1);
                        cursor1++;
                    }
                }
            }
        }

        public int FirstTrue()
        {
            return _onBits.Count == 0 ? -1 : (int)_onBits[0];
        }

        public object Clone()
        {
            return new SparseBitVector(this);
        }

        public IEnumerator GetEnumerator()
        {
            return _onBits.GetEnumerator();
        }
        #endregion
    }
}
