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
        private CategoryDatabase _db;
        private string _profileId;

        private readonly BudgetDatabase _budgetDb;
        private readonly FinanceDatabase _financeDb;
        private readonly CategoryDatabase _categoryDb;

        private List<BudgetItem> _budgets = new();
        private List<FinanceItem> _transactions = new();

        public CategoriesView()
        {
            InitializeComponent();

            var ps = new ProfileService();
            _profileId = ps.GetCurrentProfile().Id;
            _db = ps.GetCategoryDatabase(_profileId);

            LoadCategories();

            _budgetDb = ps.GetBudgetDatabase(_profileId);
            _financeDb = ps.GetFinanceDatabase(_profileId);
            _categoryDb = ps.GetCategoryDatabase(_profileId);

            LoadBudgets();
        }

        private async void LoadCategories()
        {
            _categories = await _db.GetCategoriesAsync(_profileId);
            RenderCategories();
        }

        private async void AddCategory(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryEntry.Text) || TypePicker.SelectedIndex == -1)
                return;

            var item = new CategoryItem
            {
                ProfileId = _profileId,
                Name = CategoryEntry.Text,
                Type = TypePicker.SelectedItem.ToString()
            };

            await _db.SaveCategoryAsync(item);
            CategoryEntry.Text = "";
            TypePicker.SelectedIndex = -1;
            LoadCategories();
        }

        private void RenderCategories()
        {
            CategoryList.Children.Clear();
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
                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#40444B"),
                    CornerRadius = 12,
                    Padding = 10,
                    Content = new Label
                    {
                        Text = $"{cat.Name} ({cat.Type})",
                        TextColor = Color.FromArgb("#FFFFFF"),
                        FontSize = 14
                    }
                };
                CategoryList.Children.Add(frame);
            }
        }

        private async void LoadBudgets()
        {
            _budgets = await _budgetDb.GetBudgetsAsync(_profileId);
            _transactions = await _financeDb.GetItemsAsync(_profileId);
            _categories = await _categoryDb.GetCategoriesAsync(_profileId);

            // Автоматический пересчёт потраченной суммы
            foreach (var b in _budgets)
            {
                b.Spent = (double)_transactions
                    .Where(t => t.Type == "Расход" && t.Category == b.Category)
                    .Sum(t => t.Amount);
            }

            BudgetsList.ItemsSource = _budgets;
        }

        private async void AddBudgetClicked(object sender, EventArgs e)
        {
            string category = await Application.Current.MainPage.DisplayActionSheet(
                "Выберите категорию для бюджета:",
                "Отмена", null,
                _categories.Select(c => c.Name).ToArray()
            );

            if (string.IsNullOrEmpty(category))
                return;

            string limitStr = await Application.Current.MainPage.DisplayPromptAsync(
                "Лимит",
                "Введите сумму бюджета:",
                keyboard: Keyboard.Numeric);

            if (!double.TryParse(limitStr, out double limit))
                return;

            var item = new BudgetItem
            {
                ProfileId = _profileId,
                Category = category,
                Limit = limit,
                CreatedAt = DateTime.Now,
                ResetDate = DateTime.Now.AddMonths(1)
            };

            await _budgetDb.SaveBudgetAsync(item);
            LoadBudgets();
        }

        private async void DeleteBudgetClicked(object sender, EventArgs e)
        {
            var item = (sender as Button).CommandParameter as BudgetItem;

            if (item == null) return;

            await _budgetDb.DeleteBudgetAsync(item);
            LoadBudgets();
        }
    }
}
