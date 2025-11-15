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
        private CategoryDatabase _categoryDb;
        private List<CategoryItem> _categories = new();

        public FinanceView()
        {
            InitializeComponent();

            var profileService = new ProfileService();
            _profileId = profileService.GetCurrentProfile().Id;
            _db = profileService.GetFinanceDatabase(_profileId);
            _categoryDb = profileService.GetCategoryDatabase(_profileId);

            LoadFinanceItems();
            LoadCategories();
        }

        private async void LoadCategories()
        {
            try
            {
                _categories = await _categoryDb.GetCategoriesAsync(_profileId);

                bool hasDefault = _categories.Any(c => c.Name == "Без категории");

                if (!hasDefault)
                {
                    var defaultCategory = new CategoryItem
                    {
                        ProfileId = _profileId,
                        Name = "Без категории",
                        Type = "Расход"
                    };
                    await _categoryDb.SaveCategoryAsync(defaultCategory);

                    _categories = await _categoryDb.GetCategoriesAsync(_profileId);
                }

                CategoryPicker.Items.Clear();
                foreach (var cat in _categories)
                {
                    CategoryPicker.Items.Add(cat.Name);
                }

                var defaultIndex = CategoryPicker.Items.IndexOf("Без категории");
                CategoryPicker.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось загрузить категории:\n{ex.Message}", "OK");
            }
        }

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
        }

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

                var layout = new VerticalStackLayout { Spacing = 8 };

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
                    Text = $"{item.Description} ({item.Category})\n{item.Date:dd.MM.yyyy}",
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

                layout.Children.Add(grid);

                // ДОБАВЛЕНА КНОПКА УДАЛЕНИЯ
                var deleteBtn = new Button
                {
                    Text = "Удалить",
                    BackgroundColor = Color.FromArgb("#FF4B4B"),
                    TextColor = Color.FromArgb("fff"),
                    CornerRadius = 10,
                    CommandParameter = item,
                    HorizontalOptions = LayoutOptions.End
                };
                deleteBtn.Clicked += DeleteOperationClicked;

                layout.Children.Add(deleteBtn);

                frame.Content = layout;
                FinanceList.Children.Add(frame);
            }
        }

        private async void DeleteOperationClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is FinanceItem item)
            {
                bool ok = await Application.Current.MainPage.DisplayAlert(
                    "Удаление",
                    $"Удалить запись '{item.Description}'?",
                    "Удалить", "Отмена");

                if (!ok) return;

                await _db.DeleteItemAsync(item);

                LoadFinanceItems();
            }
        }

        private void UpdateBalance()
        {
            decimal income = _items.Where(i => i.Type == "Доход").Sum(i => i.Amount);
            decimal expenses = _items.Where(i => i.Type == "Расход").Sum(i => i.Amount);
            decimal balance = income - expenses;

            BalanceLabel.Text = $"Баланс: {balance:F2} ₽";
            BalanceLabel.TextColor = balance >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B");
        }

        private void ClearFields(object sender, EventArgs e)
        {
            TypePicker.SelectedIndex = -1;
            CategoryPicker.SelectedIndex = -1;
            DescriptionEntry.Text = "";
            AmountEntry.Text = "";
            DatePicker.Date = DateTime.Today;
        }

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
            FinanceHome.IsVisible = false;
            InnerViewContainer.IsVisible = true;
            InnerViewContent.Content = view;
        }

        private void GoBackToFinance(object sender, EventArgs e)
        {
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
