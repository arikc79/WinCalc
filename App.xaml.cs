using Microsoft.Data.Sqlite;
using System;
using System.Windows;
using WindowProfileCalculatorLibrary;

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
            try
            {
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnStartup error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void InitializeDatabase()
        {
            string dbPath = "window_calc.db";
            Console.WriteLine($"Starting database initialization for {dbPath}");
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("Database connection opened successfully in App.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to open database connection in App.");
                        return;
                    }
                    // Виклик методу для створення таблиць
                    Console.WriteLine("Attempting to create tables...");
                    new Obchyslennya().CreateTables();
                    Console.WriteLine("Tables creation attempted from App.");
                    connection.Close();
                }
                Console.WriteLine("Database initialization completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database initialization: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}