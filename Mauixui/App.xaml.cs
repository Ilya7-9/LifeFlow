using Mauixui.Services;
using Mauixui.Views;
using Mauixui.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace Mauixui
{
    public partial class App : Application
    {
        private MainDatabase _database;
        private AuthService _authService;
        private ProfileService _profileService;
        private UserProfile _currentProfile;

        public App()
        {
            try
            {
                InitializeComponent();

                // СОЗДАЕМ СЕРВИСЫ
                Debug.WriteLine("🔄 Создание сервисов...");
                _authService = new AuthService();
                _profileService = new ProfileService();
                Debug.WriteLine("✅ Сервисы созданы");

                // ❗ ОБЯЗАТЕЛЬНО: временная страница
                MainPage = new ContentPage
                {
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                {
                    new ActivityIndicator { IsRunning = true },
                    new Label { Text = "Загрузка..." }
                }
                    }
                };

                // Логирование — можно
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                        Debug.WriteLine(ex);
                };

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    Debug.WriteLine(e.Exception);
                    e.SetObserved();
                };

                // Асинхронная инициализация
                InitializeApplication();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }


        private async void InitializeApplication()
        {
            try
            {
                Debug.WriteLine("🔄 Инициализация приложения...");

                // 1. ИНИЦИАЛИЗИРУЕМ ЕДИНУЮ БАЗУ ДАННЫХ (с таймаутом)
                Debug.WriteLine("🔄 Инициализация базы данных...");
                _database ??= MainDatabase.Instance;

                if (_database == null)
                    throw new Exception("MainDatabase.Instance вернул null");

                await _database.InitializeAsync();

                Debug.WriteLine("✅ Единая база данных инициализирована");

                // 3. ПРОВЕРЯЕМ ПРОФИЛИ
                Debug.WriteLine("🔄 Проверка профилей...");
                var profiles = await _database.GetProfilesAsync();
                Debug.WriteLine($"📁 Найдено профилей в БД: {profiles.Count}");

                if (profiles.Any())
                {
                    // Есть профили - продолжаем нормально
                    await HandleExistingProfiles(profiles);
                }
                else
                {
                    // Нет профилей - создаем первый
                    await HandleNoProfiles();
                }

                Debug.WriteLine("✅ Приложение успешно запущено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА инициализации приложения: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                ShowErrorPage(ex);
            }
        }

        private async Task HandleExistingProfiles(List<UserProfile> profiles)
        {
            var savedProfileId = Preferences.Get("current_profile_id", "");
            _currentProfile = profiles.FirstOrDefault(p => p.Id == savedProfileId) ?? profiles.First();

            if (_profileService == null || _authService == null)
                throw new Exception("Сервисы не инициализированы");


            if (_currentProfile != null)
            {
                Debug.WriteLine($"👤 Текущий профиль: {_currentProfile.Name}");

                // Обновляем время входа
                _currentProfile.LastLogin = DateTime.Now;
                await _database.UpdateProfileAsync(_currentProfile);

                // Устанавливаем как текущий
                _profileService.SetCurrentProfile(_currentProfile);

                // Переходим на главную страницу
                Debug.WriteLine("🔄 Переход на главную страницу...");
                MainPage = new MainPage();
            }
            else
            {
                Debug.WriteLine("🔄 Переход к выбору профиля...");
                MainPage = new ProfileSelectionPage(_authService, _profileService);
            }
        }

        private async Task HandleNoProfiles()
        {
            Debug.WriteLine("📝 Нет профилей, переход к регистрации...");
            MainPage = new RegisterPage(_authService, _profileService);
        }

        // Инициализация трекера для профиля
        private async Task InitializeTrackerForProfileAsync(string profileId)
        {
            try
            {
                Debug.WriteLine($"🔄 Инициализация трекера для профиля ID: {profileId}");

                // 1. Инициализируем TrackerService с текущим профилем
                TrackerService.Initialize(profileId);

                // 2. Запускаем трекинг
                if (TrackerService._isInitialized)
                    TrackerService.StartTracking();

                Debug.WriteLine($"✅ Трекер инициализирован для профиля");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка инициализации трекера: {ex.Message}");
                // Не прерываем запуск приложения из-за ошибки трекера
            }
        }

        private void ShowErrorPage(Exception ex)
        {
            // Создаем кнопку с обработчиком сразу
            var retryButton = new Button
            {
                Text = "Повторить",
                BackgroundColor = Color.FromArgb("#5865F2"),
                TextColor = Color.FromArgb("#FFFFFF"),
                Padding = new Thickness(20, 10),
                Margin = new Thickness(0, 20, 0, 0)
            };

            // Добавляем обработчик
            retryButton.Clicked += async (s, e) =>
            {
                // Показываем индикатор загрузки
                var loadingPage = new ContentPage
                {
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children = { new ActivityIndicator { IsRunning = true } }
                    }
                };

                MainPage = loadingPage;

                // Ждем немного и перезапускаем
                await Task.Delay(1000);
                InitializeApplication();
            };

            // Создаем страницу с кнопкой
            if (MainPage == null)
            {
                MainPage = new ContentPage()
                {
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 20,
                        Padding = 30,
                        Children =
                        {
                            new Label
                            {
                                Text = "❌ Ошибка запуска",
                                FontSize = 20,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#FF0000")
                            },
                            new Label
                            {
                                Text = ex.Message,
                                FontSize = 14,
                                TextColor = Color.FromArgb("#808080")
                            },
                            new Label
                            {
                                Text = "Попробуйте перезапустить приложение",
                                FontSize = 12,
                                TextColor = Color.FromArgb("#888888")
                            },
                            retryButton // Используем созданную кнопку
                        }
                    }
                };
            };
        }

        // Метод для смены профиля
        public async Task SwitchProfileAsync(string profileId)
        {
            try
            {
                Debug.WriteLine($"🔄 Смена профиля на ID: {profileId}");

                // Останавливаем текущий трекер
                if (TrackerService._isInitialized)
                    TrackerService.StopTracking();

                // Получаем новый профиль
                _currentProfile = await _database.GetProfileAsync(profileId);
                if (_currentProfile != null)
                {
                    // Сохраняем в настройках
                    Preferences.Set("current_profile_id", profileId);

                    // Обновляем в ProfileService
                    _profileService.SetCurrentProfile(_currentProfile);

                    // Обновляем время входа
                    _currentProfile.LastLogin = DateTime.Now;
                    await _database.UpdateProfileAsync(_currentProfile);

                    // Инициализируем трекер для нового профиля
                    await InitializeTrackerForProfileAsync(profileId);

                    Debug.WriteLine($"✅ Переключен на профиль: {_currentProfile.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка смены профиля: {ex.Message}");
            }
        }

        // Метод для выхода из профиля
        public async Task LogoutAsync()
        {
            try
            {
                Debug.WriteLine("🚪 Выход из профиля...");

                // Останавливаем трекинг
                if (TrackerService._isInitialized)
                    TrackerService.StopTracking();

                // Сбрасываем текущий профиль
                Preferences.Remove("current_profile_id");
                _currentProfile = null;

                // Переходим на страницу выбора профиля
                MainPage = new ProfileSelectionPage(_authService, _profileService);

                Debug.WriteLine("✅ Выход выполнен");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка выхода: {ex.Message}");
            }
        }

        // Метод для получения текущего профиля
        public async Task<UserProfile> GetCurrentProfileAsync()
        {
            if (_currentProfile != null)
                return _currentProfile;

            if (_database == null)
                return null;

            var profileId = Preferences.Get("current_profile_id", "");
            if (string.IsNullOrEmpty(profileId))
                return null;

            _currentProfile = await _database.GetProfileAsync(profileId);
            return _currentProfile;
        }

        // Метод для получения базы данных
        public MainDatabase GetDatabase()
        {
            return _database;
        }

        // Метод для закрытия приложения
        protected override void OnSleep()
        {
            base.OnSleep();
            Debug.WriteLine("💤 Приложение переходит в сон...");
            if (TrackerService._isInitialized)
                TrackerService.StopTracking();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            Debug.WriteLine("🌅 Приложение возобновляет работу...");

            // Если есть текущий профиль - перезапускаем трекинг
            var profile = await GetCurrentProfileAsync();
            if (profile != null)
            {
                await InitializeTrackerForProfileAsync(profile.Id);
            }
        }

        // Метод для обновления профиля
        public async Task UpdateProfileAsync(UserProfile profile)
        {
            try
            {
                await _database.UpdateProfileAsync(profile);
                if (profile.Id == _currentProfile?.Id)
                {
                    _currentProfile = profile;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка обновления профиля: {ex.Message}");
            }
        }

        // Метод для создания нового профиля
        public async Task<UserProfile> CreateProfileAsync(string name, string email, string password, string avatar = "👤")
        {
            try
            {
                var profile = await _database.CreateProfileAsync(name, email, password, avatar);
                Debug.WriteLine($"✅ Создан новый профиль: {name}");
                return profile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка создания профиля: {ex.Message}");
                return null;
            }
        }
    }
}