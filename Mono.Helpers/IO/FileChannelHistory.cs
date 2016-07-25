using System.Collections.Concurrent;

namespace System.IO
{
    internal sealed class FileChannelHistory
    {
        private readonly ConcurrentDictionary<string, DateTime> _history
            = new ConcurrentDictionary<string, DateTime>();


        public bool IsChanged(string filePath)
        {
            var lastWriteTime = File.GetLastWriteTime(filePath);

            DateTime lastReadTime;

            if (_history.TryGetValue(filePath, out lastReadTime))
            {
                if (!DateTimeEquals(lastWriteTime, lastReadTime))
                {
                    _history.TryUpdate(filePath, lastWriteTime, lastReadTime);

                    return true;
                }
            }
            else
            {
                _history.TryAdd(filePath, lastWriteTime);

                return true;
            }

            return false;
        }


        private static bool DateTimeEquals(DateTime left, DateTime right)
        {
            return left.Year == right.Year
                   && left.Month == right.Month
                   && left.Hour == right.Hour
                   && left.Minute == right.Minute
                   && left.Second == right.Second;
        }
    }
}