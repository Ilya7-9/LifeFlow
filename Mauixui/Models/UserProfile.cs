using SQLite;
using System;

namespace Mauixui.Models
{
    [Table("UserProfiles")]
    public class UserProfile
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Unique]
        public string Email { get; set; }

        public string Name { get; set; }
        public string Avatar { get; set; } = "👤";
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLogin { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Исправляем на string
        public string Theme { get; set; } = "Unspecified";

        public string AccentColor { get; set; } = "#5865F2";
        public long TotalTrackedSeconds { get; set; } = 0;

        [Ignore]
        public TimeSpan TotalTrackedTime => TimeSpan.FromSeconds(TotalTrackedSeconds);

        // Метод для проверки пароля
        public bool CheckPassword(string inputPassword)
        {
            return Password == inputPassword;
        }

        // Метод для получения AppTheme из string
        [Ignore]
        public AppTheme AppTheme
        {
            get => Theme switch
            {
                "Dark" => AppTheme.Dark,
                "Light" => AppTheme.Light,
                _ => AppTheme.Unspecified
            };
            set => Theme = value switch
            {
                AppTheme.Dark => "Dark",
                AppTheme.Light => "Light",
                _ => "Unspecified"
            };
        }
    }
}