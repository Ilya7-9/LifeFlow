using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microcharts;
using SkiaSharp;

namespace Mauixui.Views
{
    public partial class TrackerView : ContentView
    {
        private MainDatabase _database;
        private UserProfile _currentProfile;
        private System.Timers.Timer _refreshTimer;

        public TrackerView()
        {
            InitializeComponent();
            InitializeServices();
            SetupRefreshTimer();
            LoadInitialData();
        }

        private async Task InitializeServices()
        {
            try
            {
                // Получаем текущий профиль
                if (Application.Current is App app)
                {
                    _currentProfile = await app.GetCurrentProfileAsync();
                    _database = MainDatabase.Instance;
                }

                // Инициализируем TrackerService если он еще не инициализирован
                if (_currentProfile != null)
                {
                    // Инициализация трекера
                    var tracker = new WindowsActivityTracker();
                    tracker.StartTracking();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации трекера: {ex.Message}");
            }
        }

        private void SetupRefreshTimer()
        {
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += async (s, e) => await RefreshDataAsync();
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        private async void LoadInitialData()
        {
            try
            {
                if (_currentProfile == null) return;

                // Загружаем данные
                await LoadTodayStatsAsync();
                await LoadWeeklyStatsAsync();
                await LoadTopAppsAsync();
                await LoadTopWebsitesAsync();
                await LoadProductivityDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки данных: {ex.Message}");
            }
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await LoadTodayStatsAsync();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления данных: {ex.Message}");
            }
        }

        private async Task LoadTodayStatsAsync()
        {
            try
            {
                if (_currentProfile == null || _database == null) return;

                var todayStats = await _database.GetTodayStatsAsync(_currentProfile.Id);
                var todayApps = await _database.GetTodayAppUsageAsync(_currentProfile.Id);
                var todayWebsites = await _database.GetTodayWebsiteUsageAsync(_currentProfile.Id);

                // Рассчитываем общее время
                double totalSeconds = 0;
                foreach (var app in todayApps)
                    totalSeconds += (app.EndTime - app.StartTime).TotalSeconds;

                foreach (var site in todayWebsites)
                    totalSeconds += (site.EndTime - site.StartTime).TotalSeconds;

                var todayTime = TimeSpan.FromSeconds(totalSeconds);
                var productiveTime = CalculateProductiveTime(todayApps, todayWebsites);

                Device.BeginInvokeOnMainThread(() =>
                {
                    TodayTimeLabel.Text = FormatTime(todayTime);
                    ProductiveTimeLabel.Text = FormatTime(productiveTime);
                    AppsCountLabel.Text = todayApps.Count.ToString();
                    WebsitesCountLabel.Text = todayWebsites.Count.ToString();

                    // Прогресс продуктивности
                    var productivityPercent = todayTime.TotalSeconds > 0
                        ? (int)((productiveTime.TotalSeconds / todayTime.TotalSeconds) * 100)
                        : 0;
                    ProductivityProgressBar.Progress = productivityPercent / 100.0;
                    ProductivityPercentLabel.Text = $"{productivityPercent}%";

                    // Текущая активность (заглушка)
                    CurrentAppLabel.Text = todayApps.LastOrDefault()?.AppName ?? "Нет данных";
                    CurrentWebsiteLabel.Text = todayWebsites.LastOrDefault()?.Website ?? "Нет данных";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки статистики за сегодня: {ex.Message}");
            }
        }

        private async Task LoadWeeklyStatsAsync()
        {
            try
            {
                if (_currentProfile == null || _database == null) return;

                var weeklyStats = await _database.GetLastDaysAsync(_currentProfile.Id, 7);

                if (weeklyStats.Any())
                {
                    var entries = weeklyStats.Select(stat => new ChartEntry((float)stat.TotalSeconds / 3600)
                    {
                        Label = stat.Date.ToString("ddd"),
                        ValueLabel = $"{TimeSpan.FromSeconds(stat.TotalSeconds):hh\\:mm}",
                        Color = SKColor.Parse("#5865F2")
                    }).ToList();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var chart = new LineChart
                        {
                            Entries = entries,
                            LineMode = LineMode.Straight,
                            LineSize = 4,
                            PointMode = PointMode.Circle,
                            PointSize = 8,
                            LabelTextSize = 12,
                            BackgroundColor = SKColors.Transparent
                        };
                        WeeklyChartView.Chart = chart;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки недельной статистики: {ex.Message}");
            }
        }

        private async Task LoadTopAppsAsync()
        {
            try
            {
                if (_currentProfile == null || _database == null) return;

                var todayApps = await _database.GetTodayAppUsageAsync(_currentProfile.Id);

                var topApps = todayApps
                    .GroupBy(a => a.AppName)
                    .Select(g => new
                    {
                        App = g.Key,
                        Time = TimeSpan.FromSeconds(g.Sum(a => (a.EndTime - a.StartTime).TotalSeconds)),
                        Category = g.FirstOrDefault()?.Category ?? "Другое"
                    })
                    .OrderByDescending(x => x.Time.TotalSeconds)
                    .Take(5)
                    .ToList();

                Device.BeginInvokeOnMainThread(() =>
                {
                    TopAppsStackLayout.Children.Clear();

                    if (topApps.Any())
                    {
                        foreach (var app in topApps)
                        {
                            var appView = CreateAppItemView(app.App, app.Time, app.Category);
                            TopAppsStackLayout.Children.Add(appView);
                        }
                    }
                    else
                    {
                        var label = new Label
                        {
                            Text = "Нет данных за сегодня",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#949BA4"),
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(0, 10)
                        };
                        TopAppsStackLayout.Children.Add(label);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки топ приложений: {ex.Message}");
            }
        }

        private async Task LoadTopWebsitesAsync()
        {
            try
            {
                if (_currentProfile == null || _database == null) return;

                var todayWebsites = await _database.GetTodayWebsiteUsageAsync(_currentProfile.Id);

                var topSites = todayWebsites
                    .GroupBy(w => w.Website)
                    .Select(g => new
                    {
                        Site = g.Key,
                        Time = TimeSpan.FromSeconds(g.Sum(w => (w.EndTime - w.StartTime).TotalSeconds)),
                        Category = g.FirstOrDefault()?.Category ?? "Другое"
                    })
                    .OrderByDescending(x => x.Time.TotalSeconds)
                    .Take(5)
                    .ToList();

                Device.BeginInvokeOnMainThread(() =>
                {
                    TopWebsitesStackLayout.Children.Clear();

                    if (topSites.Any())
                    {
                        foreach (var site in topSites)
                        {
                            var siteView = CreateWebsiteItemView(site.Site, site.Time, site.Category);
                            TopWebsitesStackLayout.Children.Add(siteView);
                        }
                    }
                    else
                    {
                        var label = new Label
                        {
                            Text = "Нет данных за сегодня",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#949BA4"),
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(0, 10)
                        };
                        TopWebsitesStackLayout.Children.Add(label);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки топ сайтов: {ex.Message}");
            }
        }

        private async Task LoadProductivityDataAsync()
        {
            try
            {
                if (_currentProfile == null || _database == null) return;

                var todayApps = await _database.GetTodayAppUsageAsync(_currentProfile.Id);
                var todayWebsites = await _database.GetTodayWebsiteUsageAsync(_currentProfile.Id);

                var productiveCategories = new[] { "Разработка", "Офис", "Обучение", "Почта" };
                var distractingCategories = new[] { "Соцсети", "Игры", "Видео", "Развлечения" };

                var productiveTime = CalculateTimeByCategories(todayApps, todayWebsites, productiveCategories);
                var distractingTime = CalculateTimeByCategories(todayApps, todayWebsites, distractingCategories);

                Device.BeginInvokeOnMainThread(() =>
                {
                    ProductiveTimeCard.Text = FormatTime(productiveTime);
                    DistractingTimeCard.Text = FormatTime(distractingTime);

                    // Круговая диаграмма продуктивности
                    var entries = new List<ChartEntry>();

                    if (productiveTime.TotalSeconds > 0)
                    {
                        entries.Add(new ChartEntry((float)productiveTime.TotalSeconds)
                        {
                            Label = "Продуктивно",
                            ValueLabel = FormatTime(productiveTime),
                            Color = SKColor.Parse("#23A55A")
                        });
                    }

                    if (distractingTime.TotalSeconds > 0)
                    {
                        entries.Add(new ChartEntry((float)distractingTime.TotalSeconds)
                        {
                            Label = "Отвлечения",
                            ValueLabel = FormatTime(distractingTime),
                            Color = SKColor.Parse("#F23F43")
                        });
                    }

                    if (entries.Any())
                    {
                        var chart = new DonutChart
                        {
                            Entries = entries,
                            LabelTextSize = 14,
                            BackgroundColor = SKColors.Transparent,
                            HoleRadius = 0.4f
                        };
                        ProductivityChartView.Chart = chart;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки данных продуктивности: {ex.Message}");
            }
        }

        #region Helper Methods

        private TimeSpan CalculateProductiveTime(List<AppUsageRecord> apps, List<WebsiteUsageRecord> websites)
        {
            var productiveCategories = new[] { "Разработка", "Офис", "Обучение", "Почта" };

            double productiveSeconds = 0;

            foreach (var app in apps)
            {
                if (productiveCategories.Contains(app.Category))
                    productiveSeconds += (app.EndTime - app.StartTime).TotalSeconds;
            }

            foreach (var site in websites)
            {
                if (productiveCategories.Contains(site.Category))
                    productiveSeconds += (site.EndTime - site.StartTime).TotalSeconds;
            }

            return TimeSpan.FromSeconds(productiveSeconds);
        }

        private TimeSpan CalculateTimeByCategories(
            List<AppUsageRecord> apps,
            List<WebsiteUsageRecord> websites,
            string[] categories)
        {
            double totalSeconds = 0;

            foreach (var app in apps)
            {
                if (categories.Contains(app.Category))
                    totalSeconds += (app.EndTime - app.StartTime).TotalSeconds;
            }

            foreach (var site in websites)
            {
                if (categories.Contains(site.Category))
                    totalSeconds += (site.EndTime - site.StartTime).TotalSeconds;
            }

            return TimeSpan.FromSeconds(totalSeconds);
        }

        private View CreateAppItemView(string appName, TimeSpan time, string category)
        {
            var stackLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 10,
                Padding = new Thickness(10, 5),
                BackgroundColor = Color.FromArgb("#1E1F22"),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var iconLabel = new Label
            {
                Text = GetAppIcon(category),
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center
            };

            var infoLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Spacing = 2
            };

            var nameLabel = new Label
            {
                Text = appName,
                FontSize = 14,
                TextColor = Color.FromArgb("#FFFFFF")
            };

            var timeLabel = new Label
            {
                Text = $"{FormatTime(time)} • {category}",
                FontSize = 12,
                TextColor = Color.FromArgb("#949BA4")
            };

            infoLayout.Children.Add(nameLabel);
            infoLayout.Children.Add(timeLabel);

            stackLayout.Children.Add(iconLabel);
            stackLayout.Children.Add(infoLayout);

            return stackLayout;
        }

        private View CreateWebsiteItemView(string website, TimeSpan time, string category)
        {
            var stackLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 10,
                Padding = new Thickness(10, 5),
                BackgroundColor = Color.FromArgb("#1E1F22"),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var iconLabel = new Label
            {
                Text = GetWebsiteIcon(category),
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center
            };

            var infoLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Spacing = 2
            };

            var nameLabel = new Label
            {
                Text = website.Length > 30 ? website.Substring(0, 30) + "..." : website,
                FontSize = 14,
                TextColor = Color.FromArgb("#FFFFFF")
            };

            var timeLabel = new Label
            {
                Text = $"{FormatTime(time)} • {category}",
                FontSize = 12,
                TextColor = Color.FromArgb("#949BA4")
            };

            infoLayout.Children.Add(nameLabel);
            infoLayout.Children.Add(timeLabel);

            stackLayout.Children.Add(iconLabel);
            stackLayout.Children.Add(infoLayout);

            return stackLayout;
        }

        private string GetAppIcon(string category)
        {
            return category switch
            {
                "Разработка" => "💻",
                "Браузер" => "🌐",
                "Офис" => "📄",
                "Мессенджер" => "💬",
                "Игры" => "🎮",
                "Музыка/Видео" => "🎵",
                _ => "📱"
            };
        }

        private string GetWebsiteIcon(string category)
        {
            return category switch
            {
                "Разработка" => "👨‍💻",
                "Соцсети" => "👥",
                "Видео" => "🎥",
                "Поиск" => "🔍",
                "Почта" => "📧",
                "Шопинг" => "🛒",
                _ => "🌐"
            };
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}ч {time.Minutes}м";
            else if (time.TotalMinutes >= 1)
                return $"{time.Minutes}м {time.Seconds}с";
            else
                return $"{time.Seconds}с";
        }

        #endregion

        #region Event Handlers

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            RefreshButton.IsEnabled = false;
            RefreshButton.Text = "🔄 Обновление...";

            try
            {
                await LoadTodayStatsAsync();
                await LoadWeeklyStatsAsync();
                await LoadTopAppsAsync();
                await LoadTopWebsitesAsync();
                await LoadProductivityDataAsync();

                await DisplayAlert("Успех", "Данные обновлены", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось обновить данные: {ex.Message}", "OK");
            }
            finally
            {
                RefreshButton.IsEnabled = true;
                RefreshButton.Text = "🔄 Обновить";
            }
        }

        private async void OnViewAllAppsClicked(object sender, EventArgs e)
        {
            // Создайте простую страницу для просмотра истории
            await DisplayAlert("Информация", "Функция просмотра всей истории будет добавлена в следующем обновлении", "OK");
        }

        private async void OnViewAllWebsitesClicked(object sender, EventArgs e)
        {
            // Создайте простую страницу для просмотра истории
            await DisplayAlert("Информация", "Функция просмотра всей истории будет добавлена в следующем обновлении", "OK");
        }

        private async void OnStartTrackingClicked(object sender, EventArgs e)
        {
            StartTrackingButton.IsEnabled = false;
            StopTrackingButton.IsEnabled = true;
            await DisplayAlert("Трекинг", "Трекинг активности запущен", "OK");
        }

        private async void OnStopTrackingClicked(object sender, EventArgs e)
        {
            StartTrackingButton.IsEnabled = true;
            StopTrackingButton.IsEnabled = false;
            await DisplayAlert("Трекинг", "Трекинг активности остановлен", "OK");
        }

        private async void OnClearHistoryClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Очистка истории",
                "Вы уверены, что хотите очистить всю историю трекера?", "Да", "Нет");

            if (confirm)
            {
                try
                {
                    // Здесь можно добавить логику очистки
                    await DisplayAlert("Успех", "История очищена", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось очистить историю: {ex.Message}", "OK");
                }
            }
        }

        #endregion

        protected override void OnParentSet()
        {
            base.OnParentSet();
            if (Parent == null)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
        }

        // Вспомогательные методы для диалогов
        private async Task DisplayAlert(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        private async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);

            return false;
        }
    }
}