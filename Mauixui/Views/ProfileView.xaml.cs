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
        private readonly CredentialsService _credentialsService;
        private MainDatabase _db;

        public ProfileView()
        {
            InitializeComponent();
            _profileService = new ProfileService();
            _authService = new AuthService();
            _credentialsService = new CredentialsService();

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

                _db = MainDatabase.Instance;

                // ИСПРАВЛЕНИЕ: Получаем все данные и фильтруем по profileId
                var allFinances = await _db.GetItemsAsync();
                var allAssets = await _db.GetItemsAsync();
                var allDebts = await _db.GetItemsAsync();

                // Фильтруем по текущему профилю
                var profileFinances = allFinances.Where(f => f.ProfileId == _currentProfile.Id).ToList();
                var profileAssets = allAssets.Where(a => a.ProfileId == _currentProfile.Id).ToList();
                var profileDebts = allDebts.Where(d => d.ProfileId == _currentProfile.Id).ToList();

                var totalIncome = profileFinances.Where(f => f.Type == "Доход").Sum(f => f.Amount);
                var totalExpenses = profileFinances.Where(f => f.Type == "Расход").Sum(f => f.Amount);
                var totalAssets = profileAssets.Sum(a => a.Amount);
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

                // ИСПРАВЛЕНИЕ: Используем статический метод TrackerDatabase
                var todayStats = await TrackerService.GetTodayStatsAsync();

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

        // Метод для расчета продуктивности
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

        private async void LoadProfileData()
        {
            try
            {
                _currentProfile = _profileService.GetCurrentProfile();
                if (_currentProfile == null) return;

                Device.BeginInvokeOnMainThread(() =>
                {
                    ProfileNameLabel.Text = _currentProfile.Name ?? "Без имени";
                    ProfileAvatarLabel.Text = _currentProfile.Avatar ?? "👤";

                    // Теперь email берется напрямую из профиля
                    ProfileEmailLabel.Text = !string.IsNullOrEmpty(_currentProfile.Email)
                        ? _currentProfile.Email
                        : "Email не указан";

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
            Application.Current.UserAppTheme = _currentProfile.AppTheme;
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
                "Изменить имя", "Сменить аватар");

            switch (action)
            {
                case "Изменить имя":
                    await ChangeProfileName();
                    break;
                case "Сменить аватар":
                    await ChangeProfileAvatar();
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
                await _profileService.UpdateProfileAsync(_currentProfile);
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
                await _profileService.UpdateProfileAsync(_currentProfile);
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

                var profiles = await _profileService.GetProfilesAsync();
                if (profiles.Any(p => p.Id != _currentProfile.Id &&
                    p.Email?.ToLower() == newEmail.ToLower()))
                {
                    await DisplayAlert("Ошибка", "Этот email уже используется другим профилем", "OK");
                    return;
                }

                _currentProfile.Email = newEmail.Trim().ToLower();
                await _profileService.UpdateProfileAsync(_currentProfile);
                LoadProfileData();
                await DisplayAlert("Успех", "Email успешно изменен", "OK");
            }
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            if (_currentProfile == null) return;

            try
            {
                var hasPassword = !string.IsNullOrEmpty(_currentProfile.Password);

                if (hasPassword)
                {
                    var currentPassword = await DisplayPromptAsync("Смена пароля",
                        "Введите текущий пароль:", "Продолжить", "Отмена");

                    if (string.IsNullOrWhiteSpace(currentPassword)) return;

                    if (_currentProfile.Password != currentPassword)
                    {
                        await DisplayAlert("Ошибка", "Неверный текущий пароль", "OK");
                        return;
                    }
                }

                var newPassword = await DisplayPromptAsync("Смена пароля",
                    "Введите новый пароль:", "Продолжить", "Отмена");

                if (string.IsNullOrWhiteSpace(newPassword)) return;

                var confirmPassword = await DisplayPromptAsync("Смена пароля",
                    "Подтвердите новый пароль:", "Сменить", "Отмена");

                if (string.IsNullOrWhiteSpace(confirmPassword)) return;

                if (newPassword != confirmPassword)
                {
                    await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                    return;
                }

                _currentProfile.Password = newPassword;
                await _profileService.UpdateProfileAsync(_currentProfile);

                await DisplayAlert("Успех", "Пароль успешно " + (hasPassword ? "изменен" : "установлен"), "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при смене пароля: {ex.Message}", "OK");
            }
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
                _currentProfile.AppTheme = theme switch
                {
                    "🌙 Тёмная" => AppTheme.Dark,
                    "☀️ Светлая" => AppTheme.Light,
                    "⚙️ Системная" => AppTheme.Unspecified,
                    _ => AppTheme.Unspecified
                };

                await _profileService.UpdateProfileAsync(_currentProfile);
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
        private async void OnExportClicked(object sender, EventArgs e)
        {
            try
            {
                var exportPath = await DisplayPromptAsync("Экспорт профилей",
                    "Введите полный путь для сохранения (например: C:/profiles.json):",
                    "Сохранить", "Отмена", _profileService.GetProfilesFilePath());

                if (!string.IsNullOrEmpty(exportPath) && exportPath != "Отмена")
                {
                    await _profileService.ExportToLocationAsync(exportPath);
                    await DisplayAlert("Успех", $"Профили экспортированы в:\n{exportPath}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка экспорта: {ex.Message}", "OK");
            }
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            try
            {
                var importPath = await DisplayPromptAsync("Импорт профилей",
                    "Введите полный путь к файлу для импорта:",
                    "Импортировать", "Отмена");

                if (!string.IsNullOrEmpty(importPath) && importPath != "Отмена")
                {
                    var confirm = await DisplayAlert("Подтверждение",
                        "Текущие профили будут заменены. Продолжить?",
                        "Да", "Нет");

                    if (confirm)
                    {
                        await _profileService.ImportFromFileAsync(importPath);
                        await DisplayAlert("Успех", "Профили успешно импортированы", "OK");
                        LoadProfileData();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка импорта: {ex.Message}", "OK");
            }
        }

        // ВЫХОД ИЗ АККАУНТА
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Выход",
                "Вы уверены, что хотите выйти из профиля?", "Выйти", "Отмена");

            if (confirm)
            {
                _authService.Logout();

                // Возвращаемся к выбору профиля
                Application.Current.MainPage = new NavigationPage(
                    new ProfileSelectionPage(_authService, _profileService));
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

        private async void OnBindEmailClicked(object sender, EventArgs e)
        {
            if (_currentProfile == null) return;

            try
            {
                var email = await DisplayPromptAsync("Привязка email",
                    "Введите email для привязки к профилю:", "Привязать", "Отмена");

                if (string.IsNullOrWhiteSpace(email)) return;

                if (!email.Contains("@"))
                {
                    await DisplayAlert("Ошибка", "Введите корректный email адрес", "OK");
                    return;
                }

                // Проверяем, не используется ли email другим профилем
                var profiles = _profileService.GetProfiles();
                if (profiles.Any(p => p.Id != _currentProfile.Id &&
                    p.Email?.ToLower() == email.ToLower()))
                {
                    await DisplayAlert("Ошибка", "Этот email уже используется другим профилем", "OK");
                    return;
                }

                // Устанавливаем email
                _currentProfile.Email = email.ToLower().Trim();
                _profileService.UpdateProfile(_currentProfile);

                await DisplayAlert("Успех", "Email успешно привязан к профилю", "OK");
                LoadProfileData(); // Обновляем отображение
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при привязке email: {ex.Message}", "OK");
            }
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private void OnOpenFileLocationClicked(object sender, EventArgs e)
        {
            try
            {
                _profileService.OpenFileLocation();
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось открыть файл: {ex.Message}", "OK");
            }
        }

        // Вспомогательные методы для диалогов
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