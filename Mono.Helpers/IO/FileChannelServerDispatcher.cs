namespace System.IO
{
	internal sealed class FileChannelServerDispatcher : IDisposable
	{
		public FileChannelServerDispatcher(string directory, string channelName, IFileChannelFormatter channelFormatter, Action<object> onReceiveRequestMessage)
		{
			if (string.IsNullOrEmpty(directory))
			{
				throw new ArgumentNullException("directory");
			}

			if (string.IsNullOrEmpty(channelName))
			{
				throw new ArgumentNullException("channelName");
			}

			if (channelFormatter == null)
			{
				throw new ArgumentNullException("channelFormatter");
			}

			if (onReceiveRequestMessage == null)
			{
				throw new ArgumentNullException("onReceiveRequestMessage");
			}

			var requestFileMask = string.Format("*.{0}.Request", channelName);

			_channelName = channelName;
			_serverChannel = new FileChannel(directory, requestFileMask, channelFormatter, onReceiveRequestMessage);
		}


		private readonly string _channelName;
		private readonly FileChannel _serverChannel;


		public void Reply(string client, object message)
		{
			var replyFile = string.Format("{0}.{1}.Reply", client, _channelName);

			_serverChannel.Send(replyFile, message);
		}


		public void Open()
		{
			_serverChannel.Open();
		}

		public void Close()
		{
			_serverChannel.Close();
		}


		public void Dispose()
		{
			_serverChannel.Dispose();
		}
	}
}