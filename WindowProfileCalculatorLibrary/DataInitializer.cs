using System;
using Microsoft.Data.Sqlite;

namespace WindowProfileCalculatorLibrary
{
    public static class DataInitializer
    {
        // ✅ ТЕПЕР ШЛЯХ БЕРЕТЬСЯ З КОНФІГУРАЦІЇ
        private static string ConnectionString => DbConfig.ConnectionString;

        public static void InsertInitialData()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // ====================== USERS ======================
            new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Role TEXT NOT NULL
                );", connection).ExecuteNonQuery();

            // ====================== MATERIALS ======================
            new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS Materials (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Category TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Color TEXT,
                    Price REAL NOT NULL,
                    Unit TEXT NOT NULL,
                    Description TEXT
                );", connection).ExecuteNonQuery();

            // --- Створюємо адміна, якщо користувачів нема ---
            var userCount = (long)new SqliteCommand("SELECT COUNT(*) FROM Users;", connection).ExecuteScalar();

            // Логіку створення адміна краще залишити в AuthService або тут, 
            // але в App.xaml.cs у вас вже є виклик EnsureAdminSeedAsync.
            // Тому тут просто переконуємося, що таблиці створені.

            // --- Заповнюємо матеріали, якщо пусто ---
            var matCount = (long)new SqliteCommand("SELECT COUNT(*) FROM Materials;", connection).ExecuteScalar();
            if (matCount == 0)
            {
                var materials = new (string Category, string Name, string? Color, double Price, string Unit, string? Desc)[]
                {
                    ("Профіль", "Rehau", "Білий", 150, "м.п.", "Euro-Design 70"),
                    ("Профіль", "Veka", "Білий", 160, "м.п.", "Softline 82"),
                    ("Профіль", "WDS", "Білий", 130, "м.п.", "WDS 5S"),
                    ("Профіль", "Steko", "Білий", 125, "м.п.", "S 500"),
                    ("Склопакет", "1-камерний", null, 400, "м²", "24 мм (4-16-4)"),
                    ("Склопакет", "2-камерний", null, 650, "м²", "32 мм (4-10-4-10-4)"),
                    ("Склопакет", "Енергозберігаючий", null, 750, "м²", "4i-14Ar-4-14Ar-4i"),
                    ("Фурнітура", "Maco", null, 800, "комплект", "Австрія, преміум"),
                    ("Фурнітура", "Vorne", null, 550, "комплект", "Туреччина, стандарт"),
                    ("Фурнітура", "Siegenia", null, 900, "комплект", "Німеччина, титан"),
                    ("Підвіконня", "Білий 200мм", "Білий", 200, "м.п.", "ПВХ матове"),
                    ("Підвіконня", "Білий 300мм", "Білий", 300, "м.п.", "ПВХ матове"),
                    ("Відлив", "Білий 150мм", "Білий", 100, "м.п.", "Металевий"),
                    ("Відлив", "Білий 200мм", "Білий", 140, "м.п.", "Металевий"),
                    ("Москітна сітка", "Стандарт", "Білий", 250, "м²", "Рамкова"),
                    ("Ручка", "Стандарт", "Білий", 50, "шт", "Металева"),
                    ("Ручка", "Преміум", "Білий", 150, "шт", "Hoppe Secustik"),
                    ("Ущільнювач", "Гумовий", "Чорний", 10, "м.п.", "EPDM"),
                    ("Армування", "П-подібне", null, 45, "м.п.", "Метал 1.5мм"),
                    ("Планка запорна", "Стандарт", null, 20, "шт", "Для фурнітури"),
                    ("Планка запорна нижня", "Нижня", null, 25, "шт", "Стандарт"),
                    ("Опора верхня", "Стандарт", null, 30, "шт", "Для фіксації стулки"),
                    ("Петлі", "Комплект", null, 100, "шт", "2 шт у комплекті"),
                    ("Шуруп", "Саморіз 4.2x16", null, 1.5, "шт", "Для фурнітури"),
                    ("Євро-штапик", "Стандарт", "Білий", 22, "м.п.", "ПВХ профіль")
                };

                foreach (var m in materials)
                {
                    var cmd = new SqliteCommand(
                        "INSERT INTO Materials (Category, Name, Color, Price, Unit, Description) VALUES (@c,@n,@col,@p,@u,@d);", connection);
                    cmd.Parameters.AddWithValue("@c", m.Category);
                    cmd.Parameters.AddWithValue("@n", m.Name);
                    cmd.Parameters.AddWithValue("@col", (object?)m.Color ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p", m.Price);
                    cmd.Parameters.AddWithValue("@u", m.Unit);
                    cmd.Parameters.AddWithValue("@d", (object?)m.Desc ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"✅ Inserted {materials.Length} default materials.");
            }
            else
            {
                Console.WriteLine("ℹ️ Materials table is not empty. Skipping seed.");
            }
        }
    }
}