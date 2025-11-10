using SQLite;

namespace Mauixui.Models
{
    [Table("TaskItem")]
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ProfileId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFavorite { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }

        public string Source { get; set; }

        [Ignore]
        public List<Subtask> Subtasks { get; set; } = new List<Subtask>();

        [Ignore]
        public bool ShowSubtasks { get; set; }

        [Ignore]
        public bool ShowSubtaskInput { get; set; }
    }
}