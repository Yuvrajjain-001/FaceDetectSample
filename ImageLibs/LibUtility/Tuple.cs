using System;
using System.Collections;

namespace Dpu.Utility
{
    /// <summary>
    /// An ordered set of elements with overridden hashing,
    /// equality, and printing.
    /// </summary>
    public class Tuple : IEnumerable 
    {
        #region Constructors
        public Tuple(params object[] objs)
        {
            this._objs = objs;
        }
        #endregion

        #region Fields
        private object[] _objs;
        #endregion

        #region Properties
        public object this[int i] { get { return _objs[i]; } }
        public int Count { get { return _objs.Length; } }
        #endregion

        #region Methods
        /// <summary>
        /// Deep equals on the elements of the tuple
        /// </summary>
        public override bool Equals(object obj)
        {
            Tuple tuple = obj as Tuple;
            if(tuple == null)
            {
                return false;
            }
            if(_objs.Length != tuple._objs.Length)
            {
                return false;
            }
            for(int i = 0; i < _objs.Length; i++)
            {
                if(!_objs[i].Equals(tuple._objs[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Deep hash on the elemenets
        /// </summary>
        public override int GetHashCode()
        {
            int x = 0;
            for(int i = 0; i < _objs.Length; i++)
            {
                x ^= (13*i + 2) * _objs[i].GetHashCode();
            }
            return x;
        }

        /// <summary>
        /// Print comma-separated tuple
        /// </summary>
        public override string ToString()
        {
            return ToString(", ");
        }

        /// <summary>
        /// Print tuple separated by the given separator,
        /// e.g. the tuple (1, 2, 3) printed with " | " will yield
        /// "1 | 2 | 3".
        /// </summary>
        public string ToString(string separator)
        {
            return LogUtils.PrintCollection(_objs, separator);
        }

        /// <summary>
        /// Enumerate over the elements in order
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return _objs.GetEnumerator();
        }
        #endregion
    }
}
