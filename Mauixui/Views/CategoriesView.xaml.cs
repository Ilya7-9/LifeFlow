using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Services;
using Mauixui.Models;

namespace Mauixui.Views
{
    public partial class CategoriesView : ContentView
    {
        private List<CategoryItem> _categories = new();
        private string _profileId;
        private CategoryItem _selectedCategory;
        private MainDatabase _db;

        private List<BudgetItem> _budgets = new();
        private List<FinanceItem> _transactions = new();

        public CategoriesView()
        {
            InitializeComponent();

            var ps = new ProfileService();
            _profileId = ps.GetCurrentProfile().Id;
            _db =  MainDatabase.Instance;

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await LoadBudgetsAndTransactionsAsync();
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            _categories = await _db.GetCategoriesAsync(_profileId);
            RenderCategories();
        }

        private async Task LoadBudgetsAndTransactionsAsync()
        {
            _budgets = await _db.GetBudgetsAsync(_profileId);
            _transactions = await _db.GetItemsAsync(_profileId);

            // Автоматический пересчёт потраченной суммы для бюджетов
            foreach (var budget in _budgets)
            {
                budget.Spent = (double)_transactions
                    .Where(t => t.Type == "Расход" && t.Category == budget.Category)
                    .Sum(t => t.Amount);

                // Обновляем бюджет в базе
                await _db.SaveBudgetAsync(budget);
            }
        }

        private void RenderCategories()
        {
            CategoryList.Children.Clear();

            // Кнопка добавления новой категории
            var addButtonFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#2D2D30"),
                CornerRadius = 12,
                Padding = 10,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var addButton = new Button
            {
                Text = "+ Добавить категорию",
                BackgroundColor = Color.FromArgb("#5865F2"),
                TextColor = Color.FromArgb("#fff"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 40,
                CornerRadius = 8
            };
            addButton.Clicked += ShowCreatePanel;

            addButtonFrame.Content = addButton;
            CategoryList.Children.Add(addButtonFrame);

            if (!_categories.Any())
            {
                CategoryList.Children.Add(new Label
                {
                    Text = "Категорий нет",
                    TextColor = Color.FromArgb("#888888"),
                    HorizontalOptions = LayoutOptions.Center
                });
                return;
            }

            foreach (var cat in _categories)
            {
                // Получаем бюджет для этой категории
                var categoryBudget = _budgets.FirstOrDefault(b => b.Category == cat.Name);
                var hasBudget = categoryBudget != null;

                var frame = new Frame
                {
                    BackgroundColor = (_selectedCategory == cat) ? Color.FromArgb("#5A5F6B") : Color.FromArgb("#40444B"),
                    CornerRadius = 12,
                    Padding = 10,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            new Label
                            {
                                Text = $"{cat.Name} ({cat.Type})",
                                TextColor = Color.FromArgb("#FFFFFF"),
                                FontSize = 14,
                                FontAttributes = hasBudget ? FontAttributes.Bold : FontAttributes.None
                            },
                            hasBudget ? new Label
                            {
                                Text = $"Бюджет: {categoryBudget.Limit:C}",
                                TextColor = Color.FromArgb("#57F287"),
                                FontSize = 12
                            } : null
                        }
                    }
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    await frame.ScaleTo(0.95, 100, Easing.CubicInOut);
                    await frame.ScaleTo(1.0, 100, Easing.CubicInOut);
                    SelectCategory(cat);
                };
                frame.GestureRecognizers.Add(tapGesture);

                CategoryList.Children.Add(frame);
            }
        }

        private void SelectCategory(CategoryItem cat)
        {
            _selectedCategory = cat;
            ShowEditPanel();

            // Заполняем поля данными выбранной категории
            CategoryEntry.Text = cat.Name;
            TypePicker.SelectedItem = cat.Type;

            // Загружаем информацию о бюджете
            LoadBudgetInfo(cat);
        }

        private void LoadBudgetInfo(CategoryItem cat)
        {
            var categoryBudget = _budgets.FirstOrDefault(b => b.Category == cat.Name);

            if (categoryBudget != null)
            {
                BudgetEntry.Text = categoryBudget.Limit.ToString("F2");
                BudgetInfoFrame.IsVisible = true;

                var remaining = categoryBudget.Limit - categoryBudget.Spent;
                var progress = categoryBudget.Limit > 0 ? (categoryBudget.Spent / categoryBudget.Limit) * 100 : 0;

                BudgetInfoLabel.Text = $"Бюджет: {categoryBudget.Limit:C}";
                BudgetDetailsLabel.Text = $"Потрачено: {categoryBudget.Spent:C} | Осталось: {remaining:C} ({progress:F1}%)";

                // Меняем цвет в зависимости от использования бюджета
                BudgetInfoLabel.TextColor = progress > 90 ? Color.FromArgb("#ED4245") :
                                           progress > 70 ? Color.FromArgb("#FEE75C") :
                                           Color.FromArgb("#57F287");
            }
            else
            {
                BudgetEntry.Text = string.Empty;
                BudgetInfoFrame.IsVisible = false;
            }
        }

