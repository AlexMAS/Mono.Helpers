namespace System.IO
{
    public interface IFileChannelHandler
    {
        object Handle(object request);

        void OnSuccess(object request, object reply);

        void OnError(object request, Exception error);
    }
}