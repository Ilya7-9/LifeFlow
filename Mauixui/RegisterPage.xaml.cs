using Microsoft.Maui.Controls;
using Mauixui.Services;
using Mauixui.Models;
using System;
using System.Threading.Tasks;

namespace Mauixui.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;
        private readonly CredentialsService _credentialsService;
        private string _selectedAvatar = "👤";

        public RegisterPage(AuthService authService, ProfileService profileService)
        {
            InitializeComponent();
            _authService = authService;
            _profileService = profileService;
        }

        private async void OnSelectAvatarClicked(object sender, EventArgs e)
        {
            var avatar = await DisplayActionSheet("Выберите аватар", "Отмена", null,
                "👤", "👨", "👩", "🧑", "👨‍💻", "👩‍💻", "🎮", "📚", "⚡", "🌟", "🎯", "💼", "🎨", "🚀");

            if (avatar != "Отмена")
            {
                _selectedAvatar = avatar;
                AvatarButton.Text = $"{avatar} Аватар выбран";
            }
        }

        private async void OnCreateProfileClicked(object sender, EventArgs e)
        {
            try
            {
                var name = NameEntry.Text?.Trim();
                var email = EmailEntry.Text?.Trim();
                var password = PasswordEntry.Text;
                var confirmPassword = ConfirmPasswordEntry.Text;

                // Валидация
                if (string.IsNullOrWhiteSpace(name))
                {
                    await DisplayAlert("Ошибка", "Введите имя профиля", "OK");
                    return;
                }

                if (!string.IsNullOrEmpty(password) && password.Length < 6)
                {
                    await DisplayAlert("Ошибка", "Пароль должен содержать минимум 6 символов", "OK");
                    return;
                }

                if (password != confirmPassword)
                {
                    await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                    return;
                }

                // Создаем профиль
                var profile = new UserProfile
                {
                    Name = name,
                    Email = email,
                    Password = password,
                    Avatar = _selectedAvatar,
                    CreatedAt = DateTime.Now,
                    LastLogin = DateTime.Now,
                    IsActive = true
                };

                _profileService.AddProfile(profile);
                _profileService.SetCurrentProfile(profile);

                await DisplayAlert("Успех", "Профиль создан!", "OK");

                // Возвращаемся к выбору профиля
                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
                else
                {
                    Application.Current.MainPage = new ProfileSelectionPage(_authService, _profileService);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}