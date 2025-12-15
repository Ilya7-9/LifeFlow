using SQLite;

[Table("DailyStats")]
public class DailyStat
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string ProfileId { get; set; }

    public DateTime Date { get; set; }
    public long TotalSeconds { get; set; }
    public string TopApp { get; set; }
    public string TopSite { get; set; }
    public string AppsJson { get; set; }
    public string SitesJson { get; set; }
}