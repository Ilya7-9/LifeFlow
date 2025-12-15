using SQLite;

[Table("FinanceItems")]
public class FinanceItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string ProfileId { get; set; }

    public string Type { get; set; } // "Доход" или "Расход"
    public string Category { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; }
    public bool IsRecurring { get; set; }
}