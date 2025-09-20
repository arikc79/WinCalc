using Microsoft.Data.Sqlite;
using System;

namespace WindowProfileCalculatorLibrary
{
    public class DataInitializer
    {
        /// <summary>
        /// Вставляє початкові дані в таблицю Materials.
        /// Користувачі створюються тільки через AuthService.EnsureAdminSeedAsync().
        /// </summary>
        /// <param name="dbPath">Шлях до файлу бази даних</param>
        public void InsertInitialData(string dbPath)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    Console.WriteLine("Connection opened in InsertInitialData.");

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("Failed to open connection in InsertInitialData.");
                        return;
                    }

                    // Вставка тестових матеріалів
                    string insertMaterials = @"
                        INSERT OR IGNORE INTO Materials 
                        (Category, Name, Color, Price, Unit, QuantityType, Description) VALUES
                        ('профіль', 'Basic-Design (4)', 'білий', 425.00, 'м.пог.', 'довжина', '4-камерний профіль'),
                        ('скло', 'Однокамерний', NULL, 1500.00, 'м²', 'площа', 'Стандартне скло'),
                        ('фурнітура', 'Ручка стандартна', 'срібляста', 50.00, 'шт', 'шт', 'Базова ручка')";
                    try
                    {
                        using (var command = new SqliteCommand(insertMaterials, connection))
                        {
                            int rowsAffected = command.ExecuteNonQuery();
                            Console.WriteLine($"Materials inserted/checked. Rows affected: {rowsAffected}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting Materials: {ex.Message}");
                    }

                    connection.Close();
                    Console.WriteLine("InsertInitialData completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error in InsertInitialData: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
