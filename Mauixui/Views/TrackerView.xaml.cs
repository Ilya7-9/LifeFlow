using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Mauixui.Views
{
    public partial class TrackerView : ContentView
    {
        private System.Timers.Timer _uiUpdateTimer;

        public TrackerView()
        {
            InitializeComponent();

            InitializeTracker();
            SetupUITimer();
        }

        private void InitializeTracker()
        {
            TrackerService.EnsureStarted();

            var tracker = TrackerService.Tracker;
            tracker.OnAppUsageRecorded += OnAppUsageRecorded;
            tracker.OnWebsiteUsageRecorded += OnWebsiteUsageRecorded;
            tracker.OnAppUsageUpdated += OnAppUsageUpdated;
            tracker.OnWebsiteUsageUpdated += OnWebsiteUsageUpdated;

            UpdateStatus("✅ Трекер активен");
            UpdateStats();
        }

        private void SetupUITimer()
        {
            _uiUpdateTimer = new System.Timers.Timer(2000);
            _uiUpdateTimer.Elapsed += (s, e) => UpdateUI();
            _uiUpdateTimer.AutoReset = true;
            _uiUpdateTimer.Start();
        }

        private void UpdateUI()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateStats();
                UpdateCurrentActivity();
                UpdateProfileStats();
            });
        }

        private void OnAppUsageRecorded(AppUsageRecord record)
        {
            UpdateProfileStats();
        }

        private void OnWebsiteUsageRecorded(WebsiteUsageRecord record)
        {
            UpdateProfileStats();
        }

        private void OnAppUsageUpdated(string appName, TimeSpan duration)
        {
            UpdateCurrentActivity();
        }

        private void OnWebsiteUsageUpdated(string website, TimeSpan duration)
        {
            UpdateCurrentActivity();
        }

        private void UpdateStats()
        {
            try
            {
                var todayAppUsage = TrackerService.GetTodayAppUsage();
                var todayWebsiteUsage = TrackerService.GetTodayWebsiteUsage();
                var totalTime = TrackerService.GetTotalTrackedTime(); // ИСПОЛЬЗУЕМ СИНХРОННЫЙ МЕТОД

                if (TotalTimeLabel != null)
                    TotalTimeLabel.Text = $"Общее время: {totalTime:hh\\:mm\\:ss}";

                if (AppCountLabel != null)
                    AppCountLabel.Text = $"Приложений: {todayAppUsage.Select(r => r.AppName).Distinct().Count()}";

                if (WebsiteCountLabel != null)
                    WebsiteCountLabel.Text = $"Сайтов: {todayWebsiteUsage.Select(r => r.Website).Distinct().Count()}";

                // Топ приложений
                var topApps = todayAppUsage
                    .GroupBy(r => r.AppName)
                    .Select(g => new { App = g.Key, Time = TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)) })
                    .OrderByDescending(x => x.Time)
                    .Take(5)
                    .ToList();

                if (TopAppsStack != null)
                {
                    TopAppsStack.Children.Clear();
                    foreach (var app in topApps)
                    {
                        TopAppsStack.Children.Add(new Label
                        {
                            Text = $"{app.App}: {app.Time:hh\\:mm\\:ss}",
                            TextColor = Color.FromArgb("#CCCCCC"),
                            FontSize = 12
                        });
                    }
                }

                // Топ сайтов
                var topWebsites = todayWebsiteUsage
                    .GroupBy(r => r.Website)
                    .Select(g => new { Site = g.Key, Time = TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)) })
                    .OrderByDescending(x => x.Time)
                    .Take(5)
                    .ToList();

                if (TopWebsitesStack != null)
                {
                    TopWebsitesStack.Children.Clear();
                    foreach (var site in topWebsites)
                    {
                        TopWebsitesStack.Children.Add(new Label
                        {
                            Text = $"{site.Site}: {site.Time:hh\\:mm\\:ss}",
                            TextColor = Color.FromArgb("#CCCCCC"),
                            FontSize = 12
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating stats: {ex.Message}");
            }
        }

        private void UpdateCurrentActivity()
        {
            try
            {
                // ИСПОЛЬЗУЕМ НОВЫЕ МЕТОДЫ ИЗ TrackerService
                var (currentApp, appTime) = TrackerService.GetCurrentAppActivity();
                var (currentWebsite, websiteTime) = TrackerService.GetCurrentWebsiteActivity();

                if (CurrentActivityLabel != null)
                {
                    if (!string.IsNullOrEmpty(currentApp) && currentApp != "Неизвестно")
                    {
                        CurrentActivityLabel.Text = $"Сейчас: {currentApp} ({appTime:mm\\:ss})";

                        if (!string.IsNullOrEmpty(currentWebsite) && currentWebsite != "Неизвестно" && websiteTime.TotalSeconds > 5)
                        {
                            CurrentActivityLabel.Text += $"\nСайт: {currentWebsite} ({websiteTime:mm\\:ss})";
                        }
                    }
                    else
                    {
                        CurrentActivityLabel.Text = "Активность появится здесь...";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating current activity: {ex.Message}");
            }
        }

        private void UpdateProfileStats()
        {
            try
            {
                var totalTrackedTime = TrackerService.GetTotalTrackedTime(); // ИСПОЛЬЗУЕМ СИНХРОННЫЙ МЕТОД

                if (Application.Current?.MainPage is MainPage mainPage)
                {
                    mainPage.UpdateProfileStatistics(0, 0, totalTrackedTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile stats: {ex.Message}");
            }
        }

        private void UpdateStatus(string status)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (StatusLabel != null)
                    StatusLabel.Text = status;
            });
        }

        private void OnStartTrackingClicked(object sender, EventArgs e)
        {
            TrackerService.Tracker.StartTracking();
            UpdateStatus("✅ Трекер активен");
        }

        private void OnStopTrackingClicked(object sender, EventArgs e)
        {
            TrackerService.Tracker.StopTracking();
            UpdateStatus("⏹️ Трекер остановлен");
        }

        private async void OnShowStatsClicked(object sender, EventArgs e)
        {
            var todayAppUsage = TrackerService.GetTodayAppUsage();
            var todayWebsiteUsage = TrackerService.GetTodayWebsiteUsage();

            string stats = $"📊 Статистика за {DateTime.Today:dd.MM.yyyy}\n\n";

            stats += "📱 Топ приложений:\n";
            var topApps = todayAppUsage
                .GroupBy(r => r.AppName)
                .Select(g => new { App = g.Key, Time = TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)) })
                .OrderByDescending(x => x.Time)
                .Take(10);

            foreach (var app in topApps)
            {
                stats += $"   {app.App}: {app.Time:hh\\:mm\\:ss}\n";
            }

            stats += "\n🌐 Топ сайтов:\n";
            var topWebsites = todayWebsiteUsage
                .GroupBy(r => r.Website)
                .Select(g => new { Site = g.Key, Time = TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)) })
                .OrderByDescending(x => x.Time)
                .Take(10);

            foreach (var site in topWebsites)
            {
                stats += $"   {site.Site}: {site.Time:hh\\:mm\\:ss}\n";
            }

            await DisplayAlert("Детальная статистика", stats, "OK");
        }

        private void OnUnloaded(object sender, EventArgs e)
        {
            _uiUpdateTimer?.Stop();
            _uiUpdateTimer?.Dispose();
        }

        private async System.Threading.Tasks.Task DisplayAlert(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }
}