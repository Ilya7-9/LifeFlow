using SQLite;

namespace Mauixui.Models
{
    [Table("BudgetItem")]
    public class BudgetItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }
        public string Category { get; set; }
        public double Limit { get; set; }
        public double Spent { get; set; }
        public string Period { get; set; }   // week / month / year
        public DateTime CreatedAt { get; set; }
        public DateTime ResetDate { get; set; }

        [Ignore]
        public double Progress => Limit == 0 ? 0 : Spent / Limit;

        [Ignore]
        public bool IsExceeded => Spent > Limit;
    }
}
