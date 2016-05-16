using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Dpu.Utility
{
	/// <summary>
	/// Summary description for TextExtract.
	/// </summary>
	public class TextExtract
	{
		public enum State {outside, inside};

        public class MyMatch
        {
            public int Begin;
            public int End;
            public bool IsStart;

            public MyMatch( int begin, int end, bool isStart )
            {
                Begin = begin;
                End = end;
                IsStart = isStart;
            }
        }

		public TextExtract()
		{
			//
			// TODO: Add constructor logic here
			//
		}

        public static ArrayList MatchingHelper( string begin, string end, string input, bool isSigned)
        {
            Regex regBegin = new Regex(begin);
            Regex regEnd = new Regex(end);

            MatchCollection beginList = regBegin.Matches(input);
            MatchCollection endList = regEnd.Matches(input);
            int allCount = beginList.Count + endList.Count;

            int[] pos = new int[allCount];
            MyMatch[] matchList = new MyMatch[allCount];

            int n = 0;
            foreach (Match m in beginList)
            {
                pos[n] = m.Index;
                matchList[n] = new MyMatch(m.Index, m.Index + m.Length, true);
                n++;
            }

            foreach (Match m in endList)
            {
                pos[n] = m.Index;
                matchList[n] = new MyMatch(m.Index, m.Index + m.Length, false);
                n++;
            }

            Array.Sort(pos, matchList);

            return FindDelimeted(input, matchList, isSigned);
        }

        public static ArrayList Matching( string delim, string input )
        {
            return MatchingHelper(delim, "asllkajsdfoiuwqer", input, false);
        }

        public static ArrayList Matching( string begin, string end, string input )
        {
            return MatchingHelper(begin, end, input, true);
        }

        private static ArrayList FindDelimeted( string input, MyMatch[] matchList, bool isSigned )
        {
            ArrayList res = new ArrayList();
            int allCount = matchList.Length;


            State st = State.outside;
            int beginPos = -1;

            for (int n = 0; n < allCount; ++n)
            {
                if (st == State.outside)
                {
                    if (isSigned && matchList[n].IsStart == false) // end match thrown out
                        continue;
                    else // begin match 
                    {
                        st = State.inside;
                        beginPos = matchList[n].End;
                    }
                }
                else if (st == State.inside)
                {
                    if (isSigned && matchList[n].IsStart == true) // begin match thrown out
                        continue;
                    else // end state
                    {
                        st = State.outside;
                        res.Add(input.Substring(beginPos, matchList[n].Begin - beginPos));
                    }
                }
            }
            return res;
        }

	}
}
