using SQLite;

[Table("AppUsageRecords")]
public class AppUsageRecord
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed] // Добавляем индекс для ускорения запросов
    public string ProfileId { get; set; }

    public string AppName { get; set; }
    public string WindowTitle { get; set; }
    public string ProcessName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Category { get; set; }

    [Ignore]
    public TimeSpan Duration => EndTime - StartTime;
}

namespace Models
{
    public class ActiveWindowInfo
    {
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public uint ProcessId { get; set; }
    }
}