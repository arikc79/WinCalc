
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using WinCalc.Common;

namespace WindowProfileCalculatorLibrary
{
    public static class DataInitializer
    {
        private static string ConnectionString => DbConfig.ConnectionString;

        // Инициализация базы данных и заполнение начальными данными 
        public static void InsertInitialData()
        {
            try
            {
                string dbPath = DbConfig.DbPath;
                string folder = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"),
                    $"InsertInitialData: DB path = {dbPath} at {DateTime.Now:O}\n");

                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                // Создаём таблицы
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

                // Заполнение справочников и данных
                FillCategories(connection);

                // Гарантируем подоконники и відливи
                EnsureSillAndDrain(connection);

                // Главное наполнение Materials если таблица пустая
                FillMaterials(connection);

                // Профили, стеклопакеты, фурнитура и прочее
                FillProfiles(connection);
                FillGlassPacks(connection);
                FillFittings(connection);
                FillReinforcements(connection);
                FillSeals(connection);
                FillAccessories(connection);

                EnsureSillAndDrain(connection);

                // Лог таблиц
                using var tblCmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;", connection);
                using var tblR = tblCmd.ExecuteReader();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Tables after init at {DateTime.Now:O}:");
                while (tblR.Read()) sb.AppendLine(tblR.GetString(0));
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"), sb.ToString());
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"),
                    $"DB Init Error: {ex.Message}\n{ex.StackTrace}\n");
                throw;
            }
        }

        // Утилита
        private static void ExecuteNonQuery(this SqliteConnection conn, string sql)
        {
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // Категории материалов
        private static void FillCategories(SqliteConnection connection)
        {
            var categories = new[]
            {
                    "Профіль", "Склопакет", "Фурнітура", "Ручка", "Петлі",
                    "Підвіконня", "Відлив", "Москітна сітка", "Армування", "Ущільнювач"
                };

            using var tx = connection.BeginTransaction();
            foreach (var cat in categories)
            {
                using var cmd = new SqliteCommand("INSERT OR IGNORE INTO Categories (Name) VALUES (@n);", connection, tx);
                cmd.Parameters.AddWithValue("@n", cat);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        // Матеріали

        private static void FillMaterials(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Materials;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            var catIds = new Dictionary<string, int>();
            using (var cmdCat = new SqliteCommand("SELECT Name, Id FROM Categories;", connection))
            using (var reader = cmdCat.ExecuteReader())
            {
                while (reader.Read()) catIds[reader.GetString(0)] = reader.GetInt32(1);
            }

            using var tx = connection.BeginTransaction();

            void Add(string catName, string name, double price, string unit, string color = null, string desc = null)
            {
                if (!catIds.ContainsKey(catName)) return;
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Materials (CategoryId, Name, Price, Unit, Color, Description) 
                                        VALUES (@cid, @n, @p, @u, @c, @d);";
                cmd.Parameters.AddWithValue("@cid", catIds[catName]);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // Заполняем основные позиции
            Add("Склопакет", "1-камерний (24мм)", 400, "м2", desc: "4-16-4");
            Add("Склопакет", "2-камерний (32мм)", 650, "м2", desc: "4-10-4-10-4");
            Add("Склопакет", "Енергозберігаючий", 800, "м2", desc: "4i-10-4-10-4i");

            Add("Ручка", "Стандартна", 50, "шт", "Біла");
            Add("Ручка", "Преміум (Hoppe)", 150, "шт", "Біла");

            Add("Петлі", "Комплект", 120, "шт", "Метал");
            Add("Фурнітура", "Vorne", 350, "комплект", "Метал");

            Add("Москітна сітка", "Стандарт", 200, "м2", "Сіра");
            Add("Армування", "П-подібне", 45, "м.п.", "Метал");
            Add("Ущільнювач", "Гумовий", 15, "м.п.", "Чорний");

            tx.Commit();
        }

        // Підвіконня і відливи
        private static void EnsureSillAndDrain(SqliteConnection connection)
        {
            int? sillCatId = null;
            int? drainCatId = null;

            using (var cmd = new SqliteCommand("SELECT Id FROM Categories WHERE Name=@n LIMIT 1;", connection))
            {
                cmd.Parameters.AddWithValue("@n", "Підвіконня");
                var r = cmd.ExecuteScalar();
                if (r != null && r != DBNull.Value) sillCatId = Convert.ToInt32(r);

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@n", "Відлив");
                r = cmd.ExecuteScalar();
                if (r != null && r != DBNull.Value) drainCatId = Convert.ToInt32(r);
            }

            if (!sillCatId.HasValue && !drainCatId.HasValue)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"),
                    $"EnsureSillAndDrain: categories not found (sillId={sillCatId}, drainId={drainCatId})\n");
                return;
            }

            using var tx = connection.BeginTransaction();

            void InsertOrIgnoreMaterial(int catId, string name, double price, string unit)
            {
                using var ins = new SqliteCommand(
                    "INSERT OR IGNORE INTO Materials (CategoryId, Name, Price, Unit) VALUES (@cid, @name, @price, @unit);",
                    connection, tx);
                ins.Parameters.AddWithValue("@cid", catId);
                ins.Parameters.AddWithValue("@name", name);
                ins.Parameters.AddWithValue("@price", price);
                ins.Parameters.AddWithValue("@unit", unit);
                int changed = ins.ExecuteNonQuery();
                if (changed > 0)
                {
                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "app_log.txt"),
                        $"Inserted material: categoryId={catId}, name={name}, price={price}, unit={unit}\n");
                }
            }

            if (sillCatId.HasValue)
            {
                InsertOrIgnoreMaterial(sillCatId.Value, "200 мм", 100, "м.п.");
                InsertOrIgnoreMaterial(sillCatId.Value, "300 мм", 140, "м.п.");
            }

            if (drainCatId.HasValue)
            {
                InsertOrIgnoreMaterial(drainCatId.Value, "200 мм", 60, "м.п.");
                InsertOrIgnoreMaterial(drainCatId.Value, "300 мм", 90, "м.п.");
            }

            tx.Commit();
        }

        // Профілі
        private static void FillProfiles(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Profiles;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var tx = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null, string desc = null)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR IGNORE INTO Profiles (Name, Color, Price, Unit, Description) 
                                        VALUES (@n, @c, @p, @u, @d);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            var brands = new Dictionary<string, double>
                {
                    { "WDS", 140 },
                    { "Rehau", 220 },
                    { "Steko", 180 },
                    { "Salamander", 200 },
                    { "Veka", 210 }
                };

            var variants = new (string suffix, double mul)[]
            {
                    ("4-камерний", 1.0),
                    ("5-камерний", 1.10),
                    ("6i", 1.30),
                    ("7i", 1.50)
            };

            foreach (var kv in brands)
            {
                foreach (var v in variants)
                {
                    string fullName = $"{kv.Key} {v.suffix}";
                    double price = Math.Round(kv.Value * v.mul, 2);
                    Add(fullName, price, "м.п.", "Білий", $"{v.suffix} профіль");
                }
            }

            tx.Commit();
        }
        // Склопакети
        private static void FillGlassPacks(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM GlassPacks;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var tx = connection.BeginTransaction();

            void Add(string name, double price, string unit, string desc = null)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR IGNORE INTO GlassPacks (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("1-камерний (24мм)", 400, "м2", "4-16-4");
            Add("2-камерний (32мм)", 650, "м2", "4-10-4-10-4");
            Add("Енергозберігаючий", 800, "м2", "4i-10-4-10-4i");

            tx.Commit();
        }

        // Фурнітура
        private static void FillFittings(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Fittings;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var tx = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR IGNORE INTO Fittings (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d);"; cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("Стандартна", 50, "шт", "Біла");
            Add("Преміум (Hoppe)", 150, "шт", "Білий");
            Add("Vorne", 350, "комплект", "Метал");

            tx.Commit();
        }

        // Армування
        private static void FillReinforcements(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Reinforcements;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var tx = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR IGNORE INTO Reinforcements (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("П-подібне", 45, "м.п.", "Метал");

            tx.Commit();
        }

        // Ущільнювачі
        private static void FillSeals(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Seals;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var tx = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR IGNORE INTO Seals (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("Гумовий", 15, "м.п.", "Чорний");

            tx.Commit();
        }

        // Аксесуари
        private static void FillAccessories(SqliteConnection connection)
        {
            using var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Accessories;", connection);
            if ((long)cmdCheck.ExecuteScalar() > 0) return;

            using var tx = connection.BeginTransaction();

            void Add(string name, double price, string unit, string color = null, string desc = null)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR IGNORE INTO Accessories (Name, Color, Price, Unit, Description) VALUES (@n, @c, @p, @u, @d);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@c", (object)color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@d", (object)desc ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            Add("Москітна сітка", 200, "м2", "Сіра", "Стандартна москітна сітка");

            tx.Commit();
        }

        // Атрибути матеріалів
        private static void FillMaterialAttributes(SqliteConnection connection)
        {
            using var check = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name='MaterialAttributes';", connection);
            var exists = (long)check.ExecuteScalar();
            if (exists == 0) return;

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
                long mid = Convert.ToInt64(res);

                using var cmdIns = new SqliteCommand("INSERT OR IGNORE INTO MaterialAttributes (MaterialId, [Key], Value) VALUES (@mid, @k, @v);", connection);
                cmdIns.Parameters.AddWithValue("@mid", mid);
                cmdIns.Parameters.AddWithValue("@k", "Thickness");
                cmdIns.Parameters.AddWithValue("@v", kv.Value);
                cmdIns.ExecuteNonQuery();
            }
        }
    }
}