namespace System.ServiceProcess.Linux
{
	public sealed class LinuxServiceSettings
	{
		public string ServiceName { get; set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }

		public string ServiceExe { get; set; }

		public string ServiceArgs { get; set; }

		public string Username { get; set; }

		public string[] Dependencies { get; set; }
	}
}