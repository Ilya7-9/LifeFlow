using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mauixui.Models;

namespace Mauixui.Services
{
    public class WindowsActivityTracker
    {
        private Timer _trackingTimer;
        private bool _isTracking;
        private ActiveWindowInfo _currentWindow;
        private DateTime _currentWindowStartTime;
        private readonly List<AppUsageRecord> _sessionRecords;
        private readonly List<WebsiteUsageRecord> _websiteRecords;

        // ДЕЛАЕМ ПОЛЯ ПУБЛИЧНЫМИ ИЛИ СОЗДАЕМ ПУБЛИЧНЫЕ СВОЙСТВА
        public Dictionary<string, TimeSpan> CurrentAppTimes { get; private set; }
        public Dictionary<string, TimeSpan> CurrentWebsiteTimes { get; private set; }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public event Action<AppUsageRecord> OnAppUsageRecorded;
        public event Action<WebsiteUsageRecord> OnWebsiteUsageRecorded;
        public event Action<string, TimeSpan> OnAppUsageUpdated;
        public event Action<string, TimeSpan> OnWebsiteUsageUpdated;

        public WindowsActivityTracker()
        {
            _sessionRecords = new List<AppUsageRecord>();
            _websiteRecords = new List<WebsiteUsageRecord>();

            // ИНИЦИАЛИЗИРУЕМ ПУБЛИЧНЫЕ СВОЙСТВА
            CurrentAppTimes = new Dictionary<string, TimeSpan>();
            CurrentWebsiteTimes = new Dictionary<string, TimeSpan>();
        }

        public void StartTracking()
        {
            if (_isTracking) return;

            _isTracking = true;
            _currentWindowStartTime = DateTime.Now;
            _trackingTimer = new Timer(TrackActiveWindow, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void StopTracking()
        {
            _isTracking = false;
            _trackingTimer?.Dispose();
            SaveCurrentRecord();
        }

        private void TrackActiveWindow(object state)
        {
            if (!_isTracking) return;

            var newWindow = GetActiveWindowInfo();

            if (_currentWindow == null || !IsSameWindow(_currentWindow, newWindow))
            {
                SaveCurrentRecord();
                StartNewSession(newWindow);
            }

            _currentWindow = newWindow;
            UpdateCurrentAppStats();

            if (IsBrowser(_currentWindow.ProcessName))
            {
                UpdateWebsiteStats();
            }
        }

        private bool IsSameWindow(ActiveWindowInfo window1, ActiveWindowInfo window2)
        {
            return window1.ProcessName == window2.ProcessName &&
                   window1.WindowTitle == window2.WindowTitle;
        }

        private void StartNewSession(ActiveWindowInfo window)
        {
            _currentWindow = window;
            _currentWindowStartTime = DateTime.Now;
        }

        private void SaveCurrentRecord()
        {
            if (_currentWindow == null || string.IsNullOrEmpty(_currentWindow.ProcessName))
                return;

            var duration = DateTime.Now - _currentWindowStartTime;

            if (duration.TotalSeconds < 1) return;

            var record = new AppUsageRecord
            {
                AppName = GetFriendlyAppName(_currentWindow.ProcessName),
                WindowTitle = _currentWindow.WindowTitle,
                ProcessName = _currentWindow.ProcessName,
                StartTime = _currentWindowStartTime,
                EndTime = DateTime.Now,
                Category = CategorizeActivity(_currentWindow.ProcessName, _currentWindow.WindowTitle)
            };

            _sessionRecords.Add(record);
            OnAppUsageRecorded?.Invoke(record);

            if (IsBrowser(_currentWindow.ProcessName))
            {
                SaveWebsiteRecord(record);
            }
        }

        private void SaveWebsiteRecord(AppUsageRecord appRecord)
        {
            var website = ExtractWebsiteFromTitle(appRecord.WindowTitle);

            var websiteRecord = new WebsiteUsageRecord
            {
                Website = website,
                Url = appRecord.WindowTitle,
                StartTime = appRecord.StartTime,
                EndTime = appRecord.EndTime,
                Category = CategorizeWebsite(website)
            };

            _websiteRecords.Add(websiteRecord);
            OnWebsiteUsageRecorded?.Invoke(websiteRecord);
        }

        private void UpdateCurrentAppStats()
        {
            if (_currentWindow == null) return;

            var currentDuration = DateTime.Now - _currentWindowStartTime;
            var appName = GetFriendlyAppName(_currentWindow.ProcessName);

            // ОБНОВЛЯЕМ ПУБЛИЧНЫЕ СВОЙСТВА
            CurrentAppTimes[appName] = currentDuration;
            OnAppUsageUpdated?.Invoke(appName, currentDuration);
        }

        private void UpdateWebsiteStats()
        {
            if (_currentWindow == null) return;

            var website = ExtractWebsiteFromTitle(_currentWindow.WindowTitle);
            var currentDuration = DateTime.Now - _currentWindowStartTime;

            // ОБНОВЛЯЕМ ПУБЛИЧНЫЕ СВОЙСТВА
            CurrentWebsiteTimes[website] = currentDuration;
            OnWebsiteUsageUpdated?.Invoke(website, currentDuration);
        }

        private ActiveWindowInfo GetActiveWindowInfo()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return new ActiveWindowInfo { WindowTitle = "Unknown", ProcessName = "Unknown" };

                System.Text.StringBuilder sb = new System.Text.StringBuilder(512);
                GetWindowText(hwnd, sb, 512);
                string windowTitle = sb.ToString();

                GetWindowThreadProcessId(hwnd, out uint processId);
                Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName;

                return new ActiveWindowInfo
                {
                    WindowTitle = windowTitle,
                    ProcessName = processName,
                    ProcessId = processId
                };
            }
            catch
            {
                return new ActiveWindowInfo { WindowTitle = "Unknown", ProcessName = "Unknown" };
            }
        }

