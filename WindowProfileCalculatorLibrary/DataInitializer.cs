using Microsoft.Data.Sqlite;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.Data.Sqlite;
    using System.Collections.Generic;
    using Microsoft.Data.Sqlite;
using WinCalc.Common;



namespace WindowProfileCalculatorLibrary
{
    public static class DataInitializer
    {
        private static string ConnectionString => DbConfig.ConnectionString;

        public static void InsertInitialData()
        {
            try
            {
                string dbPath = DbConfig.DbPath;
                string folder = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                // Логируем путь к БД
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), $"InsertInitialData: DB path = {dbPath} at {DateTime.Now}\n");

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    // Создаём таблицы пользователей и справочников
                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Login TEXT NOT NULL UNIQUE,
                            Password TEXT NOT NULL,
                            Role TEXT NOT NULL
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Categories (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE
                        );");

                    // Общая совместимая таблица Materials (резерв)
                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Materials (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CategoryId INTEGER,
                            Name TEXT NOT NULL,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT,
                            FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
                        );");

                    // Специализированные таблицы (Profiles, GlassPacks, Fittings, ...)
                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Profiles (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS GlassPacks (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Fittings (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Reinforcements (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Seals (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Accessories (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            Description TEXT
                        );");

                    connection.ExecuteNonQuery(@"
                        CREATE TABLE IF NOT EXISTS Calculations (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserId INTEGER NOT NULL,
                            Date DATETIME DEFAULT CURRENT_TIMESTAMP,
                            TotalPrice REAL NOT NULL,
                            Width REAL,
                            Height REAL,
                            WindowType TEXT,
                            FOREIGN KEY(UserId) REFERENCES Users(Id)
                        );");

                    // Заполняем базовые справочники/данные
                    FillCategories(connection);
                    FillMaterials(connection);
                    FillProfiles(connection);
                    FillGlassPacks(connection);
                    FillFittings(connection);
                    FillReinforcements(connection);
                    FillSeals(connection);
                    FillAccessories(connection);

                    // Лог — какие таблицы теперь есть в базе
                    using var tblCmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table';", connection);
                    using var tblR = tblCmd.ExecuteReader();
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Tables after init at {DateTime.Now}:");
                    while (tblR.Read()) sb.AppendLine(tblR.GetString(0));
                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), sb.ToString());
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), $"DB Init Error: {ex.Message}\n{ex.StackTrace}\n");
                throw; // пробрасываем — чтобы в UI вы видели ошибку и могли её диагностировать
            }
        }

