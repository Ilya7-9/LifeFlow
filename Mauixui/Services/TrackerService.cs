using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Models;

namespace Mauixui.Services
{
    public static class TrackerService
    {
        private static WindowsActivityTracker _tracker;
        private static bool _isInitialized = false;

        public static WindowsActivityTracker Tracker
        {
            get
            {
                if (!_isInitialized)
                {
                    _tracker = new WindowsActivityTracker();
                    _isInitialized = true;
                }
                return _tracker;
            }
        }

        public static void EnsureStarted()
        {
            // Гарантируем, что трекер запущен
            var tracker = Tracker;
            if (!tracker.IsTracking)
            {
                tracker.StartTracking();
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД - добавлен static
        public static async Task<TimeSpan> GetTotalTrackedTimeAsync()
        {
            try
            {
                // Используем существующий трекер для получения времени
                var todayAppUsage = GetTodayAppUsage();
                var todayWebsiteUsage = GetTodayWebsiteUsage();

                var totalSeconds = todayAppUsage.Sum(r => r.Duration.TotalSeconds) +
                                 todayWebsiteUsage.Sum(r => r.Duration.TotalSeconds);

                return TimeSpan.FromSeconds(totalSeconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения общего времени: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        // ДОБАВИМ СИНХРОННУЮ ВЕРСИЮ
        public static TimeSpan GetTotalTrackedTime()
        {
            try
            {
                var todayAppUsage = GetTodayAppUsage();
                var todayWebsiteUsage = GetTodayWebsiteUsage();

                var totalSeconds = todayAppUsage.Sum(r => r.Duration.TotalSeconds) +
                                 todayWebsiteUsage.Sum(r => r.Duration.TotalSeconds);

                return TimeSpan.FromSeconds(totalSeconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения общего времени: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        public static List<AppUsageRecord> GetTodayAppUsage()
        {
            return Tracker.GetTodayAppUsage();
        }

        public static List<WebsiteUsageRecord> GetTodayWebsiteUsage()
        {
            return Tracker.GetTodayWebsiteUsage();
        }

        // ДОБАВИМ МЕТОД ДЛЯ ПОЛУЧЕНИЯ ТЕКУЩЕЙ АКТИВНОСТИ
        public static (string app, TimeSpan time) GetCurrentAppActivity()
        {
            try
            {
                var currentAppTimes = Tracker.CurrentAppTimes;
                var currentApp = currentAppTimes.OrderByDescending(x => x.Value).FirstOrDefault();

                return (currentApp.Key ?? "Неизвестно", currentApp.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения текущей активности: {ex.Message}");
                return ("Ошибка", TimeSpan.Zero);
            }
        }

        // ДОБАВИМ МЕТОД ДЛЯ ПОЛУЧЕНИЯ ТЕКУЩЕГО САЙТА
        public static (string website, TimeSpan time) GetCurrentWebsiteActivity()
        {
            try
            {
                var currentWebsiteTimes = Tracker.CurrentWebsiteTimes;
                var currentWebsite = currentWebsiteTimes.OrderByDescending(x => x.Value).FirstOrDefault();

                return (currentWebsite.Key ?? "Неизвестно", currentWebsite.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения текущего сайта: {ex.Message}");
                return ("Ошибка", TimeSpan.Zero);
            }
        }
    }
}