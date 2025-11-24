using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Timers;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui.Storage;
using System.Collections.Generic;

namespace Mauixui.Views
{
    public partial class ProfileView : ContentView
    {
        private ProfileService _profileService;
        private AuthService _authService;
        private System.Timers.Timer _refreshTimer;
        private UserProfile _currentProfile;

        public ProfileView()
        {
            InitializeComponent();
            _profileService = new ProfileService();
            _authService = new AuthService(_profileService);

            LoadProfileData();
            SetupRefreshTimer();
            LoadFinancialStats();
            LoadTrackerStats();
        }

        private void SetupRefreshTimer()
        {
            _refreshTimer = new System.Timers.Timer(5000);
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            RefreshProfileData();
        }

        private void RefreshProfileData()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                LoadProfileData();
                LoadFinancialStats();
                LoadTrackerStats();
            });
        }

        // ЗАГРУЗКА СТАТИСТИКИ ФИНАНСОВ - ИСПРАВЛЕННАЯ
        private async void LoadFinancialStats()
        {
            try
            {
                if (_currentProfile == null) return;

                var financeDb = _profileService.GetFinanceDatabase(_currentProfile.Id);
                var assetDb = _profileService.GetAssetDatabase(_currentProfile.Id);
                var debtDb = _profileService.GetDebtDatabase(_currentProfile.Id);

                // ИСПРАВЛЕНИЕ: Получаем все данные и фильтруем по profileId
                var allFinances = await financeDb.GetItemsAsync();
                var allAssets = await assetDb.GetItemsAsync();
                var allDebts = await debtDb.GetItemsAsync();

                // Фильтруем по текущему профилю
                var profileFinances = allFinances.Where(f => f.ProfileId == _currentProfile.Id).ToList();
                var profileAssets = allAssets.Where(a => a.ProfileId == _currentProfile.Id).ToList();
                var profileDebts = allDebts.Where(d => d.ProfileId == _currentProfile.Id).ToList();

                var totalIncome = profileFinances.Where(f => f.Type == "Доход").Sum(f => f.Amount);
                var totalExpenses = profileFinances.Where(f => f.Type == "Расход").Sum(f => f.Amount);
                var totalAssets = profileAssets.Sum(a => a.Value);
                var totalDebts = profileDebts.Sum(d => d.Amount);
                var netWorth = totalAssets - totalDebts;

                Device.BeginInvokeOnMainThread(() =>
                {
                    IncomeLabel.Text = $"{totalIncome:N0}₽";
                    ExpensesLabel.Text = $"{totalExpenses:N0}₽";
                    AssetsLabel.Text = $"{totalAssets:N0}₽";
                    NetWorthLabel.Text = $"{netWorth:N0}₽";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading financial stats: {ex.Message}");
                // Устанавливаем значения по умолчанию
                Device.BeginInvokeOnMainThread(() =>
                {
                    IncomeLabel.Text = "0₽";
                    ExpensesLabel.Text = "0₽";
                    AssetsLabel.Text = "0₽";
                    NetWorthLabel.Text = "0₽";
                });
            }
        }

        // ЗАГРУЗКА СТАТИСТИКИ ТРЕКЕРА - ИСПРАВЛЕННАЯ
        private async void LoadTrackerStats()
        {
            try
            {
                if (_currentProfile == null) return;

                var trackerDb = _profileService.GetTrackerDatabase(_currentProfile.Id);
                var todayStats = await trackerDb.GetTodayStatsAsync();

                // Временные данные для демонстрации
                var todaySeconds = todayStats?.TotalSeconds ?? 7200; // 2 часа по умолчанию

                Device.BeginInvokeOnMainThread(() =>
                {
                    TodayTimeLabel.Text = FormatTime(TimeSpan.FromSeconds(todaySeconds));
                    ProductivityLabel.Text = $"{CalculateProductivity(todaySeconds)}%";
                    TopAppLabel.Text = todayStats?.TopApp ?? "Browser";
                    TopSiteLabel.Text = todayStats?.TopSite ?? "google.com";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tracker stats: {ex.Message}");
                // Устанавливаем значения по умолчанию
                Device.BeginInvokeOnMainThread(() =>
                {
                    TodayTimeLabel.Text = "0с";
                    ProductivityLabel.Text = "0%";
                    TopAppLabel.Text = "Нет данных";
                    TopSiteLabel.Text = "Нет данных";
                });
            }
        }

        // Остальные методы остаются без изменений...
        private int CalculateProductivity(long totalSeconds)
        {
            var maxProductiveSeconds = 8 * 3600;
            var productivity = (int)((totalSeconds / (double)maxProductiveSeconds) * 100);
            return Math.Min(productivity, 100);
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            if (Parent != null)
            {
                LoadProfileData();
                LoadFinancialStats();
                LoadTrackerStats();
            }
            else
            {
                _refreshTimer?.Stop();
            }
        }

        private void LoadProfileData()
        {
            try
            {
                _currentProfile = _profileService.GetCurrentProfile();
                if (_currentProfile == null) return;

                Device.BeginInvokeOnMainThread(() =>
                {
                    ProfileNameLabel.Text = _currentProfile.Name ?? "Без имени";
                    ProfileAvatarLabel.Text = _currentProfile.Avatar ?? "👤";
                    ProfileEmailLabel.Text = _currentProfile.Email ?? "Email не указан";
                    ProfileCreatedLabel.Text = $"Создан: {_currentProfile.CreatedAt:dd.MM.yyyy}";
                    UpdateAppTheme();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile data: {ex.Message}");
            }
        }

        private void UpdateAppTheme()
        {
            if (_currentProfile == null) return;
            Application.Current.UserAppTheme = _currentProfile.Theme;
        }

        private string FormatTime(TimeSpan time)
        {
            try
            {
                if (time.TotalHours >= 1)
                    return $"{(int)time.TotalHours}ч {time.Minutes}м";
                else if (time.TotalMinutes >= 1)
                    return $"{time.Minutes}м {time.Seconds}с";
                else
                    return $"{time.Seconds}с";
            }
            catch (Exception)
            {
                return "0с";
            }
        }

// РЕДАКТИРОВАНИЕ ПРОФИЛЯ
private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            if (_currentProfile == null) return;

            var action = await DisplayActionSheet("Редактировать профиль", "Отмена", null,
                "Изменить имя", "Сменить аватар", "Изменить email");

            switch (action)
            {
                case "Изменить имя":
                    await ChangeProfileName();
                    break;
                case "Сменить аватар":
                    await ChangeProfileAvatar();
                    break;
                case "Изменить email":
                    await ChangeProfileEmail();
                    break;
            }
        }

        private async Task ChangeProfileName()
        {
            var newName = await DisplayPromptAsync("Изменить имя",
                "Введите новое имя:", "Сохранить", "Отмена", _currentProfile.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != "Отмена")
            {
                _currentProfile.Name = newName.Trim();
                _profileService.UpdateProfile(_currentProfile);
                LoadProfileData();
                await DisplayAlert("Успех", "Имя успешно изменено", "OK");
            }
        }

        private async Task ChangeProfileAvatar()
        {
            var avatar = await DisplayActionSheet("Выберите аватар", "Отмена", null,
                "👤", "👨", "👩", "🧑", "👨‍💻", "👩‍💻", "🎮", "📚", "⚡", "🌟", "🎯", "💼", "🎨", "🚀");

            if (avatar != "Отмена")
            {
                _currentProfile.Avatar = avatar;
                _profileService.UpdateProfile(_currentProfile);
                LoadProfileData();
            }
        }

        private async Task ChangeProfileEmail()
        {
            var newEmail = await DisplayPromptAsync("Изменить email",
                "Введите новый email:", "Сохранить", "Отмена", _currentProfile.Email);

            if (!string.IsNullOrWhiteSpace(newEmail) && newEmail != "Отмена")
            {
                if (!newEmail.Contains("@"))
                {
                    await DisplayAlert("Ошибка", "Введите корректный email адрес", "OK");
                    return;
                }

                _currentProfile.Email = newEmail.Trim().ToLower();
                _profileService.UpdateProfile(_currentProfile);
                LoadProfileData();
                await DisplayAlert("Успех", "Email успешно изменен", "OK");
            }
        }

        // СМЕНА ПАРОЛЯ - ИСПРАВЛЕННАЯ ЛОГИКА
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            if (_currentProfile == null) return;

            // Проверяем, установлен ли пароль
            var hasPassword = !string.IsNullOrEmpty(_currentProfile.PasswordHash);

            if (hasPassword)
            {
                // Если пароль уже установлен - просим старый пароль
                var currentPassword = await DisplayPromptAsync("Смена пароля",
                    "Введите текущий пароль:", "Продолжить", "Отмена");

                if (string.IsNullOrWhiteSpace(currentPassword)) return;

                // Проверяем старый пароль
                var loginResult = _authService.Login(_currentProfile.Email, currentPassword);
                if (!loginResult.success)
                {
                    await DisplayAlert("Ошибка", "Неверный текущий пароль", "OK");
                    return;
                }
            }
            else
            {
                // Если пароля нет - просто переходим к установке нового
                await DisplayAlert("Установка пароля",
                    "Установите пароль для защиты вашего аккаунта", "OK");
            }

            // Запрос нового пароля
            var newPassword = await DisplayPromptAsync("Смена пароля",
                "Введите новый пароль:", "Продолжить", "Отмена");

            if (string.IsNullOrWhiteSpace(newPassword)) return;

            if (newPassword.Length < 6)
            {
                await DisplayAlert("Ошибка", "Пароль должен содержать минимум 6 символов", "OK");
                return;
            }

            var confirmPassword = await DisplayPromptAsync("Смена пароля",
                "Подтвердите новый пароль:", "Сменить", "Отмена");

            if (string.IsNullOrWhiteSpace(confirmPassword)) return;

            if (newPassword != confirmPassword)
            {
                await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                return;
            }

            // Устанавливаем новый пароль
            _currentProfile.PasswordHash = _authService.HashPassword(newPassword);
            _profileService.UpdateProfile(_currentProfile);

            await DisplayAlert("Успех", "Пароль успешно " + (hasPassword ? "изменен" : "установлен"), "OK");
        }

        // СМЕНА ЦВЕТА АКЦЕНТА
        private async void OnChangeAccentColorClicked(object sender, EventArgs e)
        {
            var colors = new[]
            {
                ("Синий", "#5865F2"),
                ("Зеленый", "#23A55A"),
                ("Желтый", "#F0B232"),
                ("Красный", "#F23F43"),
                ("Фиолетовый", "#9B59B6"),
                ("Розовый", "#E91E63"),
                ("Бирюзовый", "#1ABC9C"),
                ("Оранжевый", "#FF5722")
            };

            var colorNames = colors.Select(c => c.Item1).ToArray();
            var selected = await DisplayActionSheet("Выберите цвет акцента", "Отмена", null, colorNames);

            if (selected != "Отмена")
            {
                var selectedColor = colors.FirstOrDefault(c => c.Item1 == selected);
                if (selectedColor != default)
                {
                    _currentProfile.AccentColor = selectedColor.Item2;
                    _profileService.UpdateProfile(_currentProfile);
                    UpdateAppTheme();
                    await DisplayAlert("Успех", "Цвет акцента изменен", "OK");
                }
            }
        }

        // СМЕНА ТЕМЫ ПРИЛОЖЕНИЯ
        private async void OnChangeThemeClicked(object sender, EventArgs e)
        {
            var theme = await DisplayActionSheet("Выберите тему", "Отмена", null,
                "🌙 Тёмная", "☀️ Светлая", "⚙️ Системная");

            if (theme != "Отмена")
            {
                var newTheme = theme switch
                {
                    "🌙 Тёмная" => AppTheme.Dark,
                    "☀️ Светлая" => AppTheme.Light,
                    "⚙️ Системная" => AppTheme.Unspecified,
                    _ => AppTheme.Unspecified
                };

                _currentProfile.Theme = newTheme;
                _profileService.UpdateProfile(_currentProfile);
                UpdateAppTheme();
                await DisplayAlert("Успех", "Тема приложения изменена", "OK");
            }
        }

        // УВЕДОМЛЕНИЯ
        private async void OnNotificationsInfoClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Уведомления",
                "Уведомления будут приходить для:\n\n" +
                "• Ежедневных отчетов по финансам\n" +
                "• Напоминаний о крупных расходах\n" +
                "• Отчетов о продуктивности\n" +
                "• Напоминаний о целях\n\n" +
                "Функция будет доступна в следующем обновлении", "Понятно");
        }

        // ЭКСПОРТ ДАННЫХ
        private async void OnExportDataClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Экспорт данных",
                "Экспортировать все данные профиля?", "Экспортировать", "Отмена");

            if (confirm)
            {
                await DisplayAlert("Экспорт",
                    "Будут экспортированы:\n• Финансовые операции\n• Активы и долги\n• Статистика трекера\n\n" +
                    "Функция будет доступна в следующем обновлении", "OK");
            }
        }

        // ВЫХОД ИЗ АККАУНТА
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Выход",
                "Вы уверены, что хотите выйти из аккаунта?", "Выйти", "Отмена");

            if (confirm)
            {
                _authService.Logout();
                await DisplayAlert("Успех", "Вы вышли из аккаунта", "OK");
            }
        }

        // УДАЛЕНИЕ АККАУНТА
        private async void OnDeleteAccountClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Удаление аккаунта",
                "ВНИМАНИЕ: Это действие невозможно отменить. Все ваши данные будут безвозвратно удалены.",
                "Удалить", "Отмена");

            if (confirm)
            {
                var password = await DisplayPromptAsync("Подтверждение",
                    "Введите ваш пароль для подтверждения:", "Удалить", "Отмена");

                if (!string.IsNullOrWhiteSpace(password))
                {
                    var loginResult = _authService.Login(_currentProfile.Email, password);
                    if (loginResult.success)
                    {
                        _profileService.DeleteProfile(_currentProfile.Id);
                        _authService.Logout();
                        await DisplayAlert("Успех", "Аккаунт удален", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Неверный пароль", "OK");
                    }
                }
            }
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private async Task DisplayAlert(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        private async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);

            return false;
        }

        private async Task<string> DisplayPromptAsync(string title, string message, string accept, string cancel, string initialValue = "")
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, initialValue, -1, null);

            return null;
        }

        private async Task<string> DisplayActionSheet(string title, string cancel, string destruction, params string[] buttons)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons);

            return null;
        }
    }
}