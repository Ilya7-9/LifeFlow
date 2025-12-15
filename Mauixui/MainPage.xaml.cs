using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Mauixui.Views;
using Mauixui.Models;
using Mauixui.Services;
using System.Diagnostics;

namespace Mauixui
{
    public partial class MainPage : ContentPage
    {
        private Button _currentActiveButton;
        private ProfileService _profileService;
        private UserProfile _currentProfile;
        private bool _isInitialized = false;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isInitialized)
                return;

            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("🚀 Инициализация MainPage");

                ShowLoading(true);

                // 1. Инициализация сервисов
                Debug.WriteLine("▶️ ДО создания ProfileService");
                _profileService = new ProfileService();
                await _profileService.InitializeAsync();
                Debug.WriteLine("▶️ ПОСЛЕ создания ProfileService");

                Debug.WriteLine("▶️ ДО GetCurrentProfile");
                _currentProfile = _profileService.GetCurrentProfile();
                Debug.WriteLine("▶️ ПОСЛЕ GetCurrentProfile");


                if (_currentProfile != null &&
                    _currentProfile.AppTheme != AppTheme.Unspecified &&
                    Application.Current != null)
                {
                    Application.Current.UserAppTheme = _currentProfile.AppTheme;
                }

                // 2. Инициализация БД и трекера
                await InitializeHeavyComponentsAsync();

                // 3. Инициализация UI
                Device.BeginInvokeOnMainThread(() =>
                {
                    _currentActiveButton = ProfileButton;
                    SetActiveButton(ProfileButton);

                    LoadProfileView();

                    ProfileButton.Clicked += OnProfileClicked;
                    FinanceButton.Clicked += OnFinanceClicked;
                    TrackButton.Clicked += OnTrackClicked;
                    ThemeSwitch.Toggled += OnThemeSwitchToggled;

                    LoadProfileSettings();
                    RefreshProfileInfo();

                    ShowLoading(false);
                    _isInitialized = true;

                    Debug.WriteLine("✅ MainPage полностью загружен");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка инициализации MainPage: {ex}");

                ShowLoading(false);

                await DisplayAlert(
                    "Ошибка",
                    "Не удалось загрузить приложение. Перезапустите его.",
                    "OK");
            }
        }

        private void ShowLoading(bool show)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (LoadingGrid != null)
                    LoadingGrid.IsVisible = show;

