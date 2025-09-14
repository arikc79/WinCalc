using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Windows;

using WindowProfileCalculatorLibrary; // Для MessageBox

namespace WinCalc
{
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            string logPath = "app_log.txt";
            Console.WriteLine("OnStartup started at " + DateTime.Now);
            File.AppendAllText(logPath, "OnStartup started at " + DateTime.Now + Environment.NewLine);
            base.OnStartup(e);
            try
            {
                Console.WriteLine("Calling InitializeDatabase...");
                File.AppendAllText(logPath, "Calling InitializeDatabase..." + Environment.NewLine);
                InitializeDatabase();
                Console.WriteLine("InitializeDatabase completed at " + DateTime.Now);
                File.AppendAllText(logPath, "InitializeDatabase completed at " + DateTime.Now + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnStartup error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                File.AppendAllText(logPath, $"OnStartup error: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}{Environment.NewLine}");
                MessageBox.Show($"Error during startup: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        private void InitializeDatabase()
        {
            string dbPath = "window_calc.db";
            Console.WriteLine($"Starting database initialization for {dbPath} at " + DateTime.Now);
            File.AppendAllText("app_log.txt", $"Starting database initialization for {dbPath} at " + DateTime.Now + Environment.NewLine);
            MessageBox.Show("Starting database initialization");
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("Database connection opened successfully in App at " + DateTime.Now);
                        File.AppendAllText("app_log.txt", "Database connection opened successfully in App at " + DateTime.Now + Environment.NewLine);
                    }
                    else
                    {
                        Console.WriteLine("Failed to open database connection in App at " + DateTime.Now);
                        File.AppendAllText("app_log.txt", "Failed to open database connection in App at " + DateTime.Now + Environment.NewLine);
                        return;
                    }
                    Console.WriteLine("Attempting to create tables...");
                    File.AppendAllText("app_log.txt", "Attempting to create tables..." + Environment.NewLine);
                    new Obchyslennya().CreateTables();
                    Console.WriteLine("Tables creation attempted from App at " + DateTime.Now);
                    File.AppendAllText("app_log.txt", "Tables creation attempted from App at " + DateTime.Now + Environment.NewLine);
                    Console.WriteLine("Attempting to insert initial data...");
                    File.AppendAllText("app_log.txt", "Attempting to insert initial data..." + Environment.NewLine);
                    new DataInitializer().InsertInitialData(dbPath);
                    Console.WriteLine("Initial data insertion attempted from App at " + DateTime.Now);
                    File.AppendAllText("app_log.txt", "Initial data insertion attempted from App at " + DateTime.Now + Environment.NewLine);
                    connection.Close();
                }
                Console.WriteLine("Database initialization completed at " + DateTime.Now);
                File.AppendAllText("app_log.txt", "Database initialization completed at " + DateTime.Now + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database initialization: {ex.Message} at " + DateTime.Now);
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                File.AppendAllText("app_log.txt", $"Error during database initialization: {ex.Message} at " + DateTime.Now + Environment.NewLine);
                File.AppendAllText("app_log.txt", $"Stack Trace: {ex.StackTrace}" + Environment.NewLine);
                MessageBox.Show($"Error during database init: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}