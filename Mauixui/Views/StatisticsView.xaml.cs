using Microsoft.Maui.Controls;
using Mauixui.Services;
using Mauixui.Models;
using System;
using System.Linq;

namespace Mauixui.Views
{
    public partial class StatisticsView : ContentView
    {
        private FinanceDatabase _db;
        private string _profileId;
        private System.Collections.Generic.List<FinanceItem> _items = new();

        public StatisticsView()
        {
            InitializeComponent();
            var ps = new ProfileService();
            _profileId = ps.GetCurrentProfile().Id;
            _db = ps.GetFinanceDatabase(_profileId);

            // Загрузка при создании
            ReloadStatistics(null, null);
        }

        private async void ReloadStatistics(object sender, EventArgs e)
        {
            try
            {
                _items = await _db.GetItemsAsync(_profileId);
                BuildSimpleStatistics();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void BuildSimpleStatistics()
        {
            // Очистка списков
            TopExpensesList.Children.Clear();
            TopIncomeList.Children.Clear();

            if (_items == null || !_items.Any())
            {
                SummaryIncome.Text = "Нет данных";
                SummaryExpense.Text = "";
                SummaryNet.Text = "";
                PredictionLabel.Text = "";
                BestMonth.Text = "";
                WorstMonth.Text = "";
                return;
            }

            decimal income = _items.Where(i => i.Type == "Доход").Sum(i => i.Amount);
            decimal expense = _items.Where(i => i.Type == "Расход").Sum(i => i.Amount);
            decimal net = income - expense;

            SummaryIncome.Text = $"📈 Доход: {income:F2} ₽";
            SummaryExpense.Text = $"📉 Расход: {expense:F2} ₽";
            SummaryNet.Text = $"💵 Чистый итог: {net:F2} ₽";

            var topInc = _items.Where(i => i.Type == "Доход")
                               .GroupBy(i => i.Category)
                               .OrderByDescending(g => g.Sum(x => x.Amount))
                               .Take(5);

            foreach (var g in topInc)
                TopIncomeList.Children.Add(new Label { Text = $"{g.Key}: {g.Sum(x => x.Amount):F2} ₽", TextColor = Color.FromArgb("#FFFFFF") });

            var topExp = _items.Where(i => i.Type == "Расход")
                               .GroupBy(i => i.Category)
                               .OrderByDescending(g => g.Sum(x => x.Amount))
                               .Take(5);

            foreach (var g in topExp)
                TopExpensesList.Children.Add(new Label { Text = $"{g.Key}: {g.Sum(x => x.Amount):F2} ₽", TextColor = Color.FromArgb("#FFFFFF") });

            // Лучший/худший месяц
            var byMonth = _items.GroupBy(i => new DateTime(i.Date.Year, i.Date.Month, 1))
                                .Select(g => new { Month = g.Key, Net = g.Where(x => x.Type == "Доход").Sum(x => x.Amount) - g.Where(x => x.Type == "Расход").Sum(x => x.Amount) })
                                .OrderByDescending(x => x.Net)
                                .ToList();

            var best = byMonth.FirstOrDefault();
            var worst = byMonth.OrderBy(x => x.Net).FirstOrDefault();
            BestMonth.Text = best != null ? $"Лучший: {best.Month:MMMM yyyy} — {best.Net:F2} ₽" : "";
            WorstMonth.Text = worst != null ? $"Худший: {worst.Month:MMMM yyyy} — {worst.Net:F2} ₽" : "";

            // Прогноз на месяц (упрощённый)
            var current = _items.Where(i => i.Date.Month == DateTime.Now.Month && i.Date.Year == DateTime.Now.Year).ToList();
            int day = DateTime.Now.Day;
            if (day > 0)
            {
                decimal currIncome = current.Where(i => i.Type == "Доход").Sum(i => i.Amount);
                decimal currExpense = current.Where(i => i.Type == "Расход").Sum(i => i.Amount);
                decimal dailyNet = (currIncome - currExpense) / day;
                int daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                decimal forecast = dailyNet * daysInMonth;
                PredictionLabel.Text = $"Прогноз чистого дохода за месяц: {forecast:F2} ₽";
            }
            else
            {
                PredictionLabel.Text = "";
            }
        }
    }
}
