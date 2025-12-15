using SQLite;

[Table("Debts")]
public class DebtItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string ProfileId { get; set; }

    public string Party { get; set; } = "";
    public string Type { get; set; } = "Займ";
    public decimal Amount { get; set; } = 0m;
    public DateTime DueDate { get; set; } = DateTime.Now;
    public double InterestPercent { get; set; } = 0.0;
    public string Notes { get; set; } = "";
    public string Direction { get; set; } = "Я должен";
}