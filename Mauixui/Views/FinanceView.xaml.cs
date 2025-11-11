using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using Microcharts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Views;

namespace Mauixui.Views
{
    public partial class FinanceView : ContentView
    {
        private List<FinanceItem> _items = new();
        private FinanceDatabase _db;
        private string _profileId;

        public FinanceView()
        {
            InitializeComponent();

            var profileService = new ProfileService();
            _profileId = profileService.GetCurrentProfile().Id;
            _db = profileService.GetFinanceDatabase(_profileId);

            LoadFinanceItems();
        }

        // === Добавление дохода ===
        private async void AddIncomeClicked(object sender, EventArgs e)
        {
            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите корректную сумму", "OK");
                return;
            }

            var item = new FinanceItem
            {
                ProfileId = _profileId,
                Type = "Доход",
                Category = "Общие",
                Description = "Пополнение",
                Amount = amount,
                Date = DateTime.Now
            };

            await _db.SaveItemAsync(item);
            AmountEntry.Text = "";
            LoadFinanceItems();
        }

        // === Добавление расхода ===
        private async void AddExpenseClicked(object sender, EventArgs e)
        {
            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите корректную сумму", "OK");
                return;
            }

            var item = new FinanceItem
            {
                ProfileId = _profileId,
                Type = "Расход",
                Category = "Общие",
                Description = "Покупка",
                Amount = amount,
                Date = DateTime.Now
            };

            await _db.SaveItemAsync(item);
            AmountEntry.Text = "";
            LoadFinanceItems();
        }

        private async void LoadFinanceItems()
        {
            _items = await _db.GetItemsAsync(_profileId);
            RenderFinanceItems();
            UpdateBalance();
            UpdateChart();
        }

        // === Рендер списка операций ===
        private void RenderFinanceItems(List<FinanceItem>? listOverride = null)
        {
            var list = listOverride ?? _items;
            FinanceList.Children.Clear();

            if (!list.Any())
            {
                FinanceList.Children.Add(new Label
                {
                    Text = "Пока нет записей",
                    TextColor = Color.FromArgb("#888888"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 30, 0, 0)
                });
                return;
            }

            foreach (var item in list.OrderByDescending(i => i.Date))
            {
                var color = item.Type == "Доход" ? "#23D160" : "#FF4B4B";

                var frame = new Frame
                {
                    CornerRadius = 12,
                    BackgroundColor = Color.FromArgb("#40444B"),
                    Padding = 12,
                    HasShadow = true,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };

                var desc = new Label
                {
                    Text = $"{item.Description} ({item.Category})\n{item.Date:dd.MM.yyyy HH:mm}",
                    TextColor = Color.FromArgb("#FFFFFF"),
                    FontSize = 13
                };
                Grid.SetColumn(desc, 0);

                var amountLabel = new Label
                {
                    Text = $"{(item.Type == "Доход" ? "+" : "-")}{item.Amount:F2} ₽",
                    TextColor = Color.FromArgb(color),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(amountLabel, 1);

                grid.Children.Add(desc);
                grid.Children.Add(amountLabel);

                frame.Content = grid;
                FinanceList.Children.Add(frame);
            }
        }

        // === Расчёт и отображение баланса ===
        private void UpdateBalance()
        {
            decimal income = _items.Where(i => i.Type == "Доход").Sum(i => i.Amount);
            decimal expenses = _items.Where(i => i.Type == "Расход").Sum(i => i.Amount);
            decimal balance = income - expenses;

            BalanceLabel.Text = $"Баланс: {balance:F2} ₽";
            BalanceLabel.TextColor = balance >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B");
        }

        // === Обновление диаграммы ===
        private void UpdateChart()
        {
            var grouped = _items
                .Where(i => i.Type == "Расход")
                .GroupBy(i => i.Category)
                .Select(g => new { Category = g.Key, Sum = g.Sum(i => i.Amount) })
                .ToList();

            if (!grouped.Any())
            {
                ChartView.Chart = null;
                return;
            }

            var entries = grouped.Select(g => new ChartEntry((float)g.Sum)
            {
                Label = g.Category,
                ValueLabel = $"{g.Sum:F0}",
                Color = SKColor.Parse(RandomColor(g.Category))
            }).ToList();

            ChartView.Chart = new DonutChart
            {
                Entries = entries,
                LabelTextSize = 28,
                BackgroundColor = SKColors.Transparent
            };
        }

        private string RandomColor(string seed)
        {
            var hash = seed.GetHashCode();
            var r = (byte)(hash & 0xFF);
            var g = (byte)((hash >> 8) & 0xFF);
            var b = (byte)((hash >> 16) & 0xFF);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // 🔹 Очистка полей
        private void ClearFields(object sender, EventArgs e)
        {
            TypePicker.SelectedIndex = -1;
            CategoryPicker.SelectedIndex = -1;
            DescriptionEntry.Text = "";
            AmountEntry.Text = "";
            DatePicker.Date = DateTime.Today;
        }

        // 🔹 Добавление записи
        private async void AddFinanceItem(object sender, EventArgs e)
        {
            if (TypePicker.SelectedIndex == -1 || CategoryPicker.SelectedIndex == -1)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Выберите тип и категорию", "OK");
                return;
            }

            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите корректную сумму", "OK");
                return;
            }

            var item = new FinanceItem
            {
                ProfileId = _profileId,
                Type = TypePicker.SelectedItem.ToString(),
                Category = CategoryPicker.SelectedItem.ToString(),
                Description = string.IsNullOrEmpty(DescriptionEntry.Text) ? "Без описания" : DescriptionEntry.Text,
                Amount = amount,
                Date = DatePicker.Date
            };

            await _db.SaveItemAsync(item);
            ClearFields(null, null);
            LoadFinanceItems();
        }

        private void ShowInnerView(ContentView view)
        {
            // Скрываем главный экран
            FinanceHome.IsVisible = false;

            // Показываем внутренний контейнер
            InnerViewContainer.IsVisible = true;

            // Подменяем контент
            InnerViewContent.Content = view;
        }

        private void GoBackToFinance(object sender, EventArgs e)
        {
            // Возврат к основному экрану
            InnerViewContainer.IsVisible = false;
            InnerViewContent.Content = null;
            FinanceHome.IsVisible = true;
        }

        private void OpenCategories(object sender, EventArgs e)
        {
            ShowInnerView(new CategoriesView());
        }

        private void OpenBudgets(object sender, EventArgs e)
        {
            ShowInnerView(new BudgetsView());
        }

        private void OpenAssets(object sender, EventArgs e)
        {
            ShowInnerView(new AssetsView());
        }

        private void OpenStatistics(object sender, EventArgs e)
        {
            ShowInnerView(new StatisticsView());
        }


    }
}
