using System.Diagnostics;

using Mono.Unix.Native;

namespace System
{
	public static class MonoHelper
	{
		public static bool RunningAsRoot
		{
			get
			{
				return Syscall.getuid() == 0;
			}
		}

		public static bool RunninOnUnix
		{
			get
			{
				var p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 6) || (p == 128));
			}
		}

		public static bool RunninOnLinux
		{
			get
			{
				var p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 128));
			}
		}

		public static bool RunningOnMono
		{
			get
			{
				return (Type.GetType("Mono.Runtime") != null);
			}
		}


		public static ProcessResult ExecuteShellCommand(string command, int timeout, params object[] arguments)
		{
			if (string.IsNullOrWhiteSpace(command))
			{
				throw new ArgumentNullException("command");
			}

			var result = new ProcessResult();

			command = string.Format(command, arguments).Trim();

			using (var shellProcess = new Process())
			{
				shellProcess.StartInfo.FileName = "sh";
				shellProcess.StartInfo.Arguments = string.Format("-c '{0}'", command);
				shellProcess.StartInfo.UseShellExecute = false;
				shellProcess.StartInfo.RedirectStandardOutput = true;
				shellProcess.Start();

				if (shellProcess.WaitForExit(timeout))
				{
					result.Completed = true;
					result.ExitCode = shellProcess.ExitCode;
					result.Output = shellProcess.StandardOutput.ReadToEnd();
				}
			}

			return result;
		}

		public struct ProcessResult
		{
			public bool Completed;
			public int? ExitCode;
			public string Output;
		}
	}
}