        private string GetFriendlyAppName(string processName)
        {
            var friendlyNames = new Dictionary<string, string>
            {
                {"chrome", "Google Chrome"},
                {"msedge", "Microsoft Edge"},
                {"firefox", "Mozilla Firefox"},
                {"opera", "Opera Browser"},
                {"notepad", "Блокнот"},
                {"winword", "Microsoft Word"},
                {"excel", "Microsoft Excel"},
                {"powerpnt", "Microsoft PowerPoint"},
                {"devenv", "Visual Studio"},
                {"code", "VS Code"},
                {"explorer", "Проводник"},
                {"telegram", "Telegram"},
                {"discord", "Discord"},
                {"whatsapp", "WhatsApp"},
                {"slack", "Slack"},
                {"outlook", "Outlook"},
                {"teams", "Microsoft Teams"}
            };

            return friendlyNames.ContainsKey(processName.ToLower())
                ? friendlyNames[processName.ToLower()]
                : processName;
        }

        private bool IsBrowser(string processName)
        {
            var browsers = new[] { "chrome", "firefox", "msedge", "opera", "safari", "browser" };
            return browsers.Any(browser => processName.ToLower().Contains(browser));
        }

        private string ExtractWebsiteFromTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return "Unknown";

            var patterns = new[] { " - ", " | ", " — ", " • " };

            foreach (var pattern in patterns)
            {
                if (title.Contains(pattern))
                {
                    var parts = title.Split(new[] { pattern }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        var domain = parts.Last().Trim();
                        if (domain.EndsWith(" - Google Chrome"))
                            domain = domain.Replace(" - Google Chrome", "");
                        if (domain.EndsWith(" - Microsoft Edge"))
                            domain = domain.Replace(" - Microsoft Edge", "");
                        return domain;
                    }
                }
            }

            return title.Length > 40 ? title.Substring(0, 40) + "..." : title;
        }

