using System.Diagnostics;
using System.Threading.Tasks;

using Mono.Unix.Native;

namespace System
{
	public static class MonoHelper
	{
		public const int DefaultTimeout = 60 * 1000;


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


		public static Task ExecuteProcess(string fileName, string arguments = null, int timeout = DefaultTimeout)
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentNullException("fileName");
			}

			return Task.Run(() =>
			{
				fileName = fileName.Trim();
				arguments = (arguments ?? "").Trim();

				var result = new ProcessResult();

				using (var process = new Process())
				{
					process.StartInfo.FileName = fileName;
					process.StartInfo.Arguments = arguments;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.RedirectStandardError = true;
					process.Start();

					if (process.WaitForExit(timeout))
					{
						result.Completed = true;
						result.ExitCode = process.ExitCode;
						result.Output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
					}
				}

				if (!result.Completed || result.ExitCode != 0)
				{
					var processName = fileName + (string.IsNullOrEmpty(arguments) ? string.Empty : (" " + arguments));

					if (result.Completed)
					{
						throw new InvalidOperationException(string.Format(Properties.Resources.ProcessCompletedWithAnError, processName, result.ExitCode, result.Output));
					}

					throw new InvalidOperationException(string.Format(Properties.Resources.ProcessCompletedWithAnErrorByTimeout, processName, timeout));
				}
			});
		}


		private struct ProcessResult
		{
			public bool Completed;
			public int? ExitCode;
			public string Output;
		}
	}
}