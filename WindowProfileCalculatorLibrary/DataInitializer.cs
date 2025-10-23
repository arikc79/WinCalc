using System;
using Microsoft.Data.Sqlite;
using WinCalc.Security;

namespace WindowProfileCalculatorLibrary
{
    public static class DataInitializer
    {
        private const string DbPath = "window_calc.db";

        public static void InsertInitialData()
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
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
            if (userCount == 0)
            {
                string hash = PasswordHasher.Hash("admin");
                var cmd = new SqliteCommand(
                    "INSERT INTO Users (Login, Password, Role) VALUES (@l, @p, @r);", connection);
                cmd.Parameters.AddWithValue("@l", "admin");
                cmd.Parameters.AddWithValue("@p", hash);
                cmd.Parameters.AddWithValue("@r", Roles.Admin);
                cmd.ExecuteNonQuery();
                Console.WriteLine("✅ Admin user created (admin/admin)");
            }

            // --- Створюємо матеріали, якщо таблиця пуста ---
            var matsCount = (long)new SqliteCommand("SELECT COUNT(*) FROM Materials;", connection).ExecuteScalar();
            if (matsCount == 0)
            {
                Console.WriteLine("📦 Seeding default materials...");

                var materials = new (string Category, string Name, string Color, double Price, string Unit, string Desc)[]
                {
                    // ==================== ПРОФІЛІ ====================
                    ("Профіль", "Rehau Euro 60", "Білий", 260, "м.п.", "4-камерний профіль Rehau"),
                    ("Профіль", "Rehau Euro 70", "Білий", 300, "м.п.", "5-камерний профіль Rehau"),
                    ("Профіль", "Rehau Euro 80", "Антрацит", 340, "м.п.", "6-камерний профіль Rehau"),
                    ("Профіль", "Rehau Euro 90", "Антрацит", 380, "м.п.", "7-камерний профіль Rehau"),

                    ("Профіль", "Veka Softline 60", "Білий", 260, "м.п.", "4-камерний профіль Veka"),
                    ("Профіль", "Veka Softline 70", "Білий", 300, "м.п.", "5-камерний профіль Veka"),
                    ("Профіль", "Veka Softline 76", "Антрацит", 340, "м.п.", "6-камерний профіль Veka"),
                    ("Профіль", "Veka Softline 82", "Антрацит", 380, "м.п.", "7-камерний профіль Veka"),

                    ("Профіль", "Salamander Streamline 60", "Білий", 260, "м.п.", "4-камерний профіль Salamander"),
                    ("Профіль", "Salamander Streamline 70", "Білий", 300, "м.п.", "5-камерний профіль Salamander"),
                    ("Профіль", "Salamander Streamline 76", "Антрацит", 340, "м.п.", "6-камерний профіль Salamander"),
                    ("Профіль", "Salamander bluEvolution 82", "Антрацит", 380, "м.п.", "7-камерний профіль Salamander"),

                    ("Профіль", "WDS 400", "Білий", 260, "м.п.", "4-камерний профіль WDS"),
                    ("Профіль", "WDS 500", "Білий", 300, "м.п.", "5-камерний профіль WDS"),
                    ("Профіль", "WDS 6S", "Антрацит", 340, "м.п.", "6-камерний профіль WDS"),
                    ("Профіль", "WDS 8S", "Антрацит", 380, "м.п.", "7-камерний профіль WDS"),

                    ("Профіль", "OpenTeck 60", "Білий", 260, "м.п.", "4-камерний профіль OpenTeck"),
                    ("Профіль", "OpenTeck 70", "Білий", 300, "м.п.", "5-камерний профіль OpenTeck"),
                    ("Профіль", "OpenTeck 76", "Антрацит", 340, "м.п.", "6-камерний профіль OpenTeck"),
                    ("Профіль", "OpenTeck Elite 82", "Антрацит", 380, "м.п.", "7-камерний профіль OpenTeck"),

                    // ==================== РУЧКИ ====================
                    ("Ручка", "Стандарт", "Білий", 90, "шт", "Базова фурнітура"),
                    ("Ручка", "Преміум", "Срібло", 150, "шт", "Преміум клас"),

                    // ==================== СКЛОПАКЕТИ ====================
                    ("Склопакет", "1-камерний", null, 520, "м²", "4-16-4"),
                    ("Склопакет", "2-камерний", null, 740, "м²", "4-12-4-12-4"),
                    ("Склопакет", "Енергозберігаючий", null, 880, "м²", "LowE + Ar"),

                    // ==================== ПІДВІКОННЯ ====================
                    ("Підвіконня", "Білий 200мм", "Білий", 160, "м.п.", "200 мм"),
                    ("Підвіконня", "Білий 300мм", "Білий", 200, "м.п.", "300 мм"),
                    ("Підвіконня", "Дуб 200мм", "Дуб", 190, "м.п.", "200 мм"),
                    ("Підвіконня", "Дуб 300мм", "Дуб", 240, "м.п.", "300 мм"),

                    // ==================== ВІДЛИВИ ====================
                    ("Відлив", "Білий 150мм", "Білий", 180, "м.п.", "150 мм"),
                    ("Відлив", "Білий 200мм", "Білий", 200, "м.п.", "200 мм"),
                    ("Відлив", "Коричневий 150мм", "Коричневий", 200, "м.п.", "150 мм"),
                    ("Відлив", "Коричневий 200мм", "Коричневий", 220, "м.п.", "200 мм"),

                    // ==================== УЩІЛЬНЮВАЧІ, КОМПЛЕКТИ ====================
                    ("Ущільнювач скла", "Стандарт", "Чорний", 15, "м.п.", "EPDM"),
                    ("Армування", "1.2 мм", "Сталь", 45, "м.п.", "Сталь оцинкована"),
                    ("Заглушка відливна", "Стандарт", "Білий", 10, "шт", "Пластикова пара"),
                    ("Ущільнювач створки", "Стандарт", "Чорний", 18, "м.п.", "EPDM"),
                    ("Кутик ПВХ", "Стандарт", "Білий", 20, "шт", "З'єднувальний елемент"),
                    ("Планка запорна", "Верхня", null, 25, "шт", "Стандарт"),
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
                Console.WriteLine("ℹ️ Materials already exist. Skipping seeding.");
            }

            connection.Close();
        }
    }
}
