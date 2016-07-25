namespace System.IO
{
    public static class FileChannelExtensions
    {
        public static FileChannelServer Subscribe(this FileChannelServer target,
                                                  string action,
                                                  Action<object> handler,
                                                  Action<object, object> onSuccess = null,
                                                  Action<object, Exception> onError = null)
        {
            return target.Subscribe(action, new DelegateFileChannelHandler(handler, onSuccess, onError));
        }

        public static FileChannelServer Subscribe(this FileChannelServer target,
                                                  string action,
                                                  Func<object, object> handler,
                                                  Action<object, object> onSuccess = null,
                                                  Action<object, Exception> onError = null)
        {
            return target.Subscribe(action, new DelegateFileChannelHandler(handler, onSuccess, onError));
        }
    }
}