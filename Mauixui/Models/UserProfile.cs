public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Avatar { get; set; } = "👤";

    // ДОБАВЬТЕ ЭТИ ПОЛЯ:
    public string Email { get; set; }
    public string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastLogin { get; set; } = DateTime.Now;
    public bool IsActive { get; set; }

    public AppTheme Theme { get; set; } = AppTheme.Unspecified;
    public string AccentColor { get; set; } = "#5865F2";

    public int TotalTasks { get; set; }
    public int TotalNotes { get; set; }
    public TimeSpan TotalTrackedTime { get; set; }
}