        private void ShowCreatePanel(object sender, EventArgs e)
        {
            _selectedCategory = null;
            EditPanel.IsVisible = true;
            PanelTitle.Text = "Создание категории";

            // Очищаем поля
            CategoryEntry.Text = string.Empty;
            TypePicker.SelectedIndex = -1;
            BudgetEntry.Text = string.Empty;
            BudgetInfoFrame.IsVisible = false;

            // Настройка видимости кнопок
            SaveButton.IsVisible = true;
            UpdateButton.IsVisible = false;
            DeleteButton.IsVisible = false;
        }

        private void ShowEditPanel()
        {
            EditPanel.IsVisible = true;
            PanelTitle.Text = "Редактирование категории";

            // Настройка видимости кнопок
            SaveButton.IsVisible = false;
            UpdateButton.IsVisible = true;
            DeleteButton.IsVisible = true;
        }

        private async void SaveCategory(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryEntry.Text) || TypePicker.SelectedIndex == -1)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Заполните название и тип категории", "OK");
                return;
            }

            var item = new CategoryItem
            {
                ProfileId = _profileId,
                Name = CategoryEntry.Text.Trim(),
                Type = TypePicker.SelectedItem.ToString()
            };

            await _db.SaveCategoryAsync(item);

            // Если указан бюджет - создаем его
            if (!string.IsNullOrWhiteSpace(BudgetEntry.Text) && double.TryParse(BudgetEntry.Text, out double budgetAmount))
            {
                await CreateBudgetForCategory(item.Name, budgetAmount);
            }

            ClearForm();
            await LoadDataAsync();

            await Application.Current.MainPage.DisplayAlert("Успех", "Категория создана", "OK");
        }

        private async void UpdateCategory(object sender, EventArgs e)
        {
            if (_selectedCategory == null) return;

            if (string.IsNullOrWhiteSpace(CategoryEntry.Text) || TypePicker.SelectedIndex == -1)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Заполните все поля", "OK");
                return;
            }

            // Сохраняем старое название для обновления бюджета
            var oldName = _selectedCategory.Name;

            _selectedCategory.Name = CategoryEntry.Text.Trim();
            _selectedCategory.Type = TypePicker.SelectedItem.ToString();

            await _db.SaveCategoryAsync(_selectedCategory);

            // Обновляем бюджет если он указан в поле ввода
            if (!string.IsNullOrWhiteSpace(BudgetEntry.Text) && double.TryParse(BudgetEntry.Text, out double budgetAmount))
            {
                await CreateBudgetForCategory(_selectedCategory.Name, budgetAmount);
            }

            // Обновляем название категории в бюджете, если он есть
            var existingBudget = _budgets.FirstOrDefault(b => b.Category == oldName);
            if (existingBudget != null && oldName != _selectedCategory.Name)
            {
                existingBudget.Category = _selectedCategory.Name;
                await _db.SaveBudgetAsync(existingBudget);
            }

            ClearForm();
            await LoadDataAsync();

            await Application.Current.MainPage.DisplayAlert("Успех", "Категория обновлена", "OK");
        }

        private async Task CreateBudgetForCategory(string categoryName, double amount)
        {
            var existingBudget = _budgets.FirstOrDefault(b => b.Category == categoryName);

            if (existingBudget != null)
            {
                // Обновляем существующий бюджет
                existingBudget.Limit = amount;
                existingBudget.ResetDate = DateTime.Now.AddMonths(1);
                await _db.SaveBudgetAsync(existingBudget);
            }
            else
            {
                // Создаем новый бюджет
                var budgetItem = new BudgetItem
                {
                    ProfileId = _profileId,
                    Category = categoryName,
                    Limit = amount,
                    Spent = 0,
                    CreatedAt = DateTime.Now,
                    ResetDate = DateTime.Now.AddMonths(1)
                };
                await _db.SaveBudgetAsync(budgetItem);
            }
        }

        private async void DeleteCategory(object sender, EventArgs e)
        {
            if (_selectedCategory == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Подтверждение",
                $"Вы уверены, что хотите удалить категорию \"{_selectedCategory.Name}\"?",
                "Да", "Нет");

            if (confirm)
            {
                // Удаляем связанный бюджет
                var relatedBudget = _budgets.FirstOrDefault(b => b.Category == _selectedCategory.Name);
                if (relatedBudget != null)
                {
                    await _db.DeleteBudgetAsync(relatedBudget);
                }

                await _db.DeleteCategoryAsync(_selectedCategory);
                ClearForm();
                await LoadDataAsync();

                await Application.Current.MainPage.DisplayAlert("Успех", "Категория удалена", "OK");
            }
        }

        private void ClearForm()
        {
            _selectedCategory = null;
            EditPanel.IsVisible = false;
            CategoryEntry.Text = string.Empty;
            TypePicker.SelectedIndex = -1;
            BudgetEntry.Text = string.Empty;
            BudgetInfoFrame.IsVisible = false;
        }
    }
}