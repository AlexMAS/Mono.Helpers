using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Text;

using Mono.Unix.Native;

namespace System.ServiceProcess.Linux
{
	public sealed class LsbLinuxHostInstaller : Installer
	{
		private const int Timeout = 60 * 1000;


		public LsbLinuxHostInstaller(LinuxServiceSettings settings)
			: this(settings, null, LinuxServiceLogWriter.Null)
		{
		}

		public LsbLinuxHostInstaller(LinuxServiceSettings settings, Installer[] installers)
			: this(settings, installers, LinuxServiceLogWriter.Null)
		{
		}

		public LsbLinuxHostInstaller(LinuxServiceSettings settings, Installer[] installers, LinuxServiceLogWriter logWriter)
		{
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}

			_settings = settings;
			_installers = installers;
			_logWriter = logWriter;

			_installTransaction = new TransactionManager<LinuxServiceSettings>(_logWriter)
				.Stage(Properties.Resources.CreateServiceFileStage, CreateServiceFile, DeleteServiceFile)
				.Stage(Properties.Resources.SetServiceFileAsExecutableStage, SetServiceFileAsExecutable)
				.Stage(Properties.Resources.RegisterServiceFileStage, RegisterServiceFile, UnregisterServiceFile);
		}


		private readonly Installer[] _installers;
		private readonly LinuxServiceSettings _settings;
		private readonly LinuxServiceLogWriter _logWriter;
		private readonly TransactionManager<LinuxServiceSettings> _installTransaction;


		public override void Install(IDictionary stateSaver)
		{
			if (_installers != null)
			{
				Installers.AddRange(_installers);
			}

			var serviceName = BuildServiceName(_settings);

			_logWriter.InfoFormat(Properties.Resources.InstallingServiceIsStarted, serviceName);

			try
			{
				base.Install(stateSaver);

				_installTransaction.Execute(_settings);

				_logWriter.InfoFormat(Properties.Resources.InstallingServiceIsSuccessfullyCompleted, serviceName);
			}
			catch (Exception error)
			{
				error = new InstallException(string.Format(Properties.Resources.InstallingServiceFailed, serviceName), error);
				_logWriter.ErrorFormat(Properties.Resources.InstallingServiceIsCompletedWithErrors, serviceName, error);
				throw error;
			}
		}

		public override void Uninstall(IDictionary savedState)
		{
			if (_installers != null)
			{
				Installers.AddRange(_installers);
			}

			var serviceName = BuildServiceName(_settings);

			_logWriter.InfoFormat(Properties.Resources.UninstallingServiceIsStarted, serviceName);

			var errors = new List<Exception>();

			try
			{
				_installTransaction.Rollback(_settings);
			}
			catch (Exception error)
			{
				errors.Add(error);
			}

			try
			{
				base.Uninstall(savedState);
			}
			catch (Exception error)
			{
				errors.Add(error);
			}

			if (errors.Count > 1)
			{
				Exception error = new AggregateException(string.Format(Properties.Resources.UninstallingServiceFailed, serviceName), errors);
				error = new InstallException(string.Format(Properties.Resources.UninstallingServiceFailed, serviceName), error);
				_logWriter.ErrorFormat(Properties.Resources.UninstallingServiceIsCompletedWithErrors, serviceName, error);
				throw error;
			}

			if (errors.Count == 1)
			{
				var error = new InstallException(string.Format(Properties.Resources.UninstallingServiceFailed, serviceName), errors[0]);
				_logWriter.ErrorFormat(Properties.Resources.UninstallingServiceIsCompletedWithErrors, serviceName, error);
				throw error;
			}

			_logWriter.InfoFormat(Properties.Resources.UninstallingServiceIsSuccessfullyCompleted, serviceName);
		}


		private void CreateServiceFile(LinuxServiceSettings settings)
		{
			// Создание скрипта в '/etc/init.d'

			var serviceName = BuildServiceName(settings);
			var serviceFile = BuildServicePath(settings);
			var serviceScript = BuildServiceScript(settings, serviceName);

			File.WriteAllText(serviceFile, serviceScript);
		}

		private static void DeleteServiceFile(LinuxServiceSettings settings)
		{
			// Удаление скрипта из '/etc/init.d'

			var serviceFile = BuildServicePath(settings);

			if (File.Exists(serviceFile))
			{
				File.Delete(serviceFile);
			}
		}