        private string CategorizeActivity(string processName, string windowTitle)
        {
            var lowerProcess = processName.ToLower();
            var lowerTitle = windowTitle.ToLower();

            if (IsBrowser(lowerProcess)) return "Браузер";
            if (lowerProcess.Contains("devenv") || lowerProcess.Contains("code") || lowerTitle.Contains("visual studio"))
                return "Разработка";
            if (lowerProcess.Contains("word") || lowerProcess.Contains("excel") || lowerProcess.Contains("powerpoint") || lowerTitle.Contains("word") || lowerTitle.Contains("excel"))
                return "Офис";
            if (lowerProcess.Contains("notepad") || lowerProcess.Contains("wordpad")) return "Текст";
            if (lowerProcess.Contains("explorer")) return "Система";
            if (lowerProcess.Contains("telegram") || lowerProcess.Contains("discord") || lowerProcess.Contains("whatsapp") || lowerProcess.Contains("slack"))
                return "Мессенджер";
            if (lowerProcess.Contains("spotify") || lowerProcess.Contains("music") || lowerProcess.Contains("youtube.com"))
                return "Музыка/Видео";
            if (lowerProcess.Contains("game") || lowerProcess.Contains("steam")) return "Игры";
            if (lowerTitle.Contains("почта") || lowerTitle.Contains("mail") || lowerTitle.Contains("gmail") || lowerProcess.Contains("outlook"))
                return "Почта";

            return "Другое";
        }

        private string CategorizeWebsite(string website)
        {
            var lowerWebsite = website.ToLower();

            if (lowerWebsite.Contains("youtube") || lowerWebsite.Contains("twitch") || lowerWebsite.Contains("netflix"))
                return "Видео";
            if (lowerWebsite.Contains("github") || lowerWebsite.Contains("stackoverflow") || lowerWebsite.Contains("gitlab"))
                return "Разработка";
            if (lowerWebsite.Contains("facebook") || lowerWebsite.Contains("instagram") || lowerWebsite.Contains("vk") || lowerWebsite.Contains("twitter"))
                return "Соцсети";
            if (lowerWebsite.Contains("mail") || lowerWebsite.Contains("gmail") || lowerWebsite.Contains("outlook"))
                return "Почта";
            if (lowerWebsite.Contains("google") || lowerWebsite.Contains("yandex") || lowerWebsite.Contains("bing"))
                return "Поиск";
            if (lowerWebsite.Contains("amazon") || lowerWebsite.Contains("aliexpress") || lowerWebsite.Contains("wildberries"))
                return "Шопинг";
            if (lowerWebsite.Contains("reddit") || lowerWebsite.Contains("habr") || lowerWebsite.Contains("medium"))
                return "Блоги/Форумы";

            return "Другое";
        }

        // Методы для получения статистики
        public List<AppUsageRecord> GetTodayAppUsage()
        {
            var today = DateTime.Today;
            return _sessionRecords
                .Where(r => r.StartTime.Date == today)
                .OrderByDescending(r => r.StartTime)
                .ToList();
        }

        public List<WebsiteUsageRecord> GetTodayWebsiteUsage()
        {
            var today = DateTime.Today;
            return _websiteRecords
                .Where(r => r.StartTime.Date == today)
                .OrderByDescending(r => r.StartTime)
                .ToList();
        }

        public TimeSpan GetTotalUsageToday()
        {
            var todayRecords = GetTodayAppUsage();
            return TimeSpan.FromSeconds(todayRecords.Sum(r => r.Duration.TotalSeconds));
        }

        public Dictionary<string, TimeSpan> GetAppUsageByCategory()
        {
            var todayRecords = GetTodayAppUsage();
            return todayRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)));
        }

        public Dictionary<string, TimeSpan> GetWebsiteUsageByCategory()
        {
            var todayRecords = GetTodayWebsiteUsage();
            return todayRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)));
        }

        public bool IsTracking => _isTracking;
    }
}