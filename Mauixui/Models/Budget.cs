using SQLite;
using System;

namespace Mauixui.Models
{
    [Table("Budget")]
    public class Budget
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }

        public string Name { get; set; }
        public decimal Limit { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal CurrentSpending { get; set; }
    }
}
