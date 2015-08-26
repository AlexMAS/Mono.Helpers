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
				throw new ArgumentNullException("handler");
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

			if (_onSuccess != null)
			{
				_onSuccess(request, reply);
			}
		}

		public override void OnError(object request, Exception error)
		{
			base.OnError(request, error);

			if (_onError != null)
			{
				_onError(request, error);
			}
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