using SQLite;

namespace Mauixui.Models
{
    [Table("Subtask")]
    public class Subtask
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Indexed]
        public string TaskItemId { get; set; }

        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}