		private static void SetServiceFileAsExecutable(LinuxServiceSettings settings)
		{
			// Выполнение команды: 'chmod +x <serviceFile>'

			var serviceFile = BuildServicePath(settings);

			Stat fileStatus;

			// Определение состояния файла
			if (Syscall.stat(serviceFile, out fileStatus) != 0)
			{
				throw new InstallException(string.Format(Properties.Resources.CantRetrieveFileStatus, serviceFile));
			}

			// Разрешение исполнения файла
			if (Syscall.chmod(serviceFile, fileStatus.st_mode | FilePermissions.S_IXUSR | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH) != 0)
			{
				throw new InstallException(string.Format(Properties.Resources.CantSetFileAsExecutable, serviceFile));
			}
		}

		private static void RegisterServiceFile(LinuxServiceSettings settings)
		{
			// Выполнение команды: 'update-rc.d <serviceFile> defaults'

			var serviceFile = Path.GetFileName(BuildServicePath(settings));
			var commandResult = MonoHelper.ExecuteShellCommand("update-rc.d {0} defaults", Timeout, serviceFile);

			if (commandResult.ExitCode != 0)
			{
				throw new InstallException(string.Format(Properties.Resources.CantRegisterServiceFile, serviceFile, commandResult.Completed ? commandResult.Output : null));
			}
		}

		private static void UnregisterServiceFile(LinuxServiceSettings settings)
		{
			// Выполнение команды: 'update-rc.d -f <serviceFile> remove'

			var serviceFile = Path.GetFileName(BuildServicePath(settings));
			var commandResult = MonoHelper.ExecuteShellCommand("update-rc.d -f {0} remove", Timeout, serviceFile);

			if (commandResult.ExitCode != 0)
			{
				throw new InstallException(string.Format(Properties.Resources.CantUnregisterServiceFile, serviceFile, commandResult.Completed ? commandResult.Output : null));
			}
		}


		private static string BuildServiceScript(LinuxServiceSettings settings, string serviceName)
		{
			return new StringBuilder(Properties.Resources.LsbLinuxServiceScript)
				.Replace("<ServiceName>", serviceName)
				.Replace("<Dependencies>", BuildDependencies(settings))
				.Replace("<DisplayName>", BuildDisplayName(settings))
				.Replace("<Description>", BuildDescription(settings))
				.Replace("<ServiceDir>", BuildServiceDirectory(settings))
				.Replace("<ServiceExe>", BuildServiceExecutable(settings))
				.Replace("<ServiceArgs>", BuildServiceArguments(settings))
				.Replace("<ServiceUser>", BuildServiceUser(settings))
				.Replace("<ServicePidDir>", "/var/run")
				.Replace("\r\n", "\n")
				.ToString();
		}

		private static string BuildServiceName(LinuxServiceSettings settings)
		{
			return settings.ServiceName;
		}

		private static string BuildServicePath(LinuxServiceSettings settings)
		{
			var serviceName = BuildServiceName(settings);
			return Path.Combine("/etc/init.d", serviceName);
		}

		private static string BuildDisplayName(LinuxServiceSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.DisplayName)
				? BuildServiceName(settings) : settings.DisplayName;
		}

		private static string BuildDescription(LinuxServiceSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.Description)
				? BuildDisplayName(settings) : settings.Description;
		}

		private static string BuildServiceDirectory(LinuxServiceSettings settings)
		{
			return Path.GetDirectoryName(settings.ServiceExe);
		}

		private static string BuildServiceExecutable(LinuxServiceSettings settings)
		{
			return Path.GetFileName(settings.ServiceExe);
		}

		private static string BuildServiceArguments(LinuxServiceSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.ServiceArgs) ? string.Empty : settings.ServiceArgs.Trim();
		}

		private static string BuildServiceUser(LinuxServiceSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.Username) ? Environment.UserName : settings.Username.Trim();
		}

		private static string BuildDependencies(LinuxServiceSettings settings)
		{
			var dependencies = settings.Dependencies;

			if (dependencies != null && dependencies.Length > 0)
			{
				dependencies = settings.Dependencies.Where(d => !string.IsNullOrWhiteSpace(d)).ToArray();
			}

			if (dependencies == null || dependencies.Length <= 0)
			{
				dependencies = new[] { "$local_fs", "$network", "$remote_fs", "$syslog" };
			}

			return string.Join(" ", dependencies);
		}
	}
}