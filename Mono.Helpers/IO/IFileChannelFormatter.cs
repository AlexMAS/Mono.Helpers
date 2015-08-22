namespace System.IO
{
	public interface IFileChannelFormatter
	{
		object Read(Stream stream);

		void Write(Stream stream, object value);
	}
}