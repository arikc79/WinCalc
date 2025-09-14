using System;
using System.Data.SQLite;
using System.Windows;

namespace WinCalc
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string dbPath = "window_calc.db";
            if (!System.IO.File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    // Перевірка: з'єднання відкрито
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("Database file created and connection opened.");
                    }
                    connection.Close();
                }
            }
        }
    }
}