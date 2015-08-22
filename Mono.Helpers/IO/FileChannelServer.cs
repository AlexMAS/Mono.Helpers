using System.Collections.Generic;

namespace System.IO
{
	public sealed class FileChannelServer : IDisposable
	{
		public FileChannelServer(string channel, string directory = null, IFileChannelFormatter formatter = null)
		{
			if (string.IsNullOrEmpty(channel))
			{
				throw new ArgumentNullException("channel");
			}

			if (string.IsNullOrEmpty(directory))
			{
				directory = Path.GetTempPath();
			}

			if (formatter == null)
			{
				formatter = JsonFileChannelFormatter.Instance;
			}

			_dispatcher = new FileChannelServerDispatcher(directory, channel, formatter, OnReciveRequestMessage);
			_handlers = new Dictionary<string, Func<object, object>>();
		}


		private readonly FileChannelServerDispatcher _dispatcher;
		private readonly Dictionary<string, Func<object, object>> _handlers;


		public FileChannelServer Subscribe(string action, Func<object, object> handler)
		{
			if (string.IsNullOrEmpty(action))
			{
				throw new ArgumentNullException("action");
			}

			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}

			_handlers[action] = handler;

			return this;
		}


		public void Start()
		{
			_dispatcher.Open();
		}

		public void Stop()
		{
			_dispatcher.Close();
		}


		public void Dispose()
		{
			_dispatcher.Dispose();
		}


		private void OnReciveRequestMessage(dynamic request)
		{
			var reply = new ReplyMessage
			{
				ClientName = request.ClientName,
				RequestId = request.RequestId,
				Action = request.Action
			};

			Func<object, object> handler;

			if (_handlers.TryGetValue(reply.Action, out handler))
			{
				try
				{
					reply.Result = handler(request.Arguments);
				}
				catch (Exception error)
				{
					reply.Exception = error.Message;
					reply.IsFaulted = true;
				}

				reply.IsHandled = true;
			}

			_dispatcher.Reply(reply.ClientName, reply);
		}


		internal sealed class ReplyMessage
		{
			public string ClientName { get; set; }
			public string RequestId { get; set; }
			public string Action { get; set; }
			public object Result { get; set; }
			public string Exception { get; set; }
			public bool IsHandled { get; set; }
			public bool IsFaulted { get; set; }
		}
	}
}