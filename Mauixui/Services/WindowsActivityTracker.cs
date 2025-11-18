using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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

        // Живые счетчики времени (используются трекером и UI)
        public Dictionary<string, TimeSpan> CurrentAppTimes { get; private set; }
        public Dictionary<string, TimeSpan> CurrentWebsiteTimes { get; private set; }

        // 🔥 Главное добавление — событие каждый тик
        public event Action OnTick;

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

            CurrentAppTimes = new Dictionary<string, TimeSpan>();
            CurrentWebsiteTimes = new Dictionary<string, TimeSpan>();
        }

        public void StartTracking()
        {
            if (_isTracking) return;

            _isTracking = true;
            _currentWindowStartTime = DateTime.Now;

            // 🔥 каждая секунда → TrackActiveWindow
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

            // 🔥 Событие ТИКА — используется для LiveTotalTime
            OnTick?.Invoke();

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

        private bool IsSameWindow(ActiveWindowInfo w1, ActiveWindowInfo w2)
        {
            return w1.ProcessName == w2.ProcessName &&
                   w1.WindowTitle == w2.WindowTitle;
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

            var duration = DateTime.Now - _currentWindowStartTime;
            var appName = GetFriendlyAppName(_currentWindow.ProcessName);

            CurrentAppTimes[appName] = duration;
            OnAppUsageUpdated?.Invoke(appName, duration);
        }

        private void UpdateWebsiteStats()
        {
            if (_currentWindow == null) return;

            var website = ExtractWebsiteFromTitle(_currentWindow.WindowTitle);
            var duration = DateTime.Now - _currentWindowStartTime;

            CurrentWebsiteTimes[website] = duration;
            OnWebsiteUsageUpdated?.Invoke(website, duration);
        }

        private ActiveWindowInfo GetActiveWindowInfo()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return new ActiveWindowInfo { WindowTitle = "Unknown", ProcessName = "Unknown" };

                var sb = new System.Text.StringBuilder(512);
                GetWindowText(hwnd, sb, 512);

                GetWindowThreadProcessId(hwnd, out uint pid);
                var process = Process.GetProcessById((int)pid);

                return new ActiveWindowInfo
                {
                    WindowTitle = sb.ToString(),
                    ProcessName = process.ProcessName,
                    ProcessId = pid
                };
            }
            catch
            {
                return new ActiveWindowInfo { WindowTitle = "Unknown", ProcessName = "Unknown" };
            }
        }

        // --- Friendly Names ---
        private string GetFriendlyAppName(string processName)
        {
            var names = new Dictionary<string, string>
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
                {"slack", "Slack"}
            };

            var key = processName.ToLower();
            return names.ContainsKey(key) ? names[key] : processName;
        }

        // --- Website Parsing ---
        private bool IsBrowser(string processName)
        {
            string p = processName.ToLower();
            return p.Contains("chrome") || p.Contains("firefox") ||
                   p.Contains("edge") || p.Contains("opera");
        }

        private string ExtractWebsiteFromTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return "Unknown";

            string[] patterns = { " - ", " | ", " — ", " • " };

            foreach (var pattern in patterns)
            {
                if (title.Contains(pattern))
                {
                    var parts = title.Split(new[] { pattern }, StringSplitOptions.RemoveEmptyEntries);
                    return parts.Last().Trim();
                }
            }

            return title.Length > 40 ? title.Substring(0, 40) + "..." : title;
        }

        // Категоризация приложений и сайтов (оставил как у тебя)
        private string CategorizeActivity(string processName, string windowTitle) { var lowerProcess = processName.ToLower(); var lowerTitle = windowTitle.ToLower(); if (IsBrowser(lowerProcess)) return "Браузер"; if (lowerProcess.Contains("devenv") || lowerProcess.Contains("code") || lowerTitle.Contains("visual studio")) return "Разработка"; if (lowerProcess.Contains("word") || lowerProcess.Contains("excel") || lowerProcess.Contains("powerpoint") || lowerTitle.Contains("word") || lowerTitle.Contains("excel")) return "Офис"; if (lowerProcess.Contains("notepad") || lowerProcess.Contains("wordpad")) return "Текст"; if (lowerProcess.Contains("explorer")) return "Система"; if (lowerProcess.Contains("telegram") || lowerProcess.Contains("discord") || lowerProcess.Contains("whatsapp") || lowerProcess.Contains("slack")) return "Мессенджер"; if (lowerProcess.Contains("spotify") || lowerProcess.Contains("music") || lowerProcess.Contains("youtube.com")) return "Музыка/Видео"; if (lowerProcess.Contains("game") || lowerProcess.Contains("steam")) return "Игры"; if (lowerTitle.Contains("почта") || lowerTitle.Contains("mail") || lowerTitle.Contains("gmail") || lowerProcess.Contains("outlook")) return "Почта"; return "Другое"; }
        private string CategorizeWebsite(string website) { var lowerWebsite = website.ToLower(); if (lowerWebsite.Contains("youtube") || lowerWebsite.Contains("twitch") || lowerWebsite.Contains("netflix")) return "Видео"; if (lowerWebsite.Contains("github") || lowerWebsite.Contains("stackoverflow") || lowerWebsite.Contains("gitlab")) return "Разработка"; if (lowerWebsite.Contains("facebook") || lowerWebsite.Contains("instagram") || lowerWebsite.Contains("vk") || lowerWebsite.Contains("twitter")) return "Соцсети"; if (lowerWebsite.Contains("mail") || lowerWebsite.Contains("gmail") || lowerWebsite.Contains("outlook")) return "Почта"; if (lowerWebsite.Contains("google") || lowerWebsite.Contains("yandex") || lowerWebsite.Contains("bing")) return "Поиск"; if (lowerWebsite.Contains("amazon") || lowerWebsite.Contains("aliexpress") || lowerWebsite.Contains("wildberries")) return "Шопинг"; if (lowerWebsite.Contains("reddit") || lowerWebsite.Contains("habr") || lowerWebsite.Contains("medium")) return "Блоги/Форумы"; return "Другое"; }

        // --- Статистика ---
        public List<AppUsageRecord> GetTodayAppUsage()
        {
            var today = DateTime.Today;
            return _sessionRecords.Where(r => r.StartTime.Date == today).ToList();
        }

        public List<WebsiteUsageRecord> GetTodayWebsiteUsage()
        {
            var today = DateTime.Today;
            return _websiteRecords.Where(r => r.StartTime.Date == today).ToList();
        }

        public bool IsTracking => _isTracking;
    }
}
