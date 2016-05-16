using System;
using System.Collections;

namespace Dpu.Utility
{
    /// <summary>
    /// A hashtable with deep-equals & deep-hash
    /// </summary>
    public class HashSet : Hashtable
    {
        public HashSet() : base() {}
        public HashSet(int capacity) : base(capacity) {}
        public HashSet(int capacity, float loadFactor) : base(capacity, loadFactor) {}
        public override int GetHashCode()
        {
            int hash = 0;
            int index = 1;
            foreach(object val in this.Values)
            {
                hash ^= (13*index)*val.GetHashCode();
            }
            return hash;
        }
        public override bool Equals(object obj)
        {
            HashSet h = obj as HashSet;
            if(h == null) return false;

            if(this.Keys.Count != h.Keys.Count) return false;
            foreach(object key in this.Keys)
            {
                object val = this[key];
                if(val == null && !h.ContainsKey(key))
                    return false;
                object val1 = h[key];
                if(!val.Equals(val1)) { return false; }
            }
            return true;
        }
    }
}
