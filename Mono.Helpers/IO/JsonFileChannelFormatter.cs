using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace System.IO
{
    public sealed class JsonFileChannelFormatter : IFileChannelFormatter
    {
        public static readonly JsonFileChannelFormatter Instance = new JsonFileChannelFormatter();


        public object Read(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            using (var jReader = new JsonTextReader(reader))
            {
                return JToken.Load(jReader);
            }
        }

        public void Write(Stream stream, object value)
        {
            using (var writer = new StreamWriter(stream))
            using (var jWriter = new JsonTextWriter(writer))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jWriter, value);
                jWriter.Flush();
            }
        }
    }
}