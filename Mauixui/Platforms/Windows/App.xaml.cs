using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Mauixui.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            this.UnhandledException += (s, e) =>
            {
                try
                {
                    var logDir = @"D:\Logs\MyApp";
                    Directory.CreateDirectory(logDir); // гарантируем, что папка есть

                    var logPath = Path.Combine(logDir, "error.txt");

                    File.WriteAllText(logPath, e.Exception.ToString());
                }
                catch
                {
                    // Ничего не делаем, чтобы не упасть повторно
                }
            };
        }


        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
