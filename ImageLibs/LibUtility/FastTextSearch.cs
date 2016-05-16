using System;
using System.Collections;
using System.Runtime.Serialization;

namespace Dpu.Utility
{
    [Serializable]
    public class StringTooLongException : Exception
    {
        public StringTooLongException() : base() { }
        public StringTooLongException(string message, Exception e) : base(message, e) { }
        protected StringTooLongException(SerializationInfo info, StreamingContext context) : base(info, context) {}
        public StringTooLongException(string message) : base(message) { }
    }
        
        /// <summary>
    /// This class allows for the searching of a target string in a set of strings allowing for an 
    /// error rate of a specified number of characters.
    /// </summary>
    /// 
    public class FastTextSearcher : IComparer
    {
        // This is an implentation of the basic algorithm described in 
        // "Fast Text Searching With Errors" by S. Wu and U. Manber. TR 91-11 U. of Arizona
        #region Support Classes
        
        #endregion

        #region Fields
        string[]   m_TextToSearch;
        UInt32[]   m_BitFlags;
        bool       m_CaseSensitive;
        bool       m_FullTargetMatch;
        int        m_MinLen = int.MaxValue;
        int        m_MaxLen = int.MinValue;
        System.Text.ASCIIEncoding m_asciiEncoder;
        #endregion

        #region Properties
        public string[] StringsToSearch
        {
            get { return m_TextToSearch; }
        }


        /// <summary>
        /// Indicates whether character comparisons will be case sensitive or not.
        /// </summary>
        /// <value></value>
        public bool CaseSensitive
        {
            get { return m_CaseSensitive; }
            set { m_CaseSensitive = value; }
        }

        /// <summary>
        /// True = The entire target string must match within the number of errors.
        /// False = Any portion of the target string can match within the number of errors.
        /// </summary>
        /// <value></value>
        public bool FullTargetMatch
        {
            get { return m_FullTargetMatch; }
            set { m_FullTargetMatch = value; }
        }
        #endregion

        #region Methods
        public FastTextSearcher(string[] TextToSearch)
        {
            m_TextToSearch = TextToSearch;
            m_BitFlags = new UInt32[256];
            m_CaseSensitive = true;
            m_FullTargetMatch = true;
            m_asciiEncoder = new System.Text.ASCIIEncoding();
            foreach (string s in m_TextToSearch)
            {
                if (s.Length < m_MinLen) m_MinLen = s.Length;
                if (s.Length > m_MaxLen) m_MaxLen = s.Length;
            }
        }


        /// <summary>
        /// Set the set of strings to search.
        /// </summary>
        /// <param name="strings"></param>
        public void SetStringsToSearch(string[] strings) 
        {
            m_TextToSearch = strings;
            m_MinLen = int.MaxValue;
            m_MaxLen = int.MinValue;
            foreach (string s in m_TextToSearch)
            {
                if (s.Length < m_MinLen) m_MinLen = s.Length;
                if (s.Length > m_MaxLen) m_MaxLen = s.Length;
            }
        }


