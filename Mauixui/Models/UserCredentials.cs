using SQLite;

namespace Mauixui.Models
{
    [Table("UserCredentials")]
    public class UserCredentials
    {
        [PrimaryKey]
        public string ProfileId { get; set; } // Связь с профилем

        public string Email { get; set; }
        public string PasswordHash { get; set; } // Пароль в открытом виде
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}