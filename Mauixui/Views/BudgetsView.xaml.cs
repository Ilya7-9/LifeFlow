using Microsoft.Maui.Controls;
using Mauixui.Services;
using Mauixui.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Views
{
    public partial class BudgetsView : ContentView
    {
        private readonly BudgetDatabase _budgetDb;
        private readonly FinanceDatabase _financeDb;
        private readonly CategoryDatabase _categoryDb;
        private readonly string _profileId;

        private List<BudgetItem> _budgets = new();
        private List<FinanceItem> _transactions = new();
        private List<CategoryItem> _categories = new();

        public BudgetsView()
        {
            InitializeComponent();

            var ps = new ProfileService();
            _profileId = ps.GetCurrentProfile().Id;

            _budgetDb = ps.GetBudgetDatabase(_profileId);
            _financeDb = ps.GetFinanceDatabase(_profileId);
            _categoryDb = ps.GetCategoryDatabase(_profileId);

            LoadBudgets();
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
