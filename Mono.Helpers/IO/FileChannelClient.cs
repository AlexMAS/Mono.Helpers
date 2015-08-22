using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace System.IO
{
	public sealed class FileChannelClient : IDisposable
	{
		public static readonly TimeSpan DefaultInvokeTimeout = TimeSpan.FromMinutes(1);


		public FileChannelClient(string channel, string client = null, string directory = null, IFileChannelFormatter formatter = null)
		{
			if (string.IsNullOrEmpty(channel))
			{
				throw new ArgumentNullException("channel");
			}

			if (string.IsNullOrEmpty(client))
			{
				client = Guid.NewGuid().ToString("N");
			}

			if (string.IsNullOrEmpty(directory))
			{
				directory = Path.GetTempPath();
			}

			if (formatter == null)
			{
				formatter = JsonFileChannelFormatter.Instance;
			}

			InvokeTimeout = DefaultInvokeTimeout;

			_client = client;
			_dispatcher = new FileChannelClientDispatcher(directory, client, channel, formatter, OnReceiveReplyMessage);
			_requests = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
		}


		private readonly string _client;
		private readonly FileChannelClientDispatcher _dispatcher;
		private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _requests;


		public TimeSpan InvokeTimeout { get; set; }


		public object Invoke(string action, object arguments = null)
		{
			if (string.IsNullOrEmpty(action))
			{
				throw new ArgumentNullException(action);
			}

			var requestId = Guid.NewGuid().ToString("N");

			var request = new RequestMessage
				{
					ClientName = _client,
					RequestId = requestId,
					Action = action,
					Arguments = arguments
				};

			var requestResult = new TaskCompletionSource<object>();

			_requests.TryAdd(requestId, requestResult);

			lock (this)
			{
				_dispatcher.Open();
				_dispatcher.Request(request);

				if (requestResult.Task.Wait(InvokeTimeout))
				{
					if (requestResult.Task.Exception == null)
					{
						return requestResult.Task.Result;
					}

					throw requestResult.Task.Exception;
				}

				_requests.TryRemove(requestId, out requestResult);

				throw new TimeoutException();
			}
		}


		private void OnReceiveReplyMessage(dynamic reply)
		{
			string requestId = reply.RequestId;

			TaskCompletionSource<object> requestResult;

			if (_requests.TryGetValue(requestId, out requestResult))
			{
				try
				{
					if (reply.IsHandled == false)
					{
						requestResult.TrySetException(new NotSupportedException());
					}
					else if (reply.IsFaulted == true)
					{
						string errorMessage = reply.Exception;
						requestResult.TrySetException(new InvalidOperationException(errorMessage));
					}
					else
					{
						requestResult.TrySetResult(reply.Result);
					}
				}
				finally
				{
					_requests.TryRemove(requestId, out requestResult);
				}
			}
		}


		public void Dispose()
		{
			_requests.Clear();
			_dispatcher.Dispose();
		}


		internal sealed class RequestMessage
		{
			public string ClientName { get; set; }
			public string RequestId { get; set; }
			public string Action { get; set; }
			public object Arguments { get; set; }
		}
	}
}