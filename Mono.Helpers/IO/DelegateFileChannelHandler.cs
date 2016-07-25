namespace System.IO
{
    public sealed class DelegateFileChannelHandler : BaseFileChannelHandler
    {
        public DelegateFileChannelHandler(Action<object> handler, Action<object, object> onSuccess = null, Action<object, Exception> onError = null)
            : this(ActionToFunc(handler), onSuccess, onError)
        {
        }

        public DelegateFileChannelHandler(Func<object, object> handler, Action<object, object> onSuccess = null, Action<object, Exception> onError = null)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handler = handler;
            _onSuccess = onSuccess;
            _onError = onError;
        }


        private readonly Func<object, object> _handler;
        private readonly Action<object, object> _onSuccess;
        private readonly Action<object, Exception> _onError;


        public override object Handle(object request)
        {
            return _handler(request);
        }

        public override void OnSuccess(object request, object reply)
        {
            base.OnSuccess(request, reply);

            _onSuccess?.Invoke(request, reply);
        }

        public override void OnError(object request, Exception error)
        {
            base.OnError(request, error);

            _onError?.Invoke(request, error);
        }


        private static Func<object, object> ActionToFunc(Action<object> action)
        {
            return request =>
            {
                action(request);
                return null;
            };
        }
    }
}