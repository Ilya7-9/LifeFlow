using Microsoft.Maui.Controls;
using Mauixui.Services;
using Mauixui.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            var ps = new ProfileService();
            _profileId = ps.GetCurrentProfile().Id;
            _db = ps.GetFinanceDatabase(_profileId);

            PeriodPicker.SelectedIndexChanged += OnPeriodChanged;
            PeriodPicker.SelectedIndex = 0;

            _ = LoadData();
        }

        private async void ReloadStatistics(object sender, EventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            _items = await _db.GetItemsAsync(_profileId);
            BuildStatistics();
        }

        private void OnPeriodChanged(object sender, EventArgs e)
        {
            if (PeriodPicker.SelectedIndex != -1)
                BuildStatistics();
        }

        private void BuildStatistics()
        {
            if (_items == null || !_items.Any())
            {
                ShowNoDataState();
                return;
            }

            var filteredItems = FilterItemsByPeriod();
            UpdateMainMetrics(filteredItems);
            BuildCharts(filteredItems);
            UpdateFinancialHealth(filteredItems);
        }

        private List<FinanceItem> FilterItemsByPeriod()
        {
            var period = PeriodPicker.SelectedItem?.ToString();
            var now = DateTime.Now;

            return period switch
            {
                "Текущий месяц" => _items.Where(i => i.Date.Month == now.Month && i.Date.Year == now.Year).ToList(),
                "Прошлый месяц" => _items.Where(i => i.Date.Month == now.AddMonths(-1).Month && i.Date.Year == now.AddMonths(-1).Year).ToList(),
                "Текущий год" => _items.Where(i => i.Date.Year == now.Year).ToList(),
                _ => _items.Where(i => i.Date.Month == now.Month && i.Date.Year == now.Year).ToList()
            };
        }

        private void UpdateMainMetrics(List<FinanceItem> items)
        {
            decimal totalIncome = items.Where(i => i.Type == "Доход").Sum(i => i.Amount);
            decimal totalExpense = items.Where(i => i.Type == "Расход").Sum(i => i.Amount);
            decimal net = totalIncome - totalExpense;

            int incomeCount = items.Count(i => i.Type == "Доход");
            int expenseCount = items.Count(i => i.Type == "Расход");

            decimal avgIncome = incomeCount > 0 ? totalIncome / incomeCount : 0;
            decimal avgExpense = expenseCount > 0 ? totalExpense / expenseCount : 0;
            decimal savingsRate = totalIncome > 0 ? (net / totalIncome) * 100 : 0;

            TotalIncomeLabel.Text = $"{totalIncome:F2} Br";
            TotalExpenseLabel.Text = $"{totalExpense:F2} Br";
            NetWorthLabel.Text = $"{net:F2} Br";
            SavingsRateLabel.Text = $"{savingsRate:F1}%";
            AvgIncomeLabel.Text = $"{avgIncome:F2} Br";
            AvgExpenseLabel.Text = $"{avgExpense:F2} Br";

            // Цвета в зависимости от значений
            NetWorthLabel.TextColor = net >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B");
            SavingsRateLabel.TextColor = savingsRate >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B");
        }

        private void BuildCharts(List<FinanceItem> items)
        {
            BuildIncomeChart(items);
            BuildExpenseChart(items);
        }

        private void BuildIncomeChart(List<FinanceItem> items)
        {
            IncomeChart.Children.Clear();
            var incomeByCategory = items
                .Where(i => i.Type == "Доход")
                .GroupBy(i => i.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .Take(3)
                .ToList();

            foreach (var category in incomeByCategory)
            {
                var row = CreateChartRow(category.Category, category.Amount, "#23D160");
                IncomeChart.Children.Add(row);
            }
        }

        private void BuildExpenseChart(List<FinanceItem> items)
        {
            ExpenseChart.Children.Clear();
            var expenseByCategory = items
                .Where(i => i.Type == "Расход")
                .GroupBy(i => i.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .Take(3)
                .ToList();

            foreach (var category in expenseByCategory)
            {
                var row = CreateChartRow(category.Category, category.Amount, "#FF4B4B");
                ExpenseChart.Children.Add(row);
            }
        }

        private Grid CreateChartRow(string category, decimal amount, string color)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                HeightRequest = 24
            };

            var categoryLabel = new Label
            {
                Text = category.Length > 15 ? category.Substring(0, 15) + "..." : category,
                TextColor = Color.FromArgb("#FFFFFF"),
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(categoryLabel, 0);

            var amountLabel = new Label
            {
                Text = $"{amount:F2} Br",
                TextColor = Color.FromArgb(color),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(amountLabel, 1);

            grid.Children.Add(categoryLabel);
            grid.Children.Add(amountLabel);

            return grid;
        }

        private void UpdateFinancialHealth(List<FinanceItem> items)
        {
            // Прогноз на месяц
            var currentMonth = DateTime.Now;
            var currentMonthItems = _items.Where(i => i.Date.Month == currentMonth.Month && i.Date.Year == currentMonth.Year).ToList();

            decimal currentIncome = currentMonthItems.Where(i => i.Type == "Доход").Sum(i => i.Amount);
            decimal currentExpense = currentMonthItems.Where(i => i.Type == "Расход").Sum(i => i.Amount);

            int daysPassed = currentMonth.Day;
            int totalDays = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

            decimal dailyIncome = daysPassed > 0 ? currentIncome / daysPassed : 0;
            decimal dailyExpense = daysPassed > 0 ? currentExpense / daysPassed : 0;

            ProjectedIncomeLabel.Text = $"{(dailyIncome * totalDays):F2} Br";
            ProjectedExpenseLabel.Text = $"{(dailyExpense * totalDays):F2} Br";

            decimal projectedNet = (dailyIncome - dailyExpense) * totalDays;
            ProjectedNetLabel.Text = $"{projectedNet:F2} Br";
            ProjectedNetLabel.TextColor = projectedNet >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B");

            // Лучший месяц
            var monthlyPerformance = _items
                .GroupBy(i => new { i.Date.Year, i.Date.Month })
                .Select(g => new
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Net = g.Where(x => x.Type == "Доход").Sum(x => x.Amount) - g.Where(x => x.Type == "Расход").Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Net)
                .FirstOrDefault();

            if (monthlyPerformance != null)
            {
                BestMonthLabel.Text = monthlyPerformance.Period.ToString("MMMM yyyy");
                BestMonthAmount.Text = $"{monthlyPerformance.Net:F2} Br";
            }
        }

        private void ShowNoDataState()
        {
            TotalIncomeLabel.Text = "0 Br";
            TotalExpenseLabel.Text = "0 Br";
            NetWorthLabel.Text = "0 Br";
            SavingsRateLabel.Text = "0%";
            AvgIncomeLabel.Text = "0 Br";
            AvgExpenseLabel.Text = "0 Br";

            IncomeChart.Children.Clear();
            ExpenseChart.Children.Clear();

            ProjectedIncomeLabel.Text = "0 Br";
            ProjectedExpenseLabel.Text = "0 Br";
            ProjectedNetLabel.Text = "0 Br";

            BestMonthLabel.Text = "Нет данных";
            BestMonthAmount.Text = "0 Br";
        }
    }
}