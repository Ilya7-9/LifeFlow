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
        private bool _isLoggingIn = false;

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
            _isLoggingIn = false;
        }

        private void LoadProfiles()
        {
            _profiles = _profileService.GetProfiles();
            ProfilesCollectionView.ItemsSource = _profiles;

            if (_profiles.Any() && _selectedProfile == null)
            {
                ProfilesCollectionView.SelectedItem = _profiles[0];
            }
        }

        private void OnProfileSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is UserProfile selectedProfile)
            {
                _selectedProfile = selectedProfile;
                PasswordEntry.Text = "";
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
                    try
                    {
                        await CheckIfPasswordRequired();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при проверке пароля: {ex.Message}");
                    }
                }
                else
                {
                    PasswordEntry.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке пароля: {ex.Message}");
            }
        }

        private void OnShowPasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            ShowPasswordButton.Text = PasswordEntry.IsPassword ? "👁️" : "🔒";
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await TryLogin();
        }

        private async Task TryLogin()
        {
            if (_isLoggingIn) return;

            if (_selectedProfile == null)
            {
                await DisplayAlert("Ошибка", "Выберите профиль", "OK");
                return;
            }

            _isLoggingIn = true;
            LoginButton.IsEnabled = false;
            LoginButton.Text = "Вход...";

            try
            {
                var password = PasswordEntry.Text;
                var credentials = await _credentialsService.GetCredentialsAsync(_selectedProfile.Id);

                if (credentials == null)
                {
                    await LoginWithoutPassword(_selectedProfile);
                }
                else if (string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Ошибка", "Введите пароль", "OK");
                }
                else if (credentials.PasswordHash == password)
                {
                    await LoginSuccess(_selectedProfile);
                    return;
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
            finally
            {
                _isLoggingIn = false;
                LoginButton.IsEnabled = true;
                LoginButton.Text = "🔑 Войти";
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
                profile.LastLogin = DateTime.Now;
                _profileService.UpdateProfile(profile);
                _profileService.SetCurrentProfile(profile);

                // Сразу переходим на MainPage без задержек
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

        private async void OnPasswordEntryCompleted(object sender, EventArgs e)
        {
            if (_selectedProfile != null && !string.IsNullOrEmpty(PasswordEntry.Text))
            {
                await TryLogin();
            }
        }
    }
}