using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Services;
using Mauixui.Models;

namespace Mauixui.Views
{
    public partial class BudgetsView : ContentView
    {
        private List<Budget> _budgets = new();
        private BudgetDatabase _db;
        private string _profileId;

        public BudgetsView()
        {
            InitializeComponent();

            var profileService = new ProfileService();
            _profileId = profileService.GetCurrentProfile().Id;
            _db = profileService.GetBudgetDatabase(_profileId);

            LoadBudgets();
        }

        private async void LoadBudgets()
        {
            _budgets = await _db.GetBudgetsAsync(_profileId);
            RenderBudgets();
        }

        private async void AddBudget(object sender, EventArgs e)
        {
            if (!decimal.TryParse(BudgetLimitEntry.Text, out decimal limit)) return;

            var budget = new Budget
            {
                ProfileId = _profileId,
                Name = BudgetNameEntry.Text,
                Limit = limit,
                StartDate = StartDatePicker.Date,
                EndDate = EndDatePicker.Date
            };

            await _db.SaveBudgetAsync(budget);
            LoadBudgets();
        }

        private void RenderBudgets()
        {
            BudgetList.Children.Clear();

            if (!_budgets.Any())
            {
                BudgetList.Children.Add(new Label
                {
                    Text = "Нет бюджетов",
                    TextColor = Color.FromArgb("#888888"),
                    HorizontalOptions = LayoutOptions.Center
                });
                return;
            }

            foreach (var b in _budgets)
            {
                var progress = Math.Min(1.0, (double)b.CurrentSpending / (double)b.Limit);
                var bar = new ProgressBar { Progress = progress, ProgressColor = Color.FromArgb("#23D160") };

                var stack = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label { Text = $"{b.Name} — {b.CurrentSpending:F0}/{b.Limit:F0} ₽", TextColor = Color.FromArgb("#FFFFFF") },
                        bar
                    }
                };

                var frame = new Frame
                {
                    CornerRadius = 12,
                    BackgroundColor = Color.FromArgb("#40444B"),
                    Padding = 10,
                    Content = stack
                };
                BudgetList.Children.Add(frame);
            }
        }
    }
}
