using System;
using System.IO;
using System.Collections;
using System.Xml.Serialization;

namespace Dpu.Utility
{
		
	[Serializable]
	public class LexiconCount 
	{
		[Serializable]
		private class WordCount 
		{
            private string _word;
            public string Word 
            {
                get { return _word; }
                set { _word = value; }
            }

            private double _count;
            public double Count 
            {
                get { return _count; }
                set { _count = value; }
            }

			public WordCount() {}

			public WordCount(string word) 
			{
				Word = word;
				Count = 1;
			}
		}

        private ArrayList _countsList;
        [ XmlArrayItem (typeof(WordCount), ElementName = "WC")]
        public ArrayList CountsList 
        {
            get { return _countsList; }
            set { _countsList = value; }
        }

		private Hashtable _htCounts;

		public int Count 
		{
			get { return _htCounts.Count; }
		}

		public LexiconCount() 
		{
			CountsList = new ArrayList();
			_htCounts  = new Hashtable();
		}

		public double GetWordCount(string word)
		{
			WordCount wc = (WordCount) _htCounts[word];
			if (wc == null)
				return 0;
			else
				return wc.Count;
		}

		public void Add(string word)
		{
			WordCount wc = (WordCount) _htCounts[word];
			if (wc == null) 
			{
				_htCounts.Add(word, new WordCount(word));
			}
			else 
				++wc.Count;
		}

        /*
        public void Prune(int minWords)
        {
            ArrayList remove = new ArrayList();
            foreach(DictionaryEntry de in _htCounts) 
            {
                WordCount wc = (WordCount) de.Value;
                if (wc.Count < minWords) 
                {
                    remove.Add(de.Value);
                }
            }
            Console.WriteLine(String.Format("Removing {0} words from {1}", remove.Count, _htCounts.Count));
            foreach(object word in remove) 
            {
                _htCounts.Remove(word);
            }
            Console.WriteLine(String.Format("Left with {0} word counts", _htCounts.Count));
        }
        */
			
		private static XmlSerializer ser = new XmlSerializer(typeof(LexiconCount));

		public static void Serialize (LexiconCount lc, TextWriter writer) 
		{
			lc.CountsList.Clear();
			lc.CountsList.AddRange(lc._htCounts.Values);
			ser.Serialize(writer, lc);
			lc.CountsList.Clear();
		}

		public static LexiconCount Deserialize(TextReader reader)
		{
			LexiconCount res = (LexiconCount) ser.Deserialize(reader);
			foreach(WordCount wc in res.CountsList) 
			{
				res._htCounts.Add(wc.Word, wc);
			}
			res.CountsList.Clear();
			return res;
		}
	}
}
