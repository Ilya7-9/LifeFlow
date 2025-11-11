using SQLite;

namespace Mauixui.Models
{
    [Table("CategoryItem")]
    public class CategoryItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }

        public string Name { get; set; } // название категории
        public string Type { get; set; } // Доход / Расход
    }
}
