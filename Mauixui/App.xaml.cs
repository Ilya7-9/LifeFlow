using Mauixui.Services;
using Mauixui.Views;
using Microsoft.Maui.Controls;

namespace Mauixui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var profileService = new ProfileService();
            var authService = new AuthService(profileService);
            var credentialsService = new CredentialsService();

            // Всегда показываем выбор профиля при запуске
            MainPage = new NavigationPage(new ProfileSelectionPage(authService, profileService));
        }
    }
}