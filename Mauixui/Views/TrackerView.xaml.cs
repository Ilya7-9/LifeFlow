using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Text.Json;
using SQLite;
using System.IO;

namespace Mauixui.Views
{
    public partial class TrackerView : ContentView
    {
        private System.Timers.Timer _uiUpdateTimer;
        private HistoryDatabase _historyDb;
        private DateTime _lastSnapshotDate = DateTime.MinValue;

        public TrackerView()
        {
            InitializeComponent();

            // Инициализация базы истории
            _historyDb = new HistoryDatabase();

            InitializeTracker();
            SetupUITimer();

            // Загружаем историю при старте
            Device.BeginInvokeOnMainThread(async () => await LoadAndRenderHistoryAsync());
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

            // Попробуем автоматически сохранить снимок за предыдущий день, если нужно
            Device.StartTimer(TimeSpan.FromSeconds(10), () =>
            {
                TryAutoSaveDailySnapshot();
                return true; // повторяем
            });
        }

        private bool _isUpdating = false;

        private void SetupUITimer()
        {
            _uiUpdateTimer = new System.Timers.Timer(1000);
            _uiUpdateTimer.Elapsed += (s, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    UpdateStats();
                    UpdateCurrentActivity();
                });
            };
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
                var totalTime = TrackerService.LiveTotalTime;

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
                var totalTrackedTime = TrackerService.GetTotalTrackedTime();

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

        private async void OnSaveSnapshotClicked(object sender, EventArgs e)
        {
            await SaveDailySnapshotAsync(DateTime.Today);
            await LoadAndRenderHistoryAsync();
            await DisplayAlert("Сохранено", "Снимок за сегодня сохранён в истории.", "OK");
        }

        private void OnUnloaded(object sender, EventArgs e)
        {
            _uiUpdateTimer?.Stop();
            _uiUpdateTimer?.Dispose();
        }

        private async Task SaveDailySnapshotAsync(DateTime date)
        {
            try
            {
                // Получаем текущие данные
                var todayAppUsage = TrackerService.GetTodayAppUsage();
                var todayWebsiteUsage = TrackerService.GetTodayWebsiteUsage();
                var totalTime = TrackerService.GetTotalTrackedTime();

                // Топ-приложение и топ-сайт
                var topApp = todayAppUsage
                    .GroupBy(r => r.AppName)
                    .Select(g => new { App = g.Key, Seconds = g.Sum(r => r.Duration.TotalSeconds) })
                    .OrderByDescending(x => x.Seconds)
                    .FirstOrDefault();

                var topSite = todayWebsiteUsage
                    .GroupBy(r => r.Website)
                    .Select(g => new { Site = g.Key, Seconds = g.Sum(r => r.Duration.TotalSeconds) })
                    .OrderByDescending(x => x.Seconds)
                    .FirstOrDefault();

                var appsSummary = todayAppUsage
                    .GroupBy(r => r.AppName)
                    .Select(g => new { App = g.Key, Seconds = g.Sum(r => r.Duration.TotalSeconds) })
                    .OrderByDescending(x => x.Seconds)
                    .ToList();

                var sitesSummary = todayWebsiteUsage
                    .GroupBy(r => r.Website)
                    .Select(g => new { Site = g.Key, Seconds = g.Sum(r => r.Duration.TotalSeconds) })
                    .OrderByDescending(x => x.Seconds)
                    .ToList();

                var stat = new DailyStat
                {
                    Date = date.Date,
                    TotalSeconds = (long)totalTime.TotalSeconds,
                    TopApp = topApp?.App ?? string.Empty,
                    TopSite = topSite?.Site ?? string.Empty,
                    AppsJson = JsonSerializer.Serialize(appsSummary),
                    SitesJson = JsonSerializer.Serialize(sitesSummary)
                };

                await _historyDb.SaveStatAsync(stat);
                _lastSnapshotDate = date.Date;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving daily snapshot: {ex.Message}");
            }
        }

