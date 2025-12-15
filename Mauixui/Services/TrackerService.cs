using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Models;

namespace Mauixui.Services
{
    public class TrackerService
    {
        private static TrackerService _instance;
        private static WindowsActivityTracker _tracker;
        public static bool _isInitialized = false;
        private static MainDatabase _database;
        private static string _currentProfileId;

        public static TimeSpan LiveTotalTime { get; private set; } = TimeSpan.Zero;
        public static TrackerService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TrackerService();
                }
                return _instance;
            }
        }
        public static WindowsActivityTracker Tracker => _tracker;

        public static void Initialize(string currentProfileId)
        {
            _database = MainDatabase.Instance;
            _currentProfileId = currentProfileId;

            if (!_isInitialized)
            {
                _tracker = new WindowsActivityTracker();
                _tracker.OnAppUsageRecorded += OnAppUsageRecorded;
                _tracker.OnWebsiteUsageRecorded += OnWebsiteUsageRecorded;
                _isInitialized = true;
            }
        }

        public static void EnsureStarted()
        {
            var tracker = Tracker;
            if (tracker != null && !tracker.IsTracking)
            {
                tracker.StartTracking();
            }
        }

        public static void SetCurrentProfile(string profileId)
        {
            _currentProfileId = profileId;
        }

        private static async void OnAppUsageRecorded(AppUsageRecord record)
        {
            record.ProfileId = _currentProfileId;
            await _database.SaveAppUsageAsync(record);
            await UpdateTodayStatsAsync();
        }

        private static async void OnWebsiteUsageRecorded(WebsiteUsageRecord record)
        {
            record.ProfileId = _currentProfileId;
            await _database.SaveWebsiteUsageAsync(record);
            await UpdateTodayStatsAsync();
        }

        private static async Task UpdateTodayStatsAsync()
        {
            var todayStats = await _database.GetTodayStatsAsync(_currentProfileId);

            var todayApps = await _database.GetTodayAppUsageAsync(_currentProfileId);
            var todayWebsites = await _database.GetTodayWebsiteUsageAsync(_currentProfileId);

            // Обновляем статистику
            double totalSeconds = 0;
            foreach (var app in todayApps)
                totalSeconds += (app.EndTime - app.StartTime).TotalSeconds;

            foreach (var site in todayWebsites)
                totalSeconds += (site.EndTime - site.StartTime).TotalSeconds;

            todayStats.TotalSeconds = (long)totalSeconds;

            // Обновляем топ приложение и сайт
            var appGroups = todayApps.GroupBy(a => a.AppName);
            var topAppGroup = appGroups.OrderByDescending(g => g.Sum(a => (a.EndTime - a.StartTime).TotalSeconds)).FirstOrDefault();
            todayStats.TopApp = topAppGroup?.Key ?? "Нет данных";

            var siteGroups = todayWebsites.GroupBy(w => w.Website);
            var topSiteGroup = siteGroups.OrderByDescending(g => g.Sum(w => (w.EndTime - w.StartTime).TotalSeconds)).FirstOrDefault();
            todayStats.TopSite = topSiteGroup?.Key ?? "Нет данных";

            await _database.UpdateDailyStatAsync(todayStats);

            // Обновляем общее время в профиле
            await _database.UpdateProfileTrackedTimeAsync(_currentProfileId);

            LiveTotalTime = TimeSpan.FromSeconds(totalSeconds);
        }

        public static async Task<List<AppUsageRecord>> GetTodayAppUsageAsync()
        {
            return await _database.GetTodayAppUsageAsync(_currentProfileId);
        }

        public static async Task<List<WebsiteUsageRecord>> GetTodayWebsiteUsageAsync()
        {
            return await _database.GetTodayWebsiteUsageAsync(_currentProfileId);
        }

        public static async Task<DailyStat> GetTodayStatsAsync()
        {
            if (_database == null)
                throw new InvalidOperationException("TrackerService not initialized. Call Initialize() first.");
            if (string.IsNullOrEmpty(_currentProfileId))
                throw new InvalidOperationException("Profile ID not set. Call Initialize(profileId) first.");

            return await _database.GetTodayStatsAsync(_currentProfileId);
        }


        public static void StartTracking()
        {
            _tracker?.StartTracking();
        }

        public static void StopTracking()
        {
            _tracker?.StopTracking();
        }
    }
}