using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Mono.Unix.Native;

namespace System.ServiceProcess.Linux
{
	public sealed class LsbLinuxServiceController
	{
		private LsbLinuxServiceController(string serviceName)
		{
			ServiceName = serviceName;
		}


		public string ServiceName { get; private set; }


		public void Start(TimeSpan timeout)
		{
			Start(timeout.Milliseconds);
		}

		public void Start(int timeout = Timeout.Infinite)
		{
			var result = MonoHelper.ExecuteShellCommand("service {0} start", timeout, ServiceName);

			if (result.ExitCode != 0)
			{
				throw new InvalidOperationException(string.Format(Properties.Resources.CantStartService, ServiceName, result.Completed ? result.Output : null));
			}
		}


		public void Stop(TimeSpan timeout)
		{
			Stop(timeout.Milliseconds);
		}

		public void Stop(int timeout = Timeout.Infinite)
		{
			var result = MonoHelper.ExecuteShellCommand("service {0} stop", timeout, ServiceName);

			if (result.ExitCode != 0)
			{
				throw new InvalidOperationException(string.Format(Properties.Resources.CantStopService, ServiceName, result.Completed ? result.Output : null));
			}
		}


		public static LsbLinuxServiceController GetService(string serviceName)
		{
			return GetServices(serviceName).FirstOrDefault();
		}

		public static IEnumerable<LsbLinuxServiceController> GetServices(string searchPattern = "*")
		{
			var files = Directory.GetFiles("/etc/init.d/", searchPattern, SearchOption.TopDirectoryOnly);

			foreach (var file in files)
			{
				Stat fileStatus;

				if (Syscall.stat(file, out fileStatus) == 0
					&& (fileStatus.st_mode.HasFlag(FilePermissions.S_IXUSR)
						|| fileStatus.st_mode.HasFlag(FilePermissions.S_IXGRP)
						|| fileStatus.st_mode.HasFlag(FilePermissions.S_IXOTH)))
				{
					yield return new LsbLinuxServiceController(Path.GetFileName(file));
				}
			}
		}
	}
}