using SQLite;

[Table("Categories")]
public class CategoryItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string ProfileId { get; set; }

    public bool IsSelected { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // Доход / Расход
    public string Budget { get; set; }
}