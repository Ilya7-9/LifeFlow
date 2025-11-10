using SQLite;

namespace Mauixui.Models
{
    [Table("Budget")]
    public class Budget
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Limit { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Ignore]
        public decimal CurrentSpending { get; set; }
    }
}
