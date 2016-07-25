namespace System.IO
{
    internal sealed class FileChannelClientDispatcher : IDisposable
    {
        public FileChannelClientDispatcher(string directory, string clientName, string channelName, IFileChannelFormatter channelFormatter, Action<object> onReceiveReplyMessage)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (string.IsNullOrEmpty(clientName))
            {
                throw new ArgumentNullException(nameof(clientName));
            }

            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }

            if (channelFormatter == null)
            {
                throw new ArgumentNullException(nameof(channelFormatter));
            }

            if (onReceiveReplyMessage == null)
            {
                throw new ArgumentNullException(nameof(onReceiveReplyMessage));
            }

            _directory = directory;
            _requestFile = $"{clientName}.{channelName}.Request";
            _replyFile = $"{clientName}.{channelName}.Reply";
            _clientChannel = new FileChannel(directory, _replyFile, channelFormatter, onReceiveReplyMessage);
        }


        private readonly string _directory;
        private readonly string _requestFile;
        private readonly string _replyFile;
        private readonly FileChannel _clientChannel;


        public void Request(object message)
        {
            _clientChannel.Send(_requestFile, message);
        }


        public void Open()
        {
            _clientChannel.Open();
        }

        public void Close()
        {
            _clientChannel.Close();
        }


        public void Dispose()
        {
            _clientChannel.Dispose();

            try
            {
                var requestFilePath = Path.Combine(_directory, _requestFile);

                if (File.Exists(requestFilePath))
                {
                    File.Delete(requestFilePath);
                }
            }
            catch
            {
            }

            try
            {
                var replyFilePath = Path.Combine(_directory, _replyFile);

                if (File.Exists(replyFilePath))
                {
                    File.Delete(replyFilePath);
                }
            }
            catch
            {
            }
        }
    }
}