        /// <summary>
        /// Returns all the matches of textToMatch in our input set allowing for at most numberOfErrorsAllowed.
        /// </summary>
        /// <param name="textToMatch">The string to search for</param>
        /// <param name="numberOfErrorsAllowed">The maximum number of character errors allowed</param>
        /// <returns>A list of all the matching strings and their error rate, sorted by ascending error, ascending string.
        /// The Key of the DictionaryEntry is the string found and the Value is an int indicating the number of errors.
        /// </returns>
        public DictionaryEntry[] BestMatches(string textToMatch, int numberOfErrorsAllowed)
        {
            int TextLen = textToMatch.Length;
            byte[] BytesToMatch = m_asciiEncoder.GetBytes(textToMatch);

            if (TextLen == 1 ||
                m_MinLen - TextLen > numberOfErrorsAllowed ||
                (m_FullTargetMatch && (TextLen - m_MaxLen > numberOfErrorsAllowed)))
            {
                return new DictionaryEntry[]{};
            }

            if (TextLen > 32)
            {
                throw new StringTooLongException("The string to match must be 32 characters or less.");
            }

            UInt32[] MatchPatterns = new UInt32[numberOfErrorsAllowed + 1];
            Hashtable hash = new Hashtable();
            UInt32 Objective = (uint)0x80000000 >> (TextLen - 1);  // When this bit is set, there is a match.

            //Set the bit flags matrix
			for(int i=0; i<m_BitFlags.Length; ++i)
			{
				m_BitFlags[i]=0;
			}
            UInt32 ToSet = 0x80000000;

            for (int i = 0; i < TextLen; ++i)
            {
                char c = (char)BytesToMatch[i];
                if (m_CaseSensitive == false)
                {
                    c = char.ToLower(c, System.Globalization.CultureInfo.InvariantCulture);
                }
                m_BitFlags[(int)c] |= ToSet;
                ToSet >>= 1;
            }

            if (m_FullTargetMatch == true)
            {
                UInt32[] Ones = new UInt32[numberOfErrorsAllowed + 1];

                for (int i = 0; i < m_TextToSearch.Length; ++i)
                {
					// Ones represents how many ones we will shift into the left side for each error state.
                    Ones[0] = 0x80000000;
                    for (int j = 1; j <= numberOfErrorsAllowed; ++j)
                    {
                        Ones[j] = (Ones[j - 1] >> 1) | 0x80000000;
                    }

                    // If there is a difference of characters in excess of our number of errors allowed there cannot be a match
                    int LenDiff = Math.Abs(m_TextToSearch[i].Length - TextLen);
                    if (LenDiff <= numberOfErrorsAllowed)
                    {
                        int MinErrors = 0;
                        InitializeMatchMatrix(MatchPatterns);
                        for (int j = 0; j < m_TextToSearch[i].Length; ++j)
                        {
                            //Compute the values for the current character.
                            char c = m_TextToSearch[i][j];

                            if (m_CaseSensitive == false)
                            {
                                c = char.ToLower(c, System.Globalization.CultureInfo.InvariantCulture);
                            }

                            UInt32 CharPattern = m_BitFlags[(int)c];

                            // Set the exact match bits MatchPatterns[MinErrors]
                            UInt32 PreviousPattern = MatchPatterns[MinErrors];

                            MatchPatterns[MinErrors] = (MatchPatterns[MinErrors] >> 1) | (0x80000000 & Ones[MinErrors]);
                            Ones[MinErrors] <<= 1;
                            MatchPatterns[MinErrors] &= CharPattern;
                            for (int k = MinErrors + 1; k < MatchPatterns.Length; ++k)
                            {
                                UInt32 NewValue = (MatchPatterns[k] >> 1) | (0x80000000 & Ones[k]);
                                NewValue &= CharPattern;
                                Ones[k] <<= 1;
                                NewValue |= ((MatchPatterns[k - 1] | PreviousPattern) >> 1) | (0x80000000 & Ones[k]);
                                NewValue |= PreviousPattern;
                                PreviousPattern = MatchPatterns[k];
                                MatchPatterns[k] = NewValue;
                            }

                            if (MatchPatterns[MinErrors] == 0)
                            {
                                ++MinErrors;
                                if (MinErrors > numberOfErrorsAllowed) break;
                            }
                        }

                        // Now we look to see how good of a match we have.  If there is a word length difference, 
						//  these will always be errors in the forms of insertions of deletions.
                        if (MinErrors <= numberOfErrorsAllowed)
                        {
                            for (int j = 0; j < MatchPatterns.Length; ++j)
                            {
                                if ((MatchPatterns[j] & Objective) > 0)
                                {
                                    hash[m_TextToSearch[i]] = j;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // DEVNOTE: This code is a near duplicate of the true case above. It was duplicated for performance.
                for (int i = 0; i < m_TextToSearch.Length; ++i)
                {
                    InitializeMatchMatrix(MatchPatterns);
                    int MinErrorFound = MatchPatterns.Length;
                    for (int j = 0; j < m_TextToSearch[i].Length; ++j)
                    {
                        //Compute the values for the current character.
                        char c = m_TextToSearch[i][j];
                        if (m_CaseSensitive == false)
                        {
                            c = char.ToLower(c, System.Globalization.CultureInfo.InvariantCulture);
                        }

                        UInt32 CharPattern = m_BitFlags[(int)c];

                        // Set the exact match bits MatchPatterns[0]
                        UInt32 PreviousPattern = MatchPatterns[0];
                        MatchPatterns[0] = (MatchPatterns[0] >> 1) | 0x80000000;
                        MatchPatterns[0] &= CharPattern;
                        if ((MatchPatterns[0] & Objective) > 0)
                        {
                            hash[m_TextToSearch[i]] = 0;
                            break;  // Since we can't get any better than an exact sub-string match, stop.
                        }

                        for (int k = 1; k < MinErrorFound; ++k)
                        {
                            UInt32 NewValue = (MatchPatterns[k] >> 1) | 0x80000000;
                            NewValue &= CharPattern;
                            NewValue |= ((MatchPatterns[k - 1] | PreviousPattern) >> 1) | 0x80000000;
                            NewValue |= PreviousPattern;

                            PreviousPattern = MatchPatterns[k];
                            MatchPatterns[k] = NewValue;

                            if ((NewValue & Objective) > 0)
                            {
                                if (hash.ContainsKey(m_TextToSearch[i]) == false || k < (int)hash[m_TextToSearch[i]])
                                {
                                    hash[m_TextToSearch[i]] = k;
                                    MinErrorFound = k;  // We don't care about matches with more errors, so stop computing them.
                                }
                            }
                        }
                    }
                }
            }

            DictionaryEntry[] ret = new DictionaryEntry[hash.Count];
            IDictionaryEnumerator enumerator = hash.GetEnumerator();

            for (int i = 0; enumerator.MoveNext(); ++i)
            {
                ret[i] = (DictionaryEntry)enumerator.Current;
            }

            Array.Sort(ret, this);

            return ret;
        }

        public double BestScore(string textToMatch, int numberOfErrorsAllowed)
        {
            DictionaryEntry[] matches;

            try
            {
                matches = this.BestMatches(textToMatch, numberOfErrorsAllowed);
            }
            catch (StringTooLongException)
            {
                matches = new DictionaryEntry[]{};
            }

            double ret = 0.0;
            if(matches.Length > 0)
            {
                ret = MatchToScore(textToMatch.Length, textToMatch.Length - (int)matches[0].Value);
            }
            return ret;
        }

        public double MatchToScore(int numCharacters, int numMatches)
        {
            double val = (double)numMatches / numCharacters;
            if (val < .5) return 0.0;
            return 10000.0 + 30000.0 * Math.Log10(val);
        }

        void InitializeMatchMatrix(UInt32[] MatchPatterns)
        {
            MatchPatterns[0] = 0;
            UInt32 ToSet = 0x80000000;
            for (int i = 1; i < MatchPatterns.Length; ++i)
            {
                MatchPatterns[i] = ToSet;
                ToSet >>= 1;
                ToSet |= 0x80000000;
            }
        }
        #endregion

        #region IComparer Members

        public int Compare(object x, object y)
        {
            int ret = 0;
            DictionaryEntry X = (DictionaryEntry)x;
            DictionaryEntry Y = (DictionaryEntry)y;

            ret = (int)X.Value - (int)Y.Value;
            if (ret == 0)
            {
                ret = ((string)X.Key).CompareTo((string)Y.Key);
            }

            return ret;
        }

        #endregion
    }
}