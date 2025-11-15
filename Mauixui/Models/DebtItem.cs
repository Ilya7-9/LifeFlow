using SQLite;
using System;

namespace Mauixui.Models
{
    [Table("DebtItem")]
    public class DebtItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; } = "";

        public string Party { get; set; } = ""; // кому или кто должен
        public string Type { get; set; } = "Займ"; // кредит/займ/ипотека и т.д.
        public decimal Amount { get; set; } = 0m;
        public DateTime DueDate { get; set; } = DateTime.Now;
        public double InterestPercent { get; set; } = 0.0; // опционально
        public string Notes { get; set; } = "";

        public string Direction { get; set; } = "Я должен";
        // или "Мне должны"

    }
}
