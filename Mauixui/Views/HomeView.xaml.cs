using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Timers;

namespace Mauixui.Views
{
    public partial class HomeView : ContentView
    {
        private ProfileService _profileService;
        private System.Timers.Timer _refreshTimer;

        public HomeView()
        {
            InitializeComponent();
            _profileService = new ProfileService();

            _ = UpdateAllStatistics();
            SetupRefreshTimer();
        }

        private async System.Threading.Tasks.Task UpdateAllStatistics()
        {
            await _profileService.UpdateAllProfilesStatsAsync();
            LoadProfileStatistics();
        }

        private void SetupRefreshTimer()
        {
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            RefreshStatistics();
        }

        private void RefreshStatistics()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                LoadProfileStatistics();
            });
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            if (Parent != null)
            {
                _ = UpdateAllStatistics();
            }
            else
            {
                _refreshTimer?.Stop();
            }
        }

        private void LoadProfileStatistics()
        {
            try
            {
                var currentProfile = _profileService.GetCurrentProfile();

                Device.BeginInvokeOnMainThread(() =>
                {
                    if (ProfileNameLabel != null)
                        ProfileNameLabel.Text = currentProfile.Name;

                    if (ProfileAvatarLabel != null)
                        ProfileAvatarLabel.Text = currentProfile.Avatar;

                    if (TasksCountLabel != null)
                        TasksCountLabel.Text = currentProfile.TotalTasks.ToString();

                    if (NotesCountLabel != null)
                        NotesCountLabel.Text = currentProfile.TotalNotes.ToString();

                    var time = currentProfile.TotalTrackedTime;
                    string timeText = FormatTime(time);

                    if (TrackedTimeLabel != null)
                        TrackedTimeLabel.Text = timeText;

                    var productivity = CalculateProductivity(currentProfile);
                    if (ProductivityLabel != null)
                        ProductivityLabel.Text = $"{productivity}%";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile stats in HomeView: {ex.Message}");
            }
        }

        private string FormatTime(TimeSpan time)
        {
            try
            {
                if (time.TotalHours >= 1)
                {
                    return $"{(int)time.TotalHours}ч {time.Minutes}м";
                }
                else if (time.TotalMinutes >= 1)
                {
                    return $"{time.Minutes}м {time.Seconds}с";
                }
                else
                {
                    return $"{time.Seconds}с";
                }
            }
            catch (Exception)
            {
                return "0с";
            }
        }

        private async void OnShowDetailedStats(object sender, EventArgs e)
        {
            try
            {
                var currentProfile = _profileService.GetCurrentProfile();

                var timeText = FormatTimeForStats(currentProfile.TotalTrackedTime);
                var productivity = CalculateProductivity(currentProfile);

                var stats = $@"📊 Детальная статистика

👤 Профиль: {currentProfile.Name}
📅 Создан: {currentProfile.CreatedAt:dd.MM.yyyy}

📈 Активность:
• Задач: {currentProfile.TotalTasks}
• Заметок: {currentProfile.TotalNotes}
• Время: {timeText}

🎯 Продуктивность: {productivity}%";

                await DisplayAlert("Детальная статистика", stats, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить статистику: {ex.Message}", "OK");
            }
        }

        private string FormatTimeForStats(TimeSpan time)
        {
            try
            {
                if (time.TotalHours >= 1)
                {
                    return $"{(int)time.TotalHours} часов {time.Minutes} минут";
                }
                else if (time.TotalMinutes >= 1)
                {
                    return $"{time.Minutes} минут {time.Seconds} секунд";
                }
                else
                {
                    return $"{time.Seconds} секунд";
                }
            }
            catch (Exception)
            {
                return "0 секунд";
            }
        }

        private int CalculateProductivity(UserProfile profile)
        {
            try
            {
                var baseScore = 50;

                if (profile.TotalTasks > 0) baseScore += 10;
                if (profile.TotalNotes > 0) baseScore += 10;
                if (profile.TotalTrackedTime.TotalHours > 1) baseScore += 10;

                return Math.Min(baseScore, 100);
            }
            catch (Exception)
            {
                return 50;
            }
        }

        private async System.Threading.Tasks.Task DisplayAlert(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }
}