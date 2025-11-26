using System;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;
using WinCalc.Security;
using WinCalc.Services;
using WindowProfileCalculatorLibrary;

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
                // 1️⃣ Ініціалізація бази
                InitializeDatabase();

                // 2️⃣ Гарантовано створюємо адміна (з хешем)
                await _authService.EnsureAdminSeedAsync();


                File.AppendAllText("app_log.txt", $"Init DB at {DateTime.Now}\n");
                InitializeDatabase();
                File.AppendAllText("app_log.txt", $"DB initialized\n");

                // 3️⃣ Запускаємо LoginWindow
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result == true && AppSession.CurrentUser != null)
                {
                   
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Помилка запуску: {ex.Message}",
                    "Критична помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitializeDatabase()
        {
            string dbPath = WindowProfileCalculatorLibrary.DbConfig.DbPath;

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            try
            {
              //  Obchyslennya.CreateTables();
                DataInitializer.InsertInitialData(); // створює таблиці + базові дані
            }
            catch (Exception dbEx)
            {
                MessageBox.Show($"⚠️ DB init error: {dbEx.Message}");
            }
        }
    }
}