        private void TryAutoSaveDailySnapshot()
        {
            // Автосохранение: если дата изменилась и предыдущая не была сохранена
            var today = DateTime.Today;
            if (_lastSnapshotDate.Date < today)
            {
                // попытка сохранить предыдущий день (в идеале, нужно сохранять в 00:01, но здесь — максимально простая логика)
                Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await SaveDailySnapshotAsync(today.AddDays(-1));
                        await LoadAndRenderHistoryAsync();
                    }
                    catch { /* silent */ }
                });

                _lastSnapshotDate = today;
            }
        }

        private async Task LoadAndRenderHistoryAsync()
        {
            try
            {
                var last7 = await _historyDb.GetLastDaysAsync(7);

                // Prepare UI models
                var models = last7.Select(s => new HistoryViewModel
                {
                    Date = s.Date,
                    DateText = s.Date.Date == DateTime.Today ? "СЕГОДНЯ" :
                               s.Date.Date == DateTime.Today.AddDays(-1) ? "ВЧЕРА" :
                               s.Date.ToString("dd MMM"),
                    TotalSeconds = s.TotalSeconds,
                    TotalTimeText = TimeSpan.FromSeconds(s.TotalSeconds).ToString(@"hh\:mm\:ss"),
                    TopSummary = $"Топ: {s.TopApp}  |  Сайт: {s.TopSite}",
                    Raw = s
                }).ToList();

                // Render bars
                RenderWeekBars(models);

                // Bind list
                HistoryList.ItemsSource = models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading history: {ex.Message}");
            }
        }

        private void RenderWeekBars(List<HistoryViewModel> models)
        {
            WeekBarsPanel.Children.Clear();

            // find max seconds to scale bars
            long maxSeconds = models.Any() ? models.Max(m => m.TotalSeconds) : 1;

            foreach (var m in models)
            {
                // vertical container for column
                var column = new VerticalStackLayout
                {
                    WidthRequest = 48,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End
                };

                // bar: height proportional to total seconds (max height 120)
                double height = 0;
                if (maxSeconds > 0)
                    height = Math.Max(6, 120.0 * (m.TotalSeconds / (double)maxSeconds));

                var barFrame = new Frame
                {
                    HeightRequest = height,
                    WidthRequest = 32,
                    BackgroundColor = Color.FromArgb("#3C82F6"),
                    CornerRadius = 6,
                    Padding = 0,
                    HasShadow = false,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End
                };

                // label under bar
                var label = new Label
                {
                    Text = m.DateText,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#CCCCCC"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center
                };

                // tooltip-like label above with hours
                var topLabel = new Label
                {
                    Text = TimeSpan.FromSeconds(m.TotalSeconds).ToString(@"h\:mm"),
                    FontSize = 10,
                    TextColor = Color.FromArgb("#CCCCCC"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center
                };

                column.Children.Add(topLabel);
                column.Children.Add(barFrame);
                column.Children.Add(label);

                // add tap to open detail (select item in list)
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) =>
                {
                    // set selection in CollectionView
                    HistoryList.SelectedItem = m;
                };
                column.GestureRecognizers.Add(tap);

                WeekBarsPanel.Children.Add(column);
            }
        }

        private async void OnHistorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is HistoryViewModel vm)
            {
                // Show details popup
                var raw = vm.Raw;
                string appsText = "Топ приложений:\n";
                try
                {
                    var apps = JsonSerializer.Deserialize<List<AppSummary>>(raw.AppsJson);
                    foreach (var a in apps.Take(10))
                    {
                        var ts = TimeSpan.FromSeconds(a.Seconds);
                        appsText += $"{a.App}: {ts:hh\\:mm\\:ss}\n";
                    }
                }
                catch { appsText += "(нет данных)\n"; }

                string sitesText = "\nТоп сайтов:\n";
                try
                {
                    var sites = JsonSerializer.Deserialize<List<SiteSummary>>(raw.SitesJson);
                    foreach (var s in sites.Take(10))
                    {
                        var ts = TimeSpan.FromSeconds(s.Seconds);
                        sitesText += $"{s.Site}: {ts:hh\\:mm\\:ss}\n";
                    }
                }
                catch { sitesText += "(нет данных)\n"; }

                await DisplayAlert($"{vm.DateText} — {vm.TotalTimeText}", appsText + sitesText, "OK");

                // deselect
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        // Экспорт одного дня в CSV (упрощённо)
        private async Task ExportDayToCsvAsync(DailyStat stat)
        {
            try
            {
                var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filename = $"activity_{stat.Date:yyyyMMdd}.csv";
                var path = Path.Combine(folder, filename);

                using var sw = new StreamWriter(path, false);
                sw.WriteLine("type,name,seconds");
                // apps
                try
                {
                    var apps = JsonSerializer.Deserialize<List<AppSummary>>(stat.AppsJson);
                    foreach (var a in apps)
                    {
                        sw.WriteLine($"app,\"{a.App}\",{a.Seconds}");
                    }
                }
                catch { }

                // sites
                try
                {
                    var sites = JsonSerializer.Deserialize<List<SiteSummary>>(stat.SitesJson);
                    foreach (var s in sites)
                    {
                        sw.WriteLine($"site,\"{s.Site}\",{s.Seconds}");
                    }
                }
                catch { }

                await DisplayAlert("Экспорт", $"CSV сохранён:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка экспорта", ex.Message, "OK");
            }
        }

        // Утилиты для DisplayAlert
        private async System.Threading.Tasks.Task DisplayAlert(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }

    // ===== Вспомогательные модели =====

    // ViewModel для списка истории (UI-friendly)
    public class HistoryViewModel
    {
        public DateTime Date { get; set; }
        public string DateText { get; set; }
        public long TotalSeconds { get; set; }
        public string TotalTimeText { get; set; }
        public string TopSummary { get; set; }
        public DailyStat Raw { get; set; }
    }

    // Простые структуры для десериализации
    public class AppSummary
    {
        public string App { get; set; }
        public long Seconds { get; set; }
    }

    public class SiteSummary
    {
        public string Site { get; set; }
        public long Seconds { get; set; }
    }
}
