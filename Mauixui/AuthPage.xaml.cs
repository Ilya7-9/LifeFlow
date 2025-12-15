using Microsoft.Maui.Controls;
using Mauixui.Services;
using System;
using System.Threading.Tasks;

namespace Mauixui.Views
{
    public partial class AuthPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;

        public AuthPage(AuthService authService, ProfileService profileService)
        {
            InitializeComponent();
            _authService = authService;
            _profileService = profileService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Проверяем, может пользователь уже авторизован
            if (_authService.IsLoggedIn())
            {
                // Если авторизован, переходим сразу в приложение
                NavigateToMainApp();
            }
        }

        private void OnFieldsChanged(object sender, EventArgs e)
        {
            // Активируем кнопку только когда оба поля заполнены
            LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(EmailEntry.Text) &&
                                   !string.IsNullOrWhiteSpace(PasswordEntry.Text);
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                var email = EmailEntry.Text?.Trim();
                var password = PasswordEntry.Text;

                // Базовая валидация
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    await DisplayAlert("Ошибка", "Введите корректный email", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    await DisplayAlert("Ошибка", "Пароль должен содержать минимум 6 символов", "OK");
                    return;
                }

                // Показываем индикатор загрузки
                LoginButton.IsEnabled = false;
                LoginButton.Text = "Вход...";

                var result = _authService.Login(email, password);

                if (result.success)
                {
                    await DisplayAlert("Успех", result.message, "OK");
                    NavigateToMainApp();
                }
                else
                {
                    await DisplayAlert("Ошибка", result.message, "OK");
                    // Возвращаем кнопку в исходное состояние
                    LoginButton.IsEnabled = true;
                    LoginButton.Text = "Войти";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
                LoginButton.IsEnabled = true;
                LoginButton.Text = "Войти";
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            // Переходим на страницу регистрации
            await Navigation.PushAsync(new RegisterPage(_authService, _profileService));
        }

        private async void NavigateToMainApp()
        {
            // Переходим на главную страницу приложения
            Application.Current.MainPage = new MainPage();

            // Удаляем страницу авторизации из стека навигации
            if (Navigation.NavigationStack.Count > 1)
            {
                Navigation.RemovePage(Navigation.NavigationStack[0]);
            }
        }
    }
}