using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Models;

namespace Mauixui.Services
{
    public static class TrackerService
    {
        // Живое время (каждая секунда увеличивается)
        public static TimeSpan LiveTotalTime = TimeSpan.Zero;

        private static WindowsActivityTracker _tracker;
        private static bool _isInitialized = false;

        // Блокировка, чтобы избежать гонок
        private static readonly object _lock = new object();

        public static WindowsActivityTracker Tracker
        {
            get
            {
                if (!_isInitialized)
                {
                    _tracker = new WindowsActivityTracker();
                    _tracker.OnTick += OnTrackerTick;   // ВАЖНО: таймер реального времени
                    _isInitialized = true;
                }
                return _tracker;
            }
        }

        /// <summary>
        /// Запускает трекер, если он ещё не запущен.
        /// </summary>
        public static void EnsureStarted()
        {
            var tracker = Tracker;
            if (!tracker.IsTracking)
                tracker.StartTracking();
        }

        // 🔥 Обновляется каждую секунду из внутреннего таймера WindowsActivityTracker
        private static void OnTrackerTick()
        {
            lock (_lock)
            {
                LiveTotalTime += TimeSpan.FromSeconds(1);
            }
        }

        // ---------------------------
        // ВРЕМЯ (сумма всей дневной активности)
        // ---------------------------

        public static async Task<TimeSpan> GetTotalTrackedTimeAsync()
        {
            try
            {
                var todayApps = GetTodayAppUsage();
                var todaySites = GetTodayWebsiteUsage();

                double total =
                    todayApps.Sum(r => r.Duration.TotalSeconds) +
                    todaySites.Sum(r => r.Duration.TotalSeconds);

                return TimeSpan.FromSeconds(total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения общего времени: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        public static TimeSpan GetTotalTrackedTime()
        {
            try
            {
                var todayApps = GetTodayAppUsage();
                var todaySites = GetTodayWebsiteUsage();

                double total =
                    todayApps.Sum(r => r.Duration.TotalSeconds) +
                    todaySites.Sum(r => r.Duration.TotalSeconds);

                return TimeSpan.FromSeconds(total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения общего времени: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        // ---------------------------
        // ЗАПИСИ ЗА СЕГОДНЯ
        // ---------------------------

        public static List<AppUsageRecord> GetTodayAppUsage()
        {
            return Tracker.GetTodayAppUsage();
        }

        public static List<WebsiteUsageRecord> GetTodayWebsiteUsage()
        {
            return Tracker.GetTodayWebsiteUsage();
        }

        // ---------------------------
        // ТЕКУЩАЯ АКТИВНОСТЬ (LIVE)
        // ---------------------------

        public static (string app, TimeSpan time) GetCurrentAppActivity()
        {
            try
            {
                var dict = Tracker.CurrentAppTimes;

                if (dict.Count == 0)
                    return ("Неизвестно", TimeSpan.Zero);

                var pair = dict.OrderByDescending(x => x.Value).First();

                return (pair.Key ?? "Неизвестно", pair.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения текущей активности: {ex.Message}");
                return ("Ошибка", TimeSpan.Zero);
            }
        }

        public static (string website, TimeSpan time) GetCurrentWebsiteActivity()
        {
            try
            {
                var dict = Tracker.CurrentWebsiteTimes;

                if (dict.Count == 0)
                    return ("Неизвестно", TimeSpan.Zero);

                var pair = dict.OrderByDescending(x => x.Value).First();

                return (pair.Key ?? "Неизвестно", pair.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения текущего сайта: {ex.Message}");
                return ("Ошибка", TimeSpan.Zero);
            }
        }
    }
}
