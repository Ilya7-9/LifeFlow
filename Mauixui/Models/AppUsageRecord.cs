using System;

namespace Mauixui.Models
{
    public class AppUsageRecord
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string Category { get; set; }
    }

    public class WebsiteUsageRecord
    {
        public int Id { get; set; }
        public string Website { get; set; }
        public string Url { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string Category { get; set; }
    }

    public class ActiveWindowInfo
    {
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public uint ProcessId { get; set; }
    }
}