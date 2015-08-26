namespace System.IO
{
	public abstract class BaseFileChannelHandler : IFileChannelHandler
	{
		public abstract object Handle(object request);

		public virtual void OnSuccess(object request, object reply)
		{
		}

		public virtual void OnError(object request, Exception error)
		{
		}
	}
}