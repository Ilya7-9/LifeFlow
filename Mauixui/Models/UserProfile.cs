using System;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

namespace Mauixui.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Avatar { get; set; } = "👤";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; }

        public AppTheme Theme { get; set; } = AppTheme.Unspecified;
        public string AccentColor { get; set; } = "#5865F2";

        public int TotalTasks { get; set; }
        public int TotalNotes { get; set; }
        public TimeSpan TotalTrackedTime { get; set; }
    }

    public class ProfileManager
    {
        public List<UserProfile> Profiles { get; set; } = new();
        public UserProfile CurrentProfile { get; set; }
    }
}