                if (MainGrid != null)
                    MainGrid.IsVisible = !show;
            });
        }

        private async Task InitializeHeavyComponentsAsync()
        {
            if (_currentProfile == null)
                throw new Exception("_currentProfile == null");

            TrackerService.Initialize(_currentProfile.Id);   // 1️⃣ инициализация базы и ID профиля
            TrackerService.EnsureStarted();                 // 2️⃣ запуск трекера

            var todayStats = await TrackerService.GetTodayStatsAsync(); // 3️⃣ теперь безопасно
            Console.WriteLine($"Сегодняшнее время: {todayStats.TotalSeconds} секунд");
        }



        private void LoadProfileView()
        {
            if (MainContent != null)
            {
                MainContent.Children.Clear();

                try
                {
                    var profileView = new ProfileView();
                    MainContent.Children.Add(profileView);
                    Console.WriteLine("✅ ProfileView загружен");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка загрузки ProfileView: {ex.Message}");

                    // Запасной вариант
                    var errorView = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 20,
                        Children =
                        {
                            new Label
                            {
                                Text = "👤 Профиль",
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Application.Current.UserAppTheme == AppTheme.Dark ?
                                           Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000")
                            },
                            new Label
                            {
                                Text = "Ошибка загрузки",
                                TextColor = Color.FromArgb("#FF4B4B")
                            },
                            new Button
                            {
                                Text = "Попробовать снова",
                                BackgroundColor = Color.FromArgb("#5865F2"),
                                TextColor = Color.FromArgb("#fff"),
                                Command = new Command(() => LoadProfileView())
                            }
                        }
                    };
                    MainContent.Children.Add(errorView);
                }
            }
        }

        private void LoadView(View view)
        {
            if (MainContent == null) return;

            MainContent.Children.Clear();

            // Для TrackerView загружаем с задержкой
            if (view is TrackerView)
            {
                // Показываем индикатор загрузки
                var loadingView = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            Color = Color.FromArgb("#5865F2"),
                            WidthRequest = 30,
                            HeightRequest = 30
                        },
                        new Label
                        {
                            Text = "Загрузка трекера...",
                            TextColor = Application.Current.UserAppTheme == AppTheme.Dark ?
                                       Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000"),
                            FontSize = 12
                        }
                    }
                };

                MainContent.Children.Add(loadingView);

                // Отложенная загрузка
                Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        MainContent.Children.Clear();
                        MainContent.Children.Add(view);
                    });
                    return false; // Останавливаем таймер
                });
            }
            else
            {
                MainContent.Children.Add(view);
            }
        }

        private void LoadProfileSettings()
        {
            if (ThemeSwitch != null && _currentProfile != null)
            {
                ThemeSwitch.IsToggled = _currentProfile.AppTheme == AppTheme.Dark ||
                                       (Application.Current.UserAppTheme == AppTheme.Dark);
            }
            RefreshProfileInfo();
        }

        private void RefreshProfileInfo()
        {
            _currentProfile = _profileService.GetCurrentProfile();
            UpdateButtonColors();
        }

        private void SetActiveButton(Button activeButton)
        {
            if (_currentActiveButton != null)
            {
                _currentActiveButton.BackgroundColor = Color.FromArgb("#00FFFFFF");
                _currentActiveButton.TextColor = Application.Current.UserAppTheme == AppTheme.Dark ?
                    Color.FromArgb("#B9BBBE") : Color.FromArgb("#747F8D");
            }

            var accentColor = _currentProfile?.AccentColor ?? "#5865F2";
            activeButton.BackgroundColor = Color.FromArgb(accentColor);
            activeButton.TextColor = Color.FromArgb("#FFFFFF");

            _currentActiveButton = activeButton;
        }

        private void OnProfileClicked(object sender, EventArgs e)
        {
            if (_currentActiveButton != ProfileButton)
            {
                SetActiveButton(ProfileButton);
                LoadView(new ProfileView());
            }
        }

        private void OnFinanceClicked(object sender, EventArgs e)
        {
            if (_currentActiveButton != FinanceButton)
            {
                SetActiveButton(FinanceButton);
                LoadView(new FinanceView());
            }
        }

        private void OnTrackClicked(object sender, EventArgs e)
        {
            if (_currentActiveButton != TrackButton)
            {
                SetActiveButton(TrackButton);

                // Показываем индикатор перед загрузкой тяжелого TrackerView
                var trackerView = new TrackerView();
                LoadView(trackerView);
            }
        }

        private async void OnProfileMenuClicked(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Управление профилями", "Отмена", null,
                "Сменить профиль", "Создать профиль", "Редактировать профиль", "Статистика");

            switch (action)
            {
                case "Сменить профиль":
                    await ShowProfileSelection();
                    break;
                case "Создать профиль":
                    await CreateNewProfile();
                    break;
                case "Редактировать профиль":
                    await EditCurrentProfile();
                    break;
                case "Статистика":
                    await ShowProfileStatistics();
                    break;
            }
        }

        private async Task ShowProfileSelection()
        {
            var profiles = _profileService.GetProfiles();
            var profileNames = profiles.Select(p => $"{p.Avatar} {p.Name}").ToArray();

            var selected = await DisplayActionSheet("Выберите профиль", "Отмена", null, profileNames);

            if (selected != "Отмена" && !string.IsNullOrEmpty(selected))
            {
                var selectedProfile = profiles.FirstOrDefault(p => $"{p.Avatar} {p.Name}" == selected);
                if (selectedProfile != null)
                {
                    _profileService.SetCurrentProfile(selectedProfile);
                    await _profileService.UpdateAllProfilesStatsAsync();
                    RefreshProfileInfo();

                    if (_currentActiveButton == ProfileButton)
                        LoadView(new ProfileView());
                    else if (_currentActiveButton == FinanceButton)
                        LoadView(new FinanceView());
                    else if (_currentActiveButton == TrackButton)
                        LoadView(new TrackerView());

                    await DisplayAlert("Успех", $"Профиль {selectedProfile.Name} активирован", "OK");
                }
            }
        }

        public async void RefreshGlobalStatistics()
        {
            await _profileService.UpdateAllProfilesStatsAsync();
            RefreshProfileInfo();
        }

        private async Task CreateNewProfile()
        {
            var name = await DisplayPromptAsync("Создать профиль", "Введите имя профиля:", "Создать", "Отмена", "Новый профиль");

            if (!string.IsNullOrWhiteSpace(name) && name != "Отмена")
            {
                var avatar = await DisplayActionSheet("Выберите аватар", "Отмена", null,
                    "👤", "👨", "👩", "🧑", "👨‍💻", "👩‍💻", "🎮", "📚", "⚡");

                if (avatar != "Отмена")
                {
                    var newProfile = _profileService.CreateProfile(name, avatar);
                    RefreshProfileInfo();
                    await DisplayAlert("Успех", $"Профиль {name} создан", "OK");
                }
            }
        }

        private async Task EditCurrentProfile()
        {
            var action = await DisplayActionSheet("Редактировать профиль", "Отмена", "Удалить",
                "Изменить имя", "Сменить аватар", "Цвет темы");

            switch (action)
            {
                case "Изменить имя":
                    await ChangeProfileName();
                    break;
                case "Сменить аватар":
                    await ChangeProfileAvatar();
                    break;
                case "Цвет темы":
                    await ChangeAccentColor();
                    break;
                case "Удалить":
                    await DeleteCurrentProfile();
                    break;
            }
        }

        private async Task ChangeProfileName()
        {
            var newName = await DisplayPromptAsync("Изменить имя", "Введите новое имя:", "Сохранить", "Отмена", _currentProfile.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != "Отмена")
            {
                _currentProfile.Name = newName;
                _profileService.UpdateProfile(_currentProfile);
                RefreshProfileInfo();
            }
        }

        private async Task ChangeProfileAvatar()
        {
            var avatar = await DisplayActionSheet("Выберите аватар", "Отмена", null,
                "👤", "👨", "👩", "🧑", "👨‍💻", "👩‍💻", "🎮", "📚", "⚡", "🌟", "🎯", "💼");

            if (avatar != "Отмена")
            {
                _currentProfile.Avatar = avatar;
                _profileService.UpdateProfile(_currentProfile);
                RefreshProfileInfo();
            }
        }

        private async Task ChangeAccentColor()
        {
            var colors = new[]
            {
                ("Синий", "#5865F2"),
                ("Зеленый", "#23A55A"),
                ("Желтый", "#F0B232"),
                ("Красный", "#F23F43"),
                ("Фиолетовый", "#9B59B6"),
                ("Розовый", "#E91E63"),
                ("Бирюзовый", "#1ABC9C")
            };

            var colorNames = colors.Select(c => c.Item1).ToArray();
            var selected = await DisplayActionSheet("Цвет акцента", "Отмена", null, colorNames);

            if (selected != "Отмена")
            {
                var selectedColor = colors.FirstOrDefault(c => c.Item1 == selected);
                if (selectedColor != default)
                {
                    _currentProfile.AccentColor = selectedColor.Item2;
                    _profileService.UpdateProfile(_currentProfile);
                    RefreshProfileInfo();
                }
            }
        }

        private async Task DeleteCurrentProfile()
        {
            var confirm = await DisplayAlert("Удалить профиль",
                $"Вы уверены, что хотите удалить профиль {_currentProfile.Name}? Это действие нельзя отменить.",
                "Удалить", "Отмена");

            if (confirm)
            {
                _profileService.DeleteProfile(_currentProfile.Id);
                RefreshProfileInfo();
                await DisplayAlert("Успех", "Профиль удален", "OK");
            }
        }

        private async Task ShowProfileStatistics()
        {
            var currentProfile = _profileService.GetCurrentProfile();

            var stats = "📊 Статистика профиля: " + currentProfile.Name + "\n\n" +
                       "📅 Создан: " + currentProfile.CreatedAt.ToString("dd.MM.yyyy") + "\n" +
                       "⏱️ Отслежено времени: " + currentProfile.TotalTrackedTime.ToString(@"hh\:mm\:ss") + "\n\n" +
                       "🎨 Настройки:\n" +
                       "• Тема: " + GetThemeName(currentProfile.AppTheme) + "\n" +
                       "• Цвет акцента: " + currentProfile.AccentColor;

            await DisplayAlert("Статистика профиля", stats, "OK");
        }

        private string GetThemeName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Dark => "Тёмная",
                AppTheme.Light => "Светлая",
                AppTheme.Unspecified => "Системная",
                _ => "Неизвестно"
            };
        }

        public void UpdateProfileStatistics(int tasksCount, int notesCount, TimeSpan trackedTime)
        {
            try
            {
                if (_profileService != null)
                {
                    //_profileService.UpdateProfileStats(tasksCount, notesCount, trackedTime);
                    RefreshProfileInfo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статистики профиля: {ex.Message}");
            }
        }

        private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            var newTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
            Application.Current.UserAppTheme = newTheme;

            _currentProfile.AppTheme = newTheme;
            _profileService.UpdateProfile(_currentProfile);

            UpdateButtonColors();
        }

        private void UpdateButtonColors()
        {
            var currentActive = _currentActiveButton;
            if (currentActive != null)
            {
                SetActiveButton(currentActive);
            }
        }
    }
}