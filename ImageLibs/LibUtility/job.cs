using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.Dpu.Utility
{
	[Serializable]
	public abstract class Job
	{
		protected Job(string description)
		{
			this.description = description;
		}

		public abstract void Run();

		public enum JobStatus { BEGIN, INPROGRESS, DONE };
		public string description;

		// Assigned by the JobMaster
		public JobStatus status;
		public int pid, gid;
		public string file; // where the job is

		public override string ToString()
		{
			return String.Format("{0}({1}-{2}, {3}, {4})", GetType().Name, gid, pid, file, description);
		}

	}

	class JobGroupInfo
	{
		public JobGroupInfo()
		{
			this.ndone = 0;
			this.pids = new Hashtable();
		}

		public int ndone;
		public Hashtable pids;
		public JobMaster.JobCallback callback;
	}

	public class JobMaster
	{
		public delegate void JobCallback(Job job);

		string dir;
		static int curr_pid, curr_gid;
		Hashtable jobs;
		Hashtable ginfo; // gid -> group info

		public JobMaster(string dir)
		{
			this.dir = dir;
			jobs = new Hashtable();
			ginfo = new Hashtable();
		}

		public int NewGid()
		{
			return ++curr_gid;
		}
		private int NewPid()
		{
			return ++curr_pid;
		}

		// Serialize the job to disk
		/*public void AddJob(Job job)
		{
			AddJob(job, NewGid());
		}*/

		public void AddJob(Job job, int gid)
		{
			job.pid = NewPid();
			job.gid = gid;
			job.file = job.description + "." + job.gid + "-" + job.pid;
			job.status = Job.JobStatus.INPROGRESS;

			JobGroupInfo gi = (JobGroupInfo)ginfo[gid];
			if(gi == null)
				ginfo[gid] = gi = new JobGroupInfo();
			if(gi.pids[job.pid] != null)
				throw new ArgumentException("Group already contains that pid");
			gi.pids[job.pid] = true;

			JobUtils.WriteJob(job, dir+"/"+job.file, JobUtils.slave);
		}

		// Go read the jobs that have been completed
		private void ReapJobs()
		{
			foreach(string file in JobUtils.FindAllJobs(dir, JobUtils.master))
			{
				Job job = JobUtils.ReadJob(file, JobUtils.master, true);
				int gid = job.gid;
				JobGroupInfo gi = (JobGroupInfo)ginfo[gid];
				if(gi == null)
				{
					Console.WriteLine("ReapJobs(): unknown group {0}", gid);
					continue;
				}

				if(gi.pids[job.pid] == null)
				{
					Console.WriteLine("ReapJobs(): unknown group {0} doesn't contain pid {1}", gid, job.pid);
					continue;
				}

				Console.WriteLine("ReapJobs(): job {0} finished; {1}/{2} done in group", job, gi.ndone, gi.pids.Count);
				gi.ndone++;
				if(gi.callback != null)
					gi.callback(job);
			}
		}

		private JobGroupInfo GetJobGroupInfo(int gid)
		{
			JobGroupInfo gi = (JobGroupInfo)ginfo[gid];
			if(gi == null)
				throw new ArgumentException("Don't know about " + gid);
			return gi;
		}

		private bool IsGroupDone(int gid)
		{
			JobGroupInfo gi = GetJobGroupInfo(gid);
			return gi.ndone == gi.pids.Count;
		}

		public void WaitUntilGroupDone(int gid)
		{
			while(!IsGroupDone(gid))
			{
				ReapJobs();
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}

		public void SetCallback(int gid, JobCallback callback)
		{
			JobGroupInfo gi = GetJobGroupInfo(gid);
			gi.callback = callback;
		}

		public void RemoveGroup(int gid)
		{
			ginfo.Remove(gid);
		}
	}

	public class JobSlave
	{
		public int waitTime = 1; // seconds
		public string dir;

		public JobSlave(string dir)
		{
			this.dir = dir;
		}

		private Job ReadJob()
		{
			string file = JobUtils.FindJob(dir, JobUtils.slave);
			if(file == null) return null;
			return JobUtils.ReadJob(file, JobUtils.slave, true);
		}

		private void WriteJob(Job job)
		{
			JobUtils.WriteJob(job, dir+"/"+job.file, JobUtils.master);
		}

		protected virtual void RunJob(Job job)
		{
			Console.WriteLine("JobSlave.RunJob(): running " + job);
			job.Run();
		}

		public void Run()
		{
			while(true)
			{
				Job job = ReadJob();
				if(job != null)
				{
					RunJob(job);
					WriteJob(job);
				}

				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}
	}

	public class JobUtils
	{
		public static string slave = "slave";
		public static string master = "master";

		public static void WriteJob(Job job, string file, string recipient)
		{
			string signalfile = file + "." + recipient;

			// Create the file that signals a job is ready for the recipient
			if(File.Exists(signalfile))
				throw new ArgumentException("Signal file exists");

			// Write the actual job out
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			formatter.Serialize(stream, job);
			stream.Close();

			File.Create(signalfile).Close();
		}

		public static Job ReadJob(string file, string recipient, bool deleteFile)
		{
			string signalfile = file + "." + recipient;

			// Check for the signal file
			if(!File.Exists(signalfile))
				return null;
			File.Delete(signalfile);

			// Read the file out
			if(!File.Exists(file))
				throw new ArgumentException("Signal file exists, but actual file doesn't");
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			Job job = (Job)formatter.Deserialize(stream);
			stream.Close();

			if(deleteFile)
				File.Delete(file);

			return job;
		}

		// Look for a job for the recipient
		public static string[] FindAllJobs(string dir, string recipient)
		{
			string[] files = Directory.GetFiles(dir, "*."+recipient);
			for(int i = 0; i < files.Length; i++)
				files[i] = files[i].Replace("."+recipient, "");
			return files;
		}

		public static string FindJob(string dir, string recipient)
		{
			string[] files = FindAllJobs(dir, recipient);
			if(files.Length == 0) return null;
			return files[0];
		}
	}
}
