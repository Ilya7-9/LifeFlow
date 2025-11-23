using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;

namespace Mauixui.Models
{
    public class UserProfile : INotifyPropertyChanged
    {
        private string _name;
        private string _avatar;
        private string _email;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Avatar
        {
            get => _avatar;
            set
            {
                _avatar = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLogin { get; set; } = DateTime.Now;
        public bool IsActive { get; set; }

        public AppTheme Theme { get; set; } = AppTheme.Unspecified;
        public string AccentColor { get; set; } = "#5865F2";

        public int TotalTasks { get; set; }
        public int TotalNotes { get; set; }
        public TimeSpan TotalTrackedTime { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}