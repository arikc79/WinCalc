using System;
using System.Windows;
using WinCalc.Common;
using WindowProfileCalculatorLibrary;
using WinCalc.Services;
using WinCalc.Security;

namespace WinCalc
{
    public partial class App : Application
    {
        private readonly AuthService _authService = new();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                DataInitializer.InsertInitialData();
                await _authService.EnsureAdminSeedAsync();

                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result == true && AppSession.CurrentUser != null)
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                else
                    Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка ініціалізації: {ex.Message}", "Критична помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
