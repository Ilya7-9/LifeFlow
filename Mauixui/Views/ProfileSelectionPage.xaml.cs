using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mauixui.Views
{
    public partial class ProfileSelectionPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;
        private readonly CredentialsService _credentialsService;
        private List<UserProfile> _profiles;
        private UserProfile _selectedProfile;

        public ProfileSelectionPage(AuthService authService, ProfileService profileService)
        {
            InitializeComponent();
            _authService = authService;
            _profileService = profileService;
            _credentialsService = new CredentialsService();

            LoadProfiles();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            _profiles = _profileService.GetProfiles();
            ProfilesCollectionView.ItemsSource = _profiles;
        }

        private void OnProfileSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is UserProfile selectedProfile)
            {
                _selectedProfile = selectedProfile;
                Console.WriteLine($"✅ Выбран профиль: {selectedProfile.Name} ({selectedProfile.Id})");

                // Очищаем поле пароля при выборе нового профиля
                PasswordEntry.Text = "";

                // Проверяем есть ли у профиля пароль
                _ = CheckIfPasswordRequired();
            }
        }

        private async Task CheckIfPasswordRequired()
        {
            if (_selectedProfile == null) return;

            try
            {
                var credentials = await _credentialsService.GetCredentialsAsync(_selectedProfile.Id);

                if (credentials == null || string.IsNullOrEmpty(credentials.PasswordHash))
                {
                    // Если пароля нет, показываем сообщение
                    await DisplayAlert("Информация",
                        $"У профиля '{_selectedProfile.Name}' не установлен пароль. " +
                        $"Вы можете войти без пароля или установить его в настройках профиля.", "OK");
                }
                else
                {
                    // Если пароль есть, фокусируемся на поле ввода
                    PasswordEntry.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке пароля: {ex.Message}");
            }
        }

        // УБИРАЕМ автоматическую проверку при изменении текста
        private void OnPasswordChanged(object sender, EventArgs e)
        {
            // Теперь ничего не делаем при изменении текста
            // Проверка будет только при нажатии Enter или кнопки
        }

        private async void OnShowPasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            ShowPasswordButton.Text = PasswordEntry.IsPassword ? "👁️" : "🔒";
        }

        // Вход по нажатию кнопки "Войти" (добавим кнопку)
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await TryLogin();
        }

        private async Task TryLogin()
        {
            if (_selectedProfile == null)
            {
                await DisplayAlert("Ошибка", "Выберите профиль", "OK");
                return;
            }

            var password = PasswordEntry.Text;

            try
            {
                // Получаем учетные данные профиля
                var credentials = await _credentialsService.GetCredentialsAsync(_selectedProfile.Id);

                if (credentials == null)
                {
                    // Если учетных данных нет, входим без пароля
                    await LoginWithoutPassword(_selectedProfile);
                    return;
                }

                // Проверяем пароль (только если введен пароль)
                if (string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Ошибка", "Введите пароль", "OK");
                    return;
                }

                if (credentials.PasswordHash == password)
                {
                    await LoginSuccess(_selectedProfile);
                }
                else
                {
                    await DisplayAlert("Ошибка", "Неверный пароль", "OK");
                    PasswordEntry.Text = "";
                    PasswordEntry.Focus();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка входа: {ex.Message}", "OK");
            }
        }

        private async Task LoginWithoutPassword(UserProfile profile)
        {
            var result = await DisplayAlert("Вход без пароля",
                $"Войти в профиль '{profile.Name}' без пароля?", "Войти", "Отмена");

            if (result)
            {
                await LoginSuccess(profile);
            }
        }

        private async Task LoginSuccess(UserProfile profile)
        {
            try
            {
                // Обновляем время последнего входа
                profile.LastLogin = DateTime.Now;
                _profileService.UpdateProfile(profile);
                _profileService.SetCurrentProfile(profile);

                // Переходим в приложение
                Application.Current.MainPage = new MainPage();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось войти: {ex.Message}", "OK");
            }
        }

        private async void OnCreateNewProfileClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage(_authService, _profileService));
        }

        // Вход при нажатии Enter в поле пароля
        private async void OnPasswordEntryCompleted(object sender, EventArgs e)
        {
            if (_selectedProfile != null && !string.IsNullOrEmpty(PasswordEntry.Text))
            {
                await TryLogin();
            }
        }

        // Добавляем метод для отладки БД
        //private async void OnDebugCredentialsClicked(object sender, EventArgs e)
        //{
        //    await _credentialsService.DebugShowAllCredentials();
        //    await DisplayAlert("Отладка", "Информация о credentials выведена в консоль", "OK");
        //}
    }
}