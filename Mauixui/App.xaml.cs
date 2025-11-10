using Microsoft.Maui.Controls;

namespace Mauixui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Гарантированно рабочая главная страница
            MainPage = new MainPage();
        }
    }
}