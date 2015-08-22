using System.Collections.Concurrent;

namespace System.IO
{
	internal sealed class FileChannelHistory
	{
		private readonly ConcurrentDictionary<string, DateTime> _history
			= new ConcurrentDictionary<string, DateTime>();


		public bool IsChanged(string filePath)
		{
			var lastWriteTime = File.GetLastWriteTime(filePath);

			DateTime lastReadTime;

			if (_history.TryGetValue(filePath, out lastReadTime))
			{
				if (lastWriteTime != lastReadTime)
				{
					_history.TryUpdate(filePath, lastWriteTime, lastReadTime);

					return true;
				}
			}
			else
			{
				_history.TryAdd(filePath, lastWriteTime);

				return true;
			}

			return false;
		}
	}
}