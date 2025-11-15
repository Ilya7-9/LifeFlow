using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Mauixui.Views
{
    public partial class AssetsView : ContentView
    {
        private AssetDatabase _assetDb;
        private DebtDatabase _debtDb;
        private string _profileId;

        private List<AssetItem> _assets = new();
        private List<DebtItem> _debts = new();

        public AssetsView()
        {
            InitializeComponent();

            var ps = new ProfileService();
            _profileId = ps.GetCurrentProfile().Id;
            _assetDb = ps.GetAssetDatabase(_profileId);
            _debtDb = ps.GetDebtDatabase(_profileId);

            // По умолчанию — актив
            TypePicker.SelectedIndexChanged += (s, e) =>
            {
                DebtDirectionPicker.IsVisible = TypePicker.SelectedItem?.ToString() == "Долг";
            };


            LoadAll();
        }

        private async void LoadAll()
        {
            await LoadAssetsAsync();
            await LoadDebtsAsync();
            RecalculateTotals();
        }

        private async Task LoadAssetsAsync()
        {
            _assets = await _assetDb.GetAssetsAsync(_profileId);
            RenderAssets();
        }

        private async Task LoadDebtsAsync()
        {
            _debts = await _debtDb.GetDebtsAsync(_profileId);
            RenderDebts();
        }

        private void RenderAssets()
        {
            AssetsList.Children.Clear();

            if (_assets == null || _assets.Count == 0)
            {
                AssetsList.Children.Add(new Label { Text = "Активов нет", TextColor = Color.FromArgb("#888888"), HorizontalOptions = LayoutOptions.Center });
                return;
            }

            foreach (var a in _assets.OrderByDescending(x => x.DateAcquired))
            {
                var frame = new Frame { BackgroundColor = Color.FromArgb("#2D2D30"), CornerRadius = 10, Padding = 10, Margin = new Thickness(0, 0, 0, 4) };
                var stack = new VerticalStackLayout { Spacing = 4 };

                stack.Children.Add(new Label { Text = a.Name, TextColor = Color.FromArgb("#FFFFFF"), FontSize = 16, FontAttributes = FontAttributes.Bold });
                stack.Children.Add(new Label { Text = $"{a.Category} • {a.DateAcquired:dd.MM.yyyy}", TextColor = Color.FromArgb("#BBBBBB"), FontSize = 12 });
                stack.Children.Add(new Label { Text = $"{a.Value:F2} ₽", TextColor = Color.FromArgb("#23D160"), FontSize = 14 });

                var h = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End };
                var editBtn = new Button { Text = "Изм.", BackgroundColor = Color.FromArgb("#40444B"), TextColor = Color.FromArgb("#FFFFFF"), CornerRadius = 8, CommandParameter = a };
                var delBtn = new Button { Text = "Удал.", BackgroundColor = Color.FromArgb("#8B0000"), TextColor = Color.FromArgb("#FFFFFF"), CornerRadius = 8, CommandParameter = a };

                editBtn.Clicked += EditAssetClicked;
                delBtn.Clicked += DeleteAssetClicked;

                h.Children.Add(editBtn);
                h.Children.Add(delBtn);

                stack.Children.Add(h);
                frame.Content = stack;
                AssetsList.Children.Add(frame);
            }
        }

        private void RenderDebts()
        {
            DebtsList.Children.Clear();

            if (_debts == null || _debts.Count == 0)
            {
                DebtsList.Children.Add(new Label { Text = "Долгов нет", TextColor = Color.FromArgb("#888888"), HorizontalOptions = LayoutOptions.Center });
                return;
            }

            foreach (var d in _debts.OrderBy(x => x.DueDate))
            {
                var frame = new Frame { BackgroundColor = Color.FromArgb("#2D2D30"), CornerRadius = 10, Padding = 10, Margin = new Thickness(0, 0, 0, 4) };
                var stack = new VerticalStackLayout { Spacing = 4 };

                stack.Children.Add(new Label
                {
                    Text = $"{d.Party} • {d.Direction}",
                    TextColor = Color.FromArgb("#FFFFFF"),
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold
                });

                stack.Children.Add(new Label { Text = $"{d.Party} • {d.Type}", TextColor = Color.FromArgb("#FFFFFF"), FontSize = 15, FontAttributes = FontAttributes.Bold });
                stack.Children.Add(new Label { Text = $"Сумма: {d.Amount:F2} ₽  •  Срок: {d.DueDate:dd.MM.yyyy}", TextColor = Color.FromArgb("#BBBBBB"), FontSize = 12 });
                if (d.InterestPercent > 0)
                    stack.Children.Add(new Label { Text = $"Процент: {d.InterestPercent:F2} %", TextColor = Color.FromArgb("#BBBBBB"), FontSize = 12 });

                var h = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End };
                var editBtn = new Button { Text = "Изм.", BackgroundColor = Color.FromArgb("#40444B"), TextColor = Color.FromArgb("#FFFFFF"), CornerRadius = 8, CommandParameter = d };
                var delBtn = new Button { Text = "Удал.", BackgroundColor = Color.FromArgb("#8B0000"), TextColor = Color.FromArgb("#FFFFFF"), CornerRadius = 8, CommandParameter = d };

                editBtn.Clicked += EditDebtClicked;
                delBtn.Clicked += DeleteDebtClicked;

                h.Children.Add(editBtn);
                h.Children.Add(delBtn);

                stack.Children.Add(h);

                frame.Content = stack;
                DebtsList.Children.Add(frame);
            }
        }

        private void RecalculateTotals()
        {
            decimal totalAssets = _assets?.Sum(a => a.Value) ?? 0m;
            decimal totalDebts = _debts?
                .Where(d => d.Direction == "Я должен")
                .Sum(d => d.Amount) ?? 0m;

            decimal totalReceivables = _debts?
                .Where(d => d.Direction == "Мне должны")
                .Sum(d => d.Amount) ?? 0m;

            decimal net = (totalAssets + totalReceivables) - totalDebts;


            TotalDebtsLabel.Text = $"{totalDebts:F2} ₽";
            TotalAssetsLabel.Text = $"{totalAssets + totalReceivables:F2} ₽";
            NetWorthLabel.Text = $"{net:F2} ₽";


            // Цвет чистого капитала
            NetWorthLabel.TextColor = net >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B");
        }

        private void ClearFormClicked(object sender, EventArgs e)
        {
            NameEntry.Text = "";
            ValueEntry.Text = "";
            NotesEntry.Text = "";
            InterestEntry.Text = "";
            DatePicker.Date = DateTime.Today;
            TypePicker.SelectedIndex = 0;
            CategoryPicker.SelectedIndex = 0;
        }

        private async void AddItemClicked(object sender, EventArgs e)
        {
            // Валидация суммы
            if (!decimal.TryParse(ValueEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) || value < 0)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите корректную сумму.", "OK");
                return;
            }

            var isAsset = TypePicker.SelectedItem?.ToString() == "Актив";

            if (isAsset)
            {
                var asset = new AssetItem
                {
                    ProfileId = _profileId,
                    Name = string.IsNullOrWhiteSpace(NameEntry.Text) ? "Без названия" : NameEntry.Text,
                    Category = CategoryPicker.SelectedItem?.ToString() ?? "Другое",
                    Value = value,
                    DateAcquired = DatePicker.Date,
                    Notes = NotesEntry.Text ?? ""
                };

                await _assetDb.SaveAssetAsync(asset);
                await LoadAssetsAsync();
            }
            else if (TypePicker.SelectedItem.ToString() == "Долг")
            {
                if (DebtDirectionPicker.SelectedIndex == -1)
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Выберите направление долга.", "OK");
                    return;
                }

                string direction = DebtDirectionPicker.SelectedItem.ToString();

                if (!double.TryParse(InterestEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
                    pct = 0.0;

                var debt = new DebtItem
                {
                    ProfileId = _profileId,
                    Party = string.IsNullOrWhiteSpace(NameEntry.Text) ? "Не указано" : NameEntry.Text,
                    Type = CategoryPicker.SelectedItem?.ToString() ?? "Долг",
                    Amount = value,
                    DueDate = DatePicker.Date,
                    InterestPercent = pct,
                    Direction = direction,
                    Notes = NotesEntry.Text ?? ""
                };

                await _debtDb.SaveDebtAsync(debt);
                await LoadDebtsAsync();
            }
            else
            {
                // долг
                if (!double.TryParse(InterestEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
                    pct = 0.0;

                var debt = new DebtItem
                {
                    ProfileId = _profileId,
                    Party = string.IsNullOrWhiteSpace(NameEntry.Text) ? "Не указано" : NameEntry.Text,
                    Type = CategoryPicker.SelectedItem?.ToString() ?? "Займ",
                    Amount = value,
                    DueDate = DatePicker.Date,
                    InterestPercent = pct,
                    Notes = NotesEntry.Text ?? ""
                };

                await _debtDb.SaveDebtAsync(debt);
                await LoadDebtsAsync();
            }

            RecalculateTotals();
            ClearFormClicked(null, null);
        }

        // ============== EDIT / DELETE ASSET ==============
        private async void EditAssetClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(btn.CommandParameter is AssetItem asset)) return;

            // Простой модальный ввод - можно заменить на полноценную страницу редактирования
            string newName = await Application.Current.MainPage.DisplayPromptAsync("Изменить название", "Название:", initialValue: asset.Name);
            if (string.IsNullOrEmpty(newName)) return;

            string newValueStr = await Application.Current.MainPage.DisplayPromptAsync("Изменить сумму", "Сумма:", initialValue: asset.Value.ToString(CultureInfo.InvariantCulture), keyboard: Keyboard.Numeric);
            if (!decimal.TryParse(newValueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal newVal)) return;

            asset.Name = newName;
            asset.Value = newVal;

            await _assetDb.SaveAssetAsync(asset);
            await LoadAssetsAsync();
            RecalculateTotals();
        }

        private async void DeleteAssetClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(btn.CommandParameter is AssetItem asset)) return;

            bool ok = await Application.Current.MainPage.DisplayAlert("Удалить", $"Удалить актив '{asset.Name}'?", "Да", "Нет");
            if (!ok) return;

            await _assetDb.DeleteAssetAsync(asset);
            await LoadAssetsAsync();
            RecalculateTotals();
        }

        // ============== EDIT / DELETE DEBT ==============
        private async void EditDebtClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(btn.CommandParameter is DebtItem debt)) return;

            string newParty = await Application.Current.MainPage.DisplayPromptAsync("Изменить", "Кто/Кому:", initialValue: debt.Party);
            if (string.IsNullOrEmpty(newParty)) return;

            string newAmountStr = await Application.Current.MainPage.DisplayPromptAsync("Изменить сумму", "Сумма:", initialValue: debt.Amount.ToString(CultureInfo.InvariantCulture), keyboard: Keyboard.Numeric);
            if (!decimal.TryParse(newAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal newAmt)) return;

            debt.Party = newParty;
            debt.Amount = newAmt;

            await _debtDb.SaveDebtAsync(debt);
            await LoadDebtsAsync();
            RecalculateTotals();
        }

        private async void DeleteDebtClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(btn.CommandParameter is DebtItem debt)) return;

            bool ok = await Application.Current.MainPage.DisplayAlert("Удалить", $"Удалить долг '{debt.Party}'?", "Да", "Нет");
            if (!ok) return;

            await _debtDb.DeleteDebtAsync(debt);
            await LoadDebtsAsync();
            RecalculateTotals();
        }
    }
}
