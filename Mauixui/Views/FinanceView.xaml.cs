using Mauixui.Services;
using Microsoft.Maui.Controls;
using Mauixui.Models;
using Microcharts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        private async void LoadFinanceItems()
        {
            _items = await _db.GetItemsAsync(_profileId);
            RenderFinanceItems();
            UpdateChart();
        }

        private async void AddFinanceItem(object sender, EventArgs e)
        {
            if (decimal.TryParse(AmountEntry.Text, out decimal amount) && amount > 0)
            {
                var item = new FinanceItem
                {
                    ProfileId = _profileId,
                    Type = "Расход",
                    Category = "Общие",
                    Description = "Без описания",
                    Amount = amount,
                    Date = DateTime.Now
                };

                await _db.SaveItemAsync(item);
                AmountEntry.Text = string.Empty;

                LoadFinanceItems();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите корректную сумму.", "OK");
            }
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            string q = e.NewTextValue?.Trim().ToLower() ?? "";
            var filtered = _items
                .Where(i => i.Category.ToLower().Contains(q) || i.Description.ToLower().Contains(q))
                .ToList();
            RenderFinanceItems(filtered);
            UpdateChart(filtered);
        }

        private void RenderFinanceItems(List<FinanceItem>? listOverride = null)
        {
            var list = listOverride ?? _items;
            FinanceList.Children.Clear();

            if (!list.Any())
            {
                FinanceList.Children.Add(new Label
                {
                    Text = "💸 Нет записей о доходах или расходах",
                    TextColor = Color.FromArgb("#888888"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            foreach (var item in list.OrderByDescending(i => i.Date))
            {
                // Цвет суммы в зависимости от типа
                var color = item.Type == "Доход" ? "#23D160" : "#FF4B4B";

                // Контейнер для одной записи
                var frame = new Frame
                {
                    CornerRadius = 12,
                    BackgroundColor = Color.FromArgb("#40444B"),
                    HasShadow = true,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                // Сетка для размещения текста и суммы
                var grid = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
                    RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
                };

                // Название категории и описание
                var categoryLabel = new Label
                {
                    Text = $"{item.Category}",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FFFFFF")
                };
                Grid.SetColumn(categoryLabel, 0);
                Grid.SetRow(categoryLabel, 0);
                grid.Children.Add(categoryLabel);

                var descriptionLabel = new Label
                {
                    Text = $"{item.Description}",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#BBBBBB")
                };
                Grid.SetColumn(descriptionLabel, 0);
                Grid.SetRow(descriptionLabel, 1);
                grid.Children.Add(descriptionLabel);

                // Сумма справа
                var amountLabel = new Label
                {
                    Text = $"{(item.Type == "Доход" ? "+" : "-")}{item.Amount:F2} ₽",
                    TextColor = Color.FromArgb(color),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(amountLabel, 1);
                Grid.SetRowSpan(amountLabel, 2);
                grid.Children.Add(amountLabel);

                // Дата под строкой описания
                var dateLabel = new Label
                {
                    Text = item.Date.ToString("dd.MM.yyyy HH:mm"),
                    FontSize = 11,
                    TextColor = Color.FromArgb("#888888"),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                Grid.SetColumn(dateLabel, 0);
                Grid.SetRow(dateLabel, 2);
                grid.Children.Add(dateLabel);

                frame.Content = grid;
                FinanceList.Children.Add(frame);
            }
        }


        private void UpdateChart(List<FinanceItem>? listOverride = null)
        {
            var list = listOverride ?? _items;
            var grouped = list
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
                LabelTextSize = 32,
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
    }
}
