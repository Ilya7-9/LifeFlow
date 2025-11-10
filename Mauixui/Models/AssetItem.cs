using SQLite;

namespace Mauixui.Models
{
    [Table("Asset")]
    public class Asset
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "Актив" или "Обязательство"
        public decimal Value { get; set; }
        public string Description { get; set; }
    }
}
