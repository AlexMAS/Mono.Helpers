namespace System.ServiceProcess.Linux
{
	public sealed class LinuxServiceLogWriter
	{
		public static readonly LinuxServiceLogWriter Null = new LinuxServiceLogWriter();

		public delegate void LogWriterFunc(string format, params object[] args);


		public LinuxServiceLogWriter(LogWriterFunc debugFormat = null, LogWriterFunc infoFormat = null, LogWriterFunc warnFormat = null, LogWriterFunc errorFormat = null, LogWriterFunc fatalFormat = null)
		{
			_debugFormat = debugFormat;
			_infoFormat = infoFormat;
			_warnFormat = warnFormat;
			_errorFormat = errorFormat;
			_fatalFormat = fatalFormat;
		}


		private readonly LogWriterFunc _debugFormat;
		private readonly LogWriterFunc _infoFormat;
		private readonly LogWriterFunc _warnFormat;
		private readonly LogWriterFunc _errorFormat;
		private readonly LogWriterFunc _fatalFormat;


		public void DebugFormat(string format, params object[] args)
		{
			LogFormat(_debugFormat, format, args);
		}

		public void InfoFormat(string format, params object[] args)
		{
			LogFormat(_infoFormat, format, args);
		}

		public void WarnFormat(string format, params object[] args)
		{
			LogFormat(_warnFormat, format, args);
		}

		public void ErrorFormat(string format, params object[] args)
		{
			LogFormat(_errorFormat, format, args);
		}

		public void FatalFormat(string format, params object[] args)
		{
			LogFormat(_fatalFormat, format, args);
		}


		private static void LogFormat(LogWriterFunc logFunc, string format, params object[] args)
		{
			if (logFunc != null)
			{
				try
				{
					logFunc(format, args);
				}
				catch
				{
				}
			}
		}
	}
}