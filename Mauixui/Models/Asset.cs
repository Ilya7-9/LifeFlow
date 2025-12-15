using SQLite;

[Table("Assets")]
public class AssetItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string ProfileId { get; set; }

    public string Name { get; set; } = "";
    public string Category { get; set; } = "Другое";
    public decimal Value { get; set; } = 0m;
    public DateTime DateAcquired { get; set; } = DateTime.Now;
    public string Notes { get; set; } = "";
}