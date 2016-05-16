using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Dpu.Utility
{
	using Output = Console;

    public interface ILogger
    {
        void Write(int level, string str);
        void WriteLine(int level, string str);
    }

    public class ConsoleLogger : ILogger
    {
        public int IndentSize = 3;
        private bool _writeTabsNext = true;
        public void Write(int v, string str)
        {
            string[] split = str.Split('\r', '\n');
            for(int i = 0; i < split.Length; i++)
            {
                if(_writeTabsNext) WriteTabs(v);
                if(i == split.Length-1)
                {
					Output.Error.Write(split[i]);//.Trim()
                    _writeTabsNext = false;
                }
                else
                {
                    Output.Error.WriteLine(split[i]);//.Trim()
                    _writeTabsNext = true;
                }
            }
        }

        public void WriteLine(int v, string str)
        {
            Write(v, str);
            Output.Error.WriteLine("");
            _writeTabsNext = true;
        }

        private void WriteTabs(int v)
        {
            for(int i = 0; i < v*IndentSize; i++)
            {
                Output.Error.Write(" ");
            }
        }
    }


    public class Log
    {
        public static int Verbose = 100;
        private static int LastV = 0;
        public static ILogger Logger = new ConsoleLogger();

        public static void Indent(int levels) { LastV += levels; Debug.Assert(LastV >= 0); }

        public static void Indent() { Indent(1); }

        public static void WriteIf(bool condition, string format, params object[] items)
        {
            if(condition) Write(format, items);
        }

        public static void WriteLineIf(bool condition, string str)
        {
            if(condition) WriteLine(str);
        }

        public static void WriteLineIf(bool condition, string format, params object[] items)
        {
            if(condition) WriteLine(format, items);
        }

        public static void WriteLineIf(bool condition, int v, string format, params object[] items)
        {
            if(condition) WriteLine(v, format, items);
        }

        public static void WriteEnumerableIf(bool condition, string caption, IEnumerable en)
        {
            if(condition) WriteEnumerable(caption, en);
        }

        private static void WriteLine(int v, string str)
        {
            if(Logger != null && Verbose >= v) Logger.WriteLine(v, str);
            LastV = v;
        }

        private static void WriteLine(int v, string format, params object[] items)
        {
            WriteLine(v, String.Format(format, items));
        }

        private static void Write(int v, string str)
        {
            if(Verbose >= v && Logger != null) Logger.Write(v, str);
            LastV = v;
        }

        private static void Write(int v, string format, params object[] items)
        {
            Write(v, String.Format(format, items));
        }
 
        public static void Write(string str)
        {
            Write(LastV, str);
        }

        public static void Write(string format, params object[] items)
        {
            Write(LastV, format, items);
        }

        public static void WriteLine(string str)
        {
            WriteLine(LastV, str);
        }

        public static void WriteLine(string format, params object[] items)
        {
            WriteLine(LastV, format, items);
        }

        public static void WriteLineIndent(string format, params object[] items)
        {
            WriteLine(LastV+1, format, items);
        }

        public static void WriteEnumerable(string caption, IEnumerable en)
        {
            WriteLine(caption + " {");
            Indent(1);
            foreach(object obj in en)
            {
                WriteLine(obj.ToString());
            }
            Indent(-1);
            WriteLine("}");
        }

    }


    public class LogUtils
    {
        public static string PrintCollection(ICollection c, string separator)
        {
            StringBuilder sb = new StringBuilder();
            int cursor = 0;
            foreach(object o in c)
            {
                sb.Append(o.ToString());
                if(cursor++ < c.Count-1)
                {
                    sb.Append(separator);
                }
            }
            return sb.ToString();
        }
    }
}
