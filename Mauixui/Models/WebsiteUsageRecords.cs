using SQLite;

[Table("WebsiteUsageRecords")]
public class WebsiteUsageRecord
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed]
    public string ProfileId { get; set; }

    public string Website { get; set; }
    public string Url { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Category { get; set; }

    [Ignore]
    public TimeSpan Duration => EndTime - StartTime;
}