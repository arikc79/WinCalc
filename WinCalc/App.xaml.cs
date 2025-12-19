using System;
using System.IO;
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

            string dbPath = DbConfig.DbPath;
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), $"OnStartup: DbPath = {dbPath} at {DateTime.Now}\n");

            try
            {
                // Инициализация БД (таблицы + справочники)
                DataInitializer.InsertInitialData();
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), $"InsertInitialData completed at {DateTime.Now}\n");

                // Гарантируем наличие админа с правильным хешем
                await _authService.EnsureAdminSeedAsync();

                // Показываем окно логина
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result == true && AppSession.CurrentUser != null)
                {
                    // Успешный логин — запускаем основное приложение
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                else
                {
                    // Пользователь не залогинен — завершаем
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), $"InsertInitialData threw: {ex.Message}\n{ex.StackTrace}\n");
                MessageBox.Show($"Ошибка инициализации: {ex.Message}\nСм. app_log.txt", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
