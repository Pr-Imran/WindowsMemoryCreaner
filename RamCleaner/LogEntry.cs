using System;

namespace RamCleaner
{
    public class LogEntry
    {
        public string Time { get; set; }
        public string Message { get; set; }

        public LogEntry(string message)
        {
            Time = DateTime.Now.ToString("HH:mm:ss");
            Message = message;
        }
    }
}
