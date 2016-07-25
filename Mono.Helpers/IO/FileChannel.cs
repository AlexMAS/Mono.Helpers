using System.Threading;

namespace System.IO
{
    public sealed class FileChannel : IDisposable
    {
        public const int MaxReceiveMessageAttempts = 5;


        public FileChannel(string directory, string channelFileMask, IFileChannelFormatter channelFormatter, Action<object> onReceiveMessage)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (string.IsNullOrEmpty(channelFileMask))
            {
                throw new ArgumentNullException(nameof(channelFileMask));
            }

            if (channelFormatter == null)
            {
                throw new ArgumentNullException(nameof(channelFormatter));
            }

            if (onReceiveMessage == null)
            {
                throw new ArgumentNullException(nameof(onReceiveMessage));
            }

            _directory = directory;
            _channelFileMask = channelFileMask;
            _channelFormatter = channelFormatter;
            _onReceiveMessage = onReceiveMessage;
            _channelHistory = new FileChannelHistory();
        }


        private readonly string _directory;
        private readonly string _channelFileMask;
        private readonly IFileChannelFormatter _channelFormatter;
        private readonly Action<object> _onReceiveMessage;
        private readonly FileChannelHistory _channelHistory;

        private volatile FileSystemWatcher _watcher;
        private readonly object _watcherSync = new object();


        public void Send(string channelFile, object message)
        {
            using (var stream = File.Create(Path.Combine(_directory, channelFile)))
            {
                _channelFormatter.Write(stream, message);
            }
        }


        public void Open()
        {
            if (_watcher == null)
            {
                lock (_watcherSync)
                {
                    if (_watcher == null)
                    {
                        var watcher = new FileSystemWatcher
                        {
                            Path = _directory,
                            Filter = _channelFileMask,
                            NotifyFilter = NotifyFilters.LastWrite,
                            IncludeSubdirectories = false
                        };

                        watcher.Changed += (s, e) =>
                        {
                            if (_channelHistory.IsChanged(e.FullPath))
                            {
                                // LastWriteTime doesn't contain ms :(
                                Thread.Sleep(1000);

                                try
                                {
                                    OnChannelFileChanged(e.FullPath);
                                }
                                catch
                                {
                                }
                            }
                        };

                        watcher.EnableRaisingEvents = true;

                        _watcher = watcher;
                    }
                }
            }
        }

        public void Close()
        {
            if (_watcher != null)
            {
                lock (_watcherSync)
                {
                    if (_watcher != null)
                    {
                        try
                        {
                            _watcher.EnableRaisingEvents = false;
                            _watcher.Dispose();
                        }
                        finally
                        {
                            _watcher = null;
                        }
                    }
                }
            }
        }


        public void Dispose()
        {
            Close();
        }


        private void OnChannelFileChanged(string channelFile)
        {
            for (int i = 0; i < MaxReceiveMessageAttempts; i++)
            {
                if (TryReceiveMessage(channelFile))
                {
                    break;
                }

                Thread.Sleep(1000);
            }
        }

        private bool TryReceiveMessage(string channelFile)
        {
            try
            {
                using (var stream = File.OpenRead(channelFile))
                {
                    var message = _channelFormatter.Read(stream);

                    _onReceiveMessage(message);
                }

                return true;
            }
            catch
            {
            }

            return false;
        }
    }
}