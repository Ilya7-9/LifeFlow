using SQLite;

namespace Mauixui.Models
{
    [Table("FinanceItem")]
    public class FinanceItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }

        public string Type { get; set; } // "Доход" или "Расход"
        public string Category { get; set; } // Еда, Транспорт, Зарплата и т.д.
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        public string PaymentMethod { get; set; } // Наличные, Карта, Счёт
        public bool IsRecurring { get; set; } // Повторяющаяся операция?
    }
}
