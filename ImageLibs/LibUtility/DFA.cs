using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Dpu.Utility
{
    #region Character Matching
    public interface CharGroup
    {
        bool Match(char c);
        char Char { get ; }
    }

    public class SingleChar : CharGroup
    {
        #region Constructor
        public SingleChar(char c) { _char = c; }
        #endregion Constructor

        #region Fields
        protected char _char;
        #endregion Fields

        #region Static Objects
        public static SingleChar NullChar
        {
            get {
                if (_nullChar == null)
                    _nullChar = new SingleChar((char) 0);
                return _nullChar;
            }
        }
        protected static SingleChar _nullChar;
        #endregion Static Objects

        #region Methods
        public bool Match(char c) { return (c == _char); }

        public char Char { get { return _char; } }

        public override bool Equals(object obj)
        {
            SingleChar other = (obj as SingleChar);
            if (other != null) return (_char == other._char);
            return false;
        }

        public override int GetHashCode()
        {
            return (int)_char;
        }

        public bool IsNullChar { get { return (_char == (char)0); } }

        public override string ToString()
        {
            if (_char == 0) return "''";
            else return "'" + _char.ToString() + "'";
        }
        #endregion Methods
    }

    public class CharRange : CharGroup
    {
        #region Constructor
        public CharRange(char begin, char end)
        {
            _begin = begin;
            _end = end;
        }
        #endregion Constructor

        #region Static Objects
        public static CharRange Digits
        {
            get
            {
                if (_digits == null)
                    _digits = new CharRange('0', '9');
                return _digits;
            }
        }

        public static CharRange UpperCase
        {
            get
            {
                if (_upperCase == null)
                    _upperCase = new CharRange('A', 'Z');
                return _upperCase;
            }
        }

        public static CharRange LowerCase
        {
            get
            {
                if (_lowerCase == null)
                    _lowerCase = new CharRange('a', 'z');
                return _lowerCase;
            }
        }


        protected static CharRange _digits;
        protected static CharRange _upperCase;
        protected static CharRange _lowerCase;
        #endregion Static Objects

        #region Fields
        char _begin;
        char _end;
        #endregion Fields

        #region Methods
        public bool Match(char c) { return ((c >= _begin) && (c <= _end)); }

        public char Char { get { return (char)_begin;  } }

        public override string ToString()
        {
            return "[" + _begin.ToString() + "," + _end.ToString() + "]";
        }
        #endregion Methods

    }

    public class CharSet : CharGroup
    {
        #region Constructor
        public CharSet(char[] elems)
        {
            _chars = (char[]) elems.Clone();
            Array.Sort<char>(_chars);
        }
        #endregion Constructor

        #region Fields
        char[] _chars;
        #endregion Fields

        #region Static Objects
        public static CharSet WhiteSpace
        {
            get
            {
                if (_whiteSpace == null)
                    _whiteSpace = new CharSet(new char[] { ' ', '\n', '\t' });
                return _whiteSpace;

            }
        }

        protected static CharSet _whiteSpace;
        #endregion Static Objects

        #region Methods
        public bool Match(char c)
        {
            return (Array.BinarySearch<char>(_chars, c) > 0);
        }

        public char Char { get { return _chars[0]; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("{");
            foreach (char c in _chars)
            {
                sb.Append(c.ToString());
                sb.Append(",");
            }
            sb.Append("}");
            return sb.ToString();
        }
        #endregion Methods
    }

    public class CharGroupManager
    {
        public static CharGroup GetCharGroup(string type, string desc)
        {
            if (type.ToUpper() == "CHARRANGE")
            {
                if (desc.Length != 3) throw new FormatException("Invalid format");
                return new CharRange(desc[0], desc[2]);
            }
            else if (type.ToUpper() == "CHARSET")
            {
                return new CharSet(parse(desc));
            }
            else if (type.ToUpper() == "CHAR")
            {
                if (desc.Length >= 2)
                {
                    if (desc[1] == 't')
                        return new SingleChar('\t');
                    else if (desc[1] == 'n')
                        return new SingleChar('\n');
                    else
                        return new SingleChar(desc[1]);
                }
                return new SingleChar(char.Parse(desc));
            }
            else
                throw new ApplicationException("Invalid Command");
        }

        public static char[] parse(string s)
        {
            List<char> outChars = new List<char>();
            char[] inp = s.ToCharArray();
            int pos = 0;
            while (pos < inp.Length)
            {
                if (inp[pos] != '\\')
                    outChars.Add(inp[pos]);
                else if (pos < inp.Length - 1)
                {
                    char c = inp[pos + 1];
                    if (c == 't')
                    {
                        outChars.Add('\t');
                    }
                    else if (c == 's')
                    {
                        outChars.Add(' ');
                        outChars.Add('\t');
                        outChars.Add('\n');
                    }
                    else if (c == 'n')
                    {
                        outChars.Add('\n');
                    }
                    else if (c == 'd')
                    {
                        for (int i = 0; i < 9; ++i)
                            outChars.Add((char)('0' + i));
                    }
                    else
                        outChars.Add(c);
                    ++pos;
                }
                ++pos;
            }
            inp = new char[outChars.Count];
            for (int i = 0; i < outChars.Count; ++i)
                inp[i] = outChars[i];
            return inp;
        }
    }
    #endregion Character Matching


    public class DFAMatch
    {
        public DFAMatch(char[] sequence, int maxEditDistance)
        {
            maxCost = maxEditDistance;
            inpSequence = sequence;
            End = sequence.Length;
            bestOutSequence = new char[End + maxEditDistance];
            bestOutCost = int.MaxValue;
            currOutCost = 0;
            currOutSequence = new char[End + maxEditDistance];
            outSequenceLen = 0;
            Begin = 0;
        }

        public DFAMatch(char[] sequence, int maxEditDistance, int begin, int end)
        {
            maxCost = maxEditDistance;
            inpSequence = sequence;
            End = sequence.Length;
            bestOutSequence = new char[End + maxEditDistance];
            bestOutCost = int.MaxValue;
            currOutCost = 0;
            currOutSequence = new char[End + maxEditDistance];
            outSequenceLen = 0;
            Begin = begin;
            End = end;
        }

        /// <summary>
        /// The sequence that we've matched against
        /// </summary>
        public readonly char[] inpSequence;

        /// <summary>
        /// The last character that we're matching against (input)
        /// </summary>
        public readonly int End;

        /// <summary>
        /// The first character we're matching against (input)
        /// </summary>
        public int Begin;

        /// <summary>
        /// The ouput of the match (output)
        /// </summary>
        public char[] bestOutSequence;
        
        /// <summary>
        /// Cost of the match (output)
        /// </summary>
        public int bestOutCost;

        /// <summary>
        /// Max cost you're willing to accept (input)
        /// </summary>
        public int maxCost;

        /// <summary>
        /// Internal state
        /// </summary>
        public int currOutCost;
        
        /// <summary>
        /// Internal state
        /// </summary>
        public int outSequenceLen;

        /// <summary>
        /// Internal state
        /// </summary>
        public char[] currOutSequence;

    }


    public class DFANode
    {
        public enum FileContents {Lexicon, DFA };

        #region Constructors
        private DFANode(CharGroup[] chars, DFANode[] nodes, bool accept)
        {
            _nodeNum = _numNodes++;
            _nextChar = chars;
            _nextNode = nodes;
            _numChildren = nodes.Length;
            _isAcceptState = accept;
        }

        public DFANode(string lexiconFileName, FileContents fc)
        {
            _nodeNum = _numNodes++;
            if (fc == FileContents.Lexicon)
            {
                #region Handle Lexicon Data
                // TODO: Modify lexicon lists so that the first line specifies the number of elements in the lexicon.
                MaxLen = -1;
                List<string> lexicon = new List<string>();
                try
                {
                    using (StreamReader sr = new StreamReader(lexiconFileName))
                    {
                        String line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.Length > MaxLen) MaxLen = line.Length;
                            lexicon.Add(line.ToUpper());
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("Could not read file {0}", lexiconFileName);
                    Console.WriteLine(e.Message);
                    throw e;
                }
                lexicon.Sort();
                char[][] inputSequences = new char[lexicon.Count][];
                for (int i = 0; i < lexicon.Count; ++i)
                    inputSequences[i] = lexicon[i].ToCharArray();
                fromSequences(inputSequences, 0, lexicon.Count, 0);

                foreach (string s in lexicon)
                {
                    // DFANode.DebugLevel = 1;
                    // System.Console.WriteLine("Checking {0}", s);
                    DFAMatch m = new DFAMatch(s.ToCharArray(), 0);
                    Match(0, 0, this, m);
                    if (m.bestOutCost != 0)
                        throw new ApplicationException("Internal Error");
                }


                #endregion Handle Lexicon Data
            }
            else if (fc == FileContents.DFA)
            {
                #region Handle DFA Data
                Exception invalidFormat = new FormatException("Invalid File Format");
                using (StreamReader sr = new StreamReader(lexiconFileName))
                {
                    String line;
                    if ((line = sr.ReadLine()) != null)
                    {
                        Regex r = new Regex(@"NumNodes\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                        Regex n = new Regex(@"^\s*NodeDescription\s*NumTransitions\s*=\s*(\d+)\s*(Accept)?", RegexOptions.IgnoreCase);
                        Regex t = new Regex(@"^\s*(Char|CharSet|CharRange)\((.*)\)\s+(\d+)\s*$", RegexOptions.IgnoreCase);
                        Match m = r.Match(line);
                        if (!m.Success) throw invalidFormat;
                        Group g = m.Groups[1];
                        int numNodes = Int32.Parse(g.Value);
                        DFANode[] nodes = new DFANode[numNodes];
                        int[][] nodeIdx = new int[numNodes][];
                        DFANode[][] nextNodes = new DFANode[numNodes][];
                        for (int i = 0; i < numNodes; ++i)
                        {
                            if ((line = sr.ReadLine()) == null) throw invalidFormat;
                            m = n.Match(line);
                            if (!m.Success) throw invalidFormat;
                            g = m.Groups[1];
                            int numTransitions = Int32.Parse(g.Value);
                            nodeIdx[i] = new int[numTransitions];
                            nextNodes[i] = new DFANode[numTransitions];
                            CharGroup[] cg = new CharGroup[numTransitions];
                            bool isAccept = false;
                            if ((m.Groups.Count > 2) && (m.Groups[2].Value.ToUpper() == "ACCEPT"))
                                isAccept = true;
                            for (int j = 0; j < numTransitions; ++j)
                            {
                                if ((line = sr.ReadLine()) == null) throw invalidFormat;
                                m = t.Match(line);
                                if (!m.Success) throw invalidFormat;
                                string type = m.Groups[1].Value;
                                string desc = m.Groups[2].Value;
                                cg[j] = CharGroupManager.GetCharGroup(type, desc);
                                nodeIdx[i][j] = Int32.Parse(m.Groups[3].Value);
                            }
                            nodes[i] = new DFANode(cg, nextNodes[i], isAccept);
                        }
                        for (int i = 0; i < numNodes; ++i)
                        {
                            int numTransitions = nodeIdx[i].Length;
                            for (int j = 0; j < numTransitions; ++j)
                            {
                                nextNodes[i][j] = nodes[nodeIdx[i][j]];
                            }
                        }
                    }
                    else
                    {
                        throw invalidFormat;
                    }
                }
                #endregion Handle DFA Data
            }
        }

        public DFANode(string[] inputStrings, int beginSeq, int endSeq)
        {
            _nodeNum = _numNodes++;
            char[][] inputSequences = new char[inputStrings.Length][];
            MaxLen = -1;
            for (int i = 0; i < inputSequences.Length; ++i)
            {
                inputSequences[i] = inputStrings[i].ToCharArray();
                if (inputSequences[i].Length > MaxLen) MaxLen = inputSequences[i].Length;
            }
            fromSequences(inputSequences, 0, inputSequences.Length, 0);
        }

        public DFANode(char[][] inputSequences, int beginSeq, int endSeq, int posn)
        {
            _nodeNum = _numNodes++;
            fromSequences(inputSequences, beginSeq, endSeq, posn);
        }

        protected void fromSequences(char[][] inputSequences, int beginSeq, int endSeq, int posn)
        {
            List<int> splitIdx = new List<int>();
            splitIdx.Add(beginSeq);
            char lastChar = (char)0;
            if (inputSequences[beginSeq].Length > posn)
                lastChar = inputSequences[beginSeq][posn];
            for (int i = beginSeq + 1; i < endSeq; ++i)
            {
                char currChar = (char)0;
                if (inputSequences[i].Length > posn)
                    currChar = inputSequences[i][posn];
                if (currChar != lastChar)
                {
                    lastChar = currChar;
                    splitIdx.Add(i);
                }
            }
            splitIdx.Add(endSeq);
            _numChildren = splitIdx.Count - 1;
            _nextChar = new CharGroup[_numChildren];
            _nextNode = new DFANode[_numChildren];
            if ((_numChildren == 1) && (inputSequences[beginSeq].Length <= posn))
            {
                _nextChar[0] = SingleChar.NullChar;
                _nextNode[0] = null;
                _isAcceptState = true;
                return;
            }
            
            for (int i = 0; i < _numChildren; ++i)
            {
                _nextChar[i] = SingleChar.NullChar;
                _nextNode[i] = null;
                if (inputSequences[splitIdx[i]].Length > posn)
                {
                    int begin = splitIdx[i];
                    int end = splitIdx[i + 1];
                    _nextChar[i] = new SingleChar(inputSequences[begin][posn]);
                    if ((begin + 1 == end) && (inputSequences[begin].Length == posn + 1))
                        _nextNode[i] = TerminalNode;
                    else
                        _nextNode[i] = new DFANode(inputSequences, begin, end, posn + 1);
                }
            }
        }
        #endregion Constructors

        #region Fields
        protected CharGroup[] _nextChar;
        protected DFANode[] _nextNode;
        protected int _numChildren;
        protected bool _isAcceptState;
        protected int _nodeNum;
        static protected int _numNodes;
        static protected DFANode _terminalNode;
        static public int DebugLevel;
        static public int MaxLen;
        #endregion Fields

        #region Properties
        public static DFANode TerminalNode
        {
            get
            {
                if (_terminalNode == null)
                {
                    CharGroup[] cg = new CharGroup[] { SingleChar.NullChar };
                    DFANode[] dn = new DFANode[] { null } ;
                    _terminalNode = new DFANode(cg, dn, true);
                }
                return _terminalNode;
            }
        }




        public bool IsAcceptState { get { throw new ApplicationException("");  return ((_nextChar == null) || (_nextChar[0] == SingleChar.NullChar)); } }

        public static int NumNodes { get { return _numNodes; } }
        #endregion Properties

        #region Methods
        public void Match(int idx, int outIdx, DFANode prevNode, DFAMatch match)
        {
            if (DebugLevel > 0)
            {
                if (match.inpSequence.Length > idx)
                    System.Console.WriteLine("Matching (pos {0}, char {1}) with Node {2}", idx, match.inpSequence[idx], this);
                else
                    System.Console.WriteLine("Matching (pos {0}, char -) with Node {1}", idx, this);
            }
            if ((idx == match.End) && _isAcceptState)
            {
                if (match.currOutCost < match.bestOutCost)
                {
                    match.bestOutCost = match.currOutCost;
                    for (int i = 0; i < outIdx; ++i)
                        match.bestOutSequence[i] = match.currOutSequence[i];
                    match.outSequenceLen = outIdx;
                }
                return;
            }
            if (idx > match.End) return;
            if (match.currOutCost > match.maxCost) return;
            if (match.currOutCost > match.bestOutCost) return;
            for (int i = 0; i < _numChildren; ++i)
            {
                DFANode n = _nextNode[i];
                match.currOutSequence[outIdx] = _nextChar[i].Char;
                if (_nextChar[i].Match(match.inpSequence[idx]))
                {
                    n.Match(idx + 1, outIdx + 1, this, match);
                    if ((match.currOutCost < match.maxCost) && (match.currOutCost < match.bestOutCost-1))
                    {
                        match.currOutCost++;
                        n.Match(idx, outIdx + 1, this, match);
                        match.currOutCost--;
                    }
                }
                else
                {
                    if ((match.currOutCost < match.maxCost) && (match.currOutCost < match.bestOutCost-1))
                    {
                        match.currOutCost++;
                        n.Match(idx + 1, outIdx + 1, this, match);
                        n.Match(idx, outIdx + 1, this, match);
                        match.currOutCost--;
                    }
                }
            }
            if ((match.currOutCost < match.maxCost) && (match.currOutCost < match.bestOutCost-1))
            {
                match.currOutCost++;
                Match(idx + 1, outIdx, this, match);
                match.currOutCost--;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[Num= ");
            sb.Append(_nodeNum.ToString());
            sb.Append(", Children = ");
            for (int i = 0; i < _numChildren; ++i)
            {
                sb.Append("(");
                sb.Append(_nextChar[i].ToString());
                sb.Append(",");
                if (_nextNode[i] != null)
                    sb.Append(_nextNode[i]._nodeNum);
                else sb.Append("-");
                sb.Append(") ");
            }

            sb.Append(" ]");

            return sb.ToString();
        }
        #endregion Methods
    }


    public class DFA
    {
        public DFA(string lexiconFileName, DFANode.FileContents fc)
        {
            _root = new DFANode(lexiconFileName, fc);
        }

        public DFA(string[] inputStrings)
        {
            _root = new DFANode(inputStrings, 0, inputStrings.Length);
        }

        public DFAMatch Match(string s, int maxEditDistance)
        {
            DFAMatch m = new DFAMatch(s.ToCharArray(), maxEditDistance);
            _root.Match(0, 0, _root, m);
            return m;
        }

        public DFAMatch Match(string s, int maxEditDistance, int begin, int end)
        {
            DFAMatch m = new DFAMatch(s.ToCharArray(), maxEditDistance, begin, end);
            _root.Match(begin, 0, _root, m);
            return m;
        }

        public List<int[]> MatchAll(string s, int maxEditDistance, int begin, int end)
        {
            char[] chars = s.ToCharArray();
            List<int[]> outputs = new List<int[]>();
            for (int i = begin; i < end; i++)
            {
                int last = Math.Min(i+DFANode.MaxLen+1, end);
                for (int j = i + 1; j < last; j++)
                {
                    DFAMatch m = new DFAMatch(chars, maxEditDistance, i, j);
                    _root.Match(i, 0, _root, m);
                    if (m.bestOutCost <= m.maxCost)
                    {
                        outputs.Add(new int[] { i, j, m.bestOutCost });
                    }
                }
            }
            return outputs;
        }

        #region Fields
        DFANode _root;
        #endregion Fields
    }
}
