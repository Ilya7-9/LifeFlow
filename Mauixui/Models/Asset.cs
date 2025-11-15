using SQLite;
using System;

namespace Mauixui.Models
{
    [Table("AssetItem")]
    public class AssetItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; } = "";

        public string Name { get; set; } = "";
        public string Category { get; set; } = "Другое"; // наличные, техника, авто, инвестиции, крипта...
        public decimal Value { get; set; } = 0m;
        public DateTime DateAcquired { get; set; } = DateTime.Now;
        public string Notes { get; set; } = "";
    }
}