        // Допоміжний метод розширення для скорочення коду
        private static void ExecuteNonQuery(this SqliteConnection conn, string sql)
        {
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static void FillCategories(SqliteConnection connection)
        {
            var categories = new[] { "Профіль", "Склопакет", "Фурнітура", "Ручка", "Петлі", "Підвіконня", "Відлив", "Москітна сітка", "Армування", "Ущільнювач" };

            foreach (var cat in categories)
            {
                using var cmd = new SqliteCommand("INSERT OR IGNORE INTO Categories (Name) VALUES (@n)", connection);
                cmd.Parameters.AddWithValue("@n", cat);
                cmd.ExecuteNonQuery();
            }
        }

        private static void FillMaterials(SqliteConnection connection)
        {
            // Перевіряємо, чи є матеріали
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Materials", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            // Словник для швидкого пошуку ID категорії за назвою
            var catIds = new Dictionary<string, int>();
            using (var cmdCat = new SqliteCommand("SELECT Name, Id FROM Categories", connection))
            using (var reader = cmdCat.ExecuteReader())
            {
                while (reader.Read()) catIds[reader.GetString(0)] = reader.GetInt32(1);
            }

            using var transaction = connection.BeginTransaction();

            void Add(string catName, string name, double price, string unit, string color = null, string desc = null)
            {
                if (!catIds.ContainsKey(catName)) return;

                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO Materials (CategoryId, Name, Price, Unit, Color, Description) 
                                    VALUES (@cid, @n, @p, @u, @c, @d)";
                cmd.Parameters.AddWithValue("@cid", catIds[catName]);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // --- ЗАПОВНЕННЯ ---
            Add("Профіль", "WDS 500", 150, "м.п.", "Білий");
            Add("Профіль", "Rehau Euro 70", 220, "м.п.", "Білий");
            Add("Профіль", "Steko S500", 180, "м.п.", "Білий");

            Add("Склопакет", "1-камерний (24мм)", 400, "м2", desc: "4-16-4");
            Add("Склопакет", "2-камерний (32мм)", 650, "м2", desc: "4-10-4-10-4");
            Add("Склопакет", "Енергозберігаючий", 800, "м2", desc: "4i-10-4-10-4i");

            Add("Ручка", "Стандартна", 50, "шт", "Біла");
            Add("Ручка", "Преміум (Hoppe)", 150, "шт", "Біла");

            Add("Петлі", "Комплект", 120, "шт", "Метал");
            Add("Фурнітура", "Vorne", 350, "комплект", "Метал");

            // Підвіконня — два типи: 200 і 300 мм (ціни в грн/м.п.)
            Add("Підвіконня", "200 мм", 100, "м.п.", "Білий");
            Add("Підвіконня", "300 мм", 140, "м.п.", "Білий");

            // Відливи — два типи: 200 і 300 мм (ціни в грн/м.п.)
            Add("Відлив", "200 мм", 60, "м.п.", "Білий");
            Add("Відлив", "300 мм", 90, "м.п.", "Білий");

            Add("Москітна сітка", "Стандарт", 200, "м2", "Сіра");
            Add("Армування", "П-подібне", 45, "м.п.", "Метал");
            Add("Ущільнювач", "Гумовий", 15, "м.п.", "Чорний");

            transaction.Commit();
        }

        private static void FillProfiles(SqliteConnection connection)
        {
            // Проверяем, есть ли уже профили
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Profiles", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var transaction = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null, string desc = null)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO Profiles (Name, Price, Unit, Color, Description) 
                                    VALUES (@n, @p, @u, @c, @d)";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // --- ЗАПОВНЕННЯ ---
            Add("WDS 500", 150, "м.п.", "Білий");
            Add("Rehau Euro 70", 220, "м.п.", "Білий");
            Add("Steko S500", 180, "м.п.", "Білий");

            transaction.Commit();
        }

        private static void FillGlassPacks(SqliteConnection connection)
        {
            // Проверяем, есть ли уже стеклопакеты
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM GlassPacks", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var transaction = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null, string desc = null)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO GlassPacks (Name, Price, Unit, Color, Description) 
                                    VALUES (@n, @p, @u, @c, @d)";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // --- ЗАПОВНЕННЯ ---
            Add("1-камерний (24мм)", 400, "м2", desc: "4-16-4");
            Add("2-камерний (32мм)", 650, "м2", desc: "4-10-4-10-4");
            Add("Енергозберігаючий", 800, "м2", desc: "4i-10-4-10-4i");

            transaction.Commit();
        }

        private static void FillFittings(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Fittings", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var transaction = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null, string desc = null)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT OR IGNORE INTO Fittings (Name, Color, Price, Unit, Description) 
                                    VALUES (@n, @c, @p, @u, @d)";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // Ручки та комплекти фурнітури
            Add("Стандартна", 50, "шт", "Біла");
            Add("Преміум (Hoppe)", 150, "шт", "Біла");
            Add("Vorne", 350, "комплект", "Метал");

            transaction.Commit();
        }

        private static void FillReinforcements(SqliteConnection connection)
        {
            using var check = new SqliteCommand("SELECT COUNT(*) FROM Reinforcements", connection);
            if ((long)check.ExecuteScalar() > 0) return;

            using var transaction = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT OR IGNORE INTO Reinforcements (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d)";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("П-подібне", 45, "м.п.", "Метал");

            transaction.Commit();
        }

        private static void FillSeals(SqliteConnection connection)
        {
            using var check = new SqliteCommand("SELECT COUNT(*) FROM Seals", connection);
            if ((long)check.ExecuteScalar() > 0) return;

            using var transaction = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT OR IGNORE INTO Seals (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d)";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("Гумовий", 15, "м.п.", "Чорний");

            transaction.Commit();
        }

        private static void FillAccessories(SqliteConnection connection)
        {
            using var check = new SqliteCommand("SELECT COUNT(*) FROM Accessories", connection);
            if ((long)check.ExecuteScalar() > 0) return;

            using var transaction = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null, string desc = null)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO Accessories (Name, Color, Price, Unit, Description) 
                                    VALUES (@n, @c, @p, @u, @d)";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // Москітна сітка та інші аксесуари
            Add("Москітна сітка", 200, "м2", "Сіра", "Стандартна москітна сітка");

            transaction.Commit();
        }

        private static void FillMaterialAttributes(SqliteConnection connection)
        {
            using var check = new SqliteCommand("SELECT COUNT(*) FROM MaterialAttributes", connection);
            if ((long)check.ExecuteScalar() > 0) return;

            var profileNames = new Dictionary<string, string>
            {
                { "WDS 500", "60 мм" },
                { "Rehau Euro 70", "70 мм" },
                { "Steko S500", "60 мм" }
            };

            foreach (var kv in profileNames)
            {
                using var cmdFind = new SqliteCommand("SELECT Id FROM Materials WHERE Name = @n LIMIT 1", connection);
                cmdFind.Parameters.AddWithValue("@n", kv.Key);
                var res = cmdFind.ExecuteScalar();
                if (res == null) continue;
                long mid = (long)res;

                using var cmdIns = new SqliteCommand("INSERT INTO MaterialAttributes (MaterialId, [Key], Value) VALUES (@mid, @k, @v)", connection);
                cmdIns.Parameters.AddWithValue("@mid", mid);
                cmdIns.Parameters.AddWithValue("@k", "Thickness");
                cmdIns.Parameters.AddWithValue("@v", kv.Value);
                cmdIns.ExecuteNonQuery();
            }
        }
    }
}