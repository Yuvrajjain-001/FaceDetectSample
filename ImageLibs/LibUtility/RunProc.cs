using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace DPU.Utility
{
	class RunProc
	{
		public void Exec(Process proc, string command, string args, ref string output, ref string error)
		{
			ProcessStartInfo startInfo = proc.StartInfo;
			startInfo.FileName = command;
			startInfo.Arguments = args;
			Console.WriteLine("Running {0} with {1}", startInfo.FileName, startInfo.Arguments);

			proc.Start();

			error = proc.StandardError.ReadToEnd();
			output = proc.StandardOutput.ReadToEnd();

			proc.WaitForExit();
		}
	}
}
