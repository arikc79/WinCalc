using System;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;
using WinCalc.Security;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // 🔹 важливо — вимикаємо автозакриття програми після LoginWindow
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                // перевірка стану користувача
                if (result == true && AppSession.CurrentUser != null)
                {
                    // відкриваємо головне вікно
                    var main = new MainWindow();
                    main.Show();

                    // після відкриття — відновлюємо нормальний режим
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка запуску: {ex.Message}",
                                "Критична помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            string logPath = "app_log.txt";
            File.AppendAllText(logPath, $"OnStartup started at {DateTime.Now}{Environment.NewLine}");
            base.OnStartup(e);

            try
            {
                File.AppendAllText(logPath, "Initializing database..." + Environment.NewLine);
                InitializeDatabase();
                File.AppendAllText(logPath, "Database initialization completed." + Environment.NewLine);
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"OnStartup error: {ex.Message}{Environment.NewLine}");
                MessageBox.Show($"Error during startup: {ex.Message}");
            }
        }

        private void InitializeDatabase()
        {
            string dbPath = "window_calc.db";
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                new Obchyslennya().CreateTables();
                new DataInitializer().InsertInitialData(dbPath);
                connection.Close();
            }
        }
    }
}
