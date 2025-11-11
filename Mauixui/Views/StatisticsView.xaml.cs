using Microsoft.Maui.Controls;
using Microcharts;
using SkiaSharp;
using System.Linq;
using Mauixui.Services;
using Mauixui.Models;

namespace Mauixui.Views
{
    public partial class StatisticsView : ContentView
    {
        private FinanceDatabase _db;
        private string _profileId;
        private List<FinanceItem> _items = new();

        public StatisticsView()
        {
            InitializeComponent();
            var profileService = new ProfileService();
            _profileId = profileService.GetCurrentProfile().Id;
            _db = profileService.GetFinanceDatabase(_profileId);
            ReloadStatistics(null, null);
        }

        private async void ReloadStatistics(object sender, EventArgs e)
        {
            _items = await _db.GetItemsAsync(_profileId);
            UpdateCharts();
        }

        private void UpdateCharts()
        {
            try
            {
                if (_items == null || !_items.Any())
                {
                    ExpensesChart.Chart = null;
                    BalanceChart.Chart = null;
                    return;
                }

                // --- Защита от null и некорректных категорий
                var byCategory = _items
                    .Where(i => i.Type == "Расход" && !string.IsNullOrEmpty(i.Category))
                    .GroupBy(i => i.Category)
                    .Select(g => new Microcharts.ChartEntry((float)(g.Sum(x => (double)x.Amount)))
                    {
                        Label = g.Key ?? "Без категории",
                        ValueLabel = $"{g.Sum(x => (double)x.Amount):F0}",
                        Color = SafeColorFromString(g.Key)
                    }).ToList();

                ExpensesChart.Chart = new Microcharts.DonutChart
                {
                    Entries = byCategory,
                    LabelTextSize = 28,
                    BackgroundColor = SKColors.Transparent
                };

                // --- Динамика по датам
                var byDate = _items
                    .GroupBy(i => i.Date.Date)
                    .Select(g => new Microcharts.ChartEntry((float)(g
                        .Where(x => x.Type == "Доход").Sum(x => (double)x.Amount)
                        - g.Where(x => x.Type == "Расход").Sum(x => (double)x.Amount)))
                    {
                        Label = g.Key.ToString("dd.MM"),
                        ValueLabel = $"{g.Sum(x => (double)x.Amount):F0}",
                        Color = SKColors.SkyBlue
                    }).ToList();

                BalanceChart.Chart = new Microcharts.LineChart
                {
                    Entries = byDate,
                    LineMode = LineMode.Straight,
                    LineSize = 6,
                    PointMode = PointMode.Circle,
                    PointSize = 8,
                    LabelTextSize = 28,
                    BackgroundColor = SKColors.Transparent
                };
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось обновить статистику:\n{ex.Message}", "OK");
                });
            }
        }

        private SKColor SafeColorFromString(string seed)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(seed))
                    seed = Guid.NewGuid().ToString();

                int hash = Math.Abs(seed.GetHashCode());
                byte r = (byte)(hash & 0xFF);
                byte g = (byte)((hash >> 8) & 0xFF);
                byte b = (byte)((hash >> 16) & 0xFF);
                return new SKColor(r, g, b);
            }
            catch
            {
                return SKColors.Gray;
            }
        }


    }
}
