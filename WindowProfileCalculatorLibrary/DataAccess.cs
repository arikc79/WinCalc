using System;
using System.Collections.Generic;
using System.Globalization; // ADD: інваріантна культура для CSV
using System.IO;           // ADD: запис CSV
using System.Text;         // ADD: кодування CSV
using Microsoft.Data.Sqlite;

namespace WindowProfileCalculatorLibrary
{
    public class DataAccess
    {
        private readonly string _dbPath = "window_calc.db";

        private SqliteConnection CreateConnection()
            => new SqliteConnection($"Data Source={_dbPath}");

        // ======================== USERS (без змін) ========================

        public void CreateUser(string login, string password, string role)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = @"INSERT INTO Users (Login, Password, Role)
                                       VALUES (@login, @password, @role)";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", password);
                command.Parameters.AddWithValue("@role", role);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
            }
        }

        public List<(string Login, string Password, string Role)> ReadUsers()
        {
            var users = new List<(string Login, string Password, string Role)>();
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = "SELECT Login, Password, Role FROM Users";
                using var command = new SqliteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    users.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading users: {ex.Message}");
            }
            return users;
        }

        public void UpdateUser(string login, string newPassword, string newRole)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = @"UPDATE Users
                                       SET Password = @password, Role = @role
                                       WHERE Login = @login";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", newPassword);
                command.Parameters.AddWithValue("@role", newRole);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
            }
        }

        public void DeleteUser(string login)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = "DELETE FROM Users WHERE Login = @login";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@login", login);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
            }
        }

        // ======================== MATERIALS ========================

        /// <summary>
        /// Переконуємось, що в таблиці Materials є потрібні колонки.
        /// Додаємо, якщо немає: Quantity (REAL, def=0), Currency (TEXT, def='грн'),
        /// Article (TEXT), QuantityType (TEXT).
        /// </summary>
        private void EnsureMaterialColumns()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();

                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SqliteCommand("PRAGMA table_info(Materials);", connection))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        // column name is at index 1
                        existing.Add(rd.GetString(1));
                    }
                }

                void AddColumnIfMissing(string name, string ddl)
                {
                    if (!existing.Contains(name))
                    {
                        using var alter = new SqliteCommand($"ALTER TABLE Materials ADD COLUMN {ddl};", connection);
                        alter.ExecuteNonQuery();
                        existing.Add(name);
                    }
                }

                // ADD: нові колонки
                AddColumnIfMissing("Quantity", "REAL DEFAULT 0");
                AddColumnIfMissing("Currency", "TEXT DEFAULT 'грн'");
                AddColumnIfMissing("Article", "TEXT");
                AddColumnIfMissing("QuantityType", "TEXT");
            }
            catch
            {
                // тихо — не валимо запуск на старих/частково-ініціалізованих БД
            }
        }

        /// <summary>
        /// Зчитує всі матеріали в повний список моделей.
        /// </summary>
        public List<Material> ReadMaterials()
        {
            EnsureMaterialColumns();

            var list = new List<Material>();
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = @"
                    SELECT Id, Category, Name, Color, Price, Unit, Quantity, QuantityType, Article, Currency, Description
                    FROM Materials
                    ORDER BY Id;";
                using var command = new SqliteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var m = new Material
                    {
                        Id = reader.GetInt32(0),
                        Category = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Color = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Price = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                        Unit = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Quantity = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                        QuantityType = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        Article = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Currency = reader.IsDBNull(9) ? "грн" : reader.GetString(9),
                        Description = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    };
                    list.Add(m);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading materials: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Створює матеріал і повертає його з присвоєним Id.
        /// </summary>
        public Material CreateMaterial(Material m)
        {
            EnsureMaterialColumns();

            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = @"
                    INSERT INTO Materials (Category, Name, Color, Price, Unit, Quantity, QuantityType, Article, Currency, Description)
                    VALUES (@category, @name, @color, @price, @unit, @quantity, @quantityType, @article, @currency, @description);
                    SELECT last_insert_rowid();";
                using var command = new SqliteCommand(query, connection);

                command.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
                command.Parameters.AddWithValue("@name", (object?)m.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@color", (object?)m.Color ?? DBNull.Value);
                command.Parameters.AddWithValue("@price", m.Price);
                command.Parameters.AddWithValue("@unit", (object?)m.Unit ?? DBNull.Value);
                command.Parameters.AddWithValue("@quantity", m.Quantity);
                command.Parameters.AddWithValue("@quantityType", (object?)m.QuantityType ?? DBNull.Value);
                command.Parameters.AddWithValue("@article", (object?)m.Article ?? DBNull.Value);
                command.Parameters.AddWithValue("@currency", (object?)m.Currency ?? "грн");
                command.Parameters.AddWithValue("@description", (object?)m.Description ?? DBNull.Value);

                var id = (long)command.ExecuteScalar();
                m.Id = (int)id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating material: {ex.Message}");
            }
            return m;
        }

        /// <summary>
        /// Сумісний зі старим інтерфейсом метод (залишено для існуючих викликів).
        /// </summary>
        public int CreateMaterial(string category, string name, string color, double price, string unit, string quantityType, string description)
        {
            var m = new Material
            {
                Category = category,
                Name = name,
                Color = color,
                Price = price,
                Unit = unit,
                Quantity = 0,            // дефолт
                QuantityType = quantityType, // тепер зберігаємо
                Article = "",           // дефолт
                Currency = "грн",        // дефолт
                Description = description
            };
            return CreateMaterial(m).Id;
        }

        /// <summary>
        /// Оновлює матеріал (повертає true, якщо змінено >= 1 рядок).
        /// </summary>
        public bool UpdateMaterial(Material m)
        {
            EnsureMaterialColumns();

            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = @"
                    UPDATE Materials
                    SET Category=@category, Name=@name, Color=@color, Price=@price,
                        Unit=@unit, Quantity=@quantity, QuantityType=@quantityType,
                        Article=@article, Currency=@currency, Description=@description
                    WHERE Id=@id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@id", m.Id);
                command.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
                command.Parameters.AddWithValue("@name", (object?)m.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@color", (object?)m.Color ?? DBNull.Value);
                command.Parameters.AddWithValue("@price", m.Price);
                command.Parameters.AddWithValue("@unit", (object?)m.Unit ?? DBNull.Value);
                command.Parameters.AddWithValue("@quantity", m.Quantity);
                command.Parameters.AddWithValue("@quantityType", (object?)m.QuantityType ?? DBNull.Value);
                command.Parameters.AddWithValue("@article", (object?)m.Article ?? DBNull.Value);
                command.Parameters.AddWithValue("@currency", (object?)m.Currency ?? "грн");
                command.Parameters.AddWithValue("@description", (object?)m.Description ?? DBNull.Value);

                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating material: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Сумісний зі старим інтерфейсом метод оновлення.
        /// </summary>
        public bool UpdateMaterial(int id, string category, string name, string color, double price, string unit, string quantityType, string description)
        {
            var m = new Material
            {
                Id = id,
                Category = category,
                Name = name,
                Color = color,
                Price = price,
                Unit = unit,
                Quantity = 0,              // залишаємо як є (старий API)
                QuantityType = quantityType,    // тепер зберігаємо
                Article = "",              // не змінюємо
                Currency = "грн",           // не змінюємо
                Description = description
            };
            return UpdateMaterial(m);
        }

        /// <summary>
        /// Видаляє матеріал за Id.
        /// </summary>
        public void DeleteMaterial(int id)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = "DELETE FROM Materials WHERE Id = @id";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting material: {ex.Message}");
            }
        }

        /// <summary>
        /// Масовий upsert матеріалів.
        /// Пріоритет ключа пошуку:
        /// 1) Id (якщо > 0), 2) Article (якщо не порожній), 3) (Category, Name, Color, Unit).
        /// </summary>
        public void BulkUpsertMaterials(List<Material> items)
        {
            if (items == null || items.Count == 0) return;

            EnsureMaterialColumns();

            using var connection = CreateConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                foreach (var m in items)
                {
                    int? existingId = null;

                    // 1) за Id
                    if (m.Id > 0)
                    {
                        using var findById = new SqliteCommand("SELECT Id FROM Materials WHERE Id=@id LIMIT 1;", connection, tx);
                        findById.Parameters.AddWithValue("@id", m.Id);
                        var res = findById.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                            existingId = Convert.ToInt32(res);
                    }

                    // 2) за Article
                    if (existingId == null && !string.IsNullOrWhiteSpace(m.Article))
                    {
                        using var findByArticle = new SqliteCommand("SELECT Id FROM Materials WHERE Article=@a LIMIT 1;", connection, tx);
                        findByArticle.Parameters.AddWithValue("@a", m.Article);
                        var res = findByArticle.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                            existingId = Convert.ToInt32(res);
                    }

                    // 3) за натуральним ключем
                    if (existingId == null)
                    {
                        const string qFind = @"
                            SELECT Id FROM Materials
                            WHERE Category = @category AND Name = @name
                              AND ifnull(Color,'') = ifnull(@color,'')
                              AND ifnull(Unit,'')  = ifnull(@unit,'')
                            LIMIT 1;";
                        using var find = new SqliteCommand(qFind, connection, tx);
                        find.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
                        find.Parameters.AddWithValue("@name", (object?)m.Name ?? DBNull.Value);
                        find.Parameters.AddWithValue("@color", (object?)m.Color ?? DBNull.Value);
                        find.Parameters.AddWithValue("@unit", (object?)m.Unit ?? DBNull.Value);

                        var res = find.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                            existingId = Convert.ToInt32(res);
                    }

                    if (existingId != null)
                    {
                        // UPDATE
                        const string qUpd = @"
                            UPDATE Materials
                            SET Category=@category, Name=@name, Color=@color, Price=@price,
                                Unit=@unit, Quantity=@quantity, QuantityType=@quantityType,
                                Article=@article, Currency=@currency, Description=@description
                            WHERE Id=@id;";
                        using var upd = new SqliteCommand(qUpd, connection, tx);
                        upd.Parameters.AddWithValue("@id", existingId.Value);
                        upd.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@name", (object?)m.Name ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@color", (object?)m.Color ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@price", m.Price);
                        upd.Parameters.AddWithValue("@unit", (object?)m.Unit ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@quantity", m.Quantity);
                        upd.Parameters.AddWithValue("@quantityType", (object?)m.QuantityType ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@article", (object?)m.Article ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@currency", (object?)m.Currency ?? "грн");
                        upd.Parameters.AddWithValue("@description", (object?)m.Description ?? DBNull.Value);
                        upd.ExecuteNonQuery();
                    }
                    else
                    {
                        // INSERT
                        const string qIns = @"
                            INSERT INTO Materials (Category, Name, Color, Price, Unit, Quantity, QuantityType, Article, Currency, Description)
                            VALUES (@category, @name, @color, @price, @unit, @quantity, @quantityType, @article, @currency, @description);";
                        using var ins = new SqliteCommand(qIns, connection, tx);
                        ins.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@name", (object?)m.Name ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@color", (object?)m.Color ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@price", m.Price);
                        ins.Parameters.AddWithValue("@unit", (object?)m.Unit ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@quantity", m.Quantity);
                        ins.Parameters.AddWithValue("@quantityType", (object?)m.QuantityType ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@article", (object?)m.Article ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@currency", (object?)m.Currency ?? "грн");
                        ins.Parameters.AddWithValue("@description", (object?)m.Description ?? DBNull.Value);
                        ins.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"BulkUpsertMaterials error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Експортує всі матеріали в CSV.
        /// Формат: Id,Category,Name,Color,Price,Unit,Quantity,QuantityType,Article,Currency,Description
        /// </summary>
        public void ExportMaterialsToCsv(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("CSV path is empty.");

            var list = ReadMaterials();

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)); // UTF-8 BOM для Excel

            // Заголовок
            sw.WriteLine("Id,Category,Name,Color,Price,Unit,Quantity,QuantityType,Article,Currency,Description");

            string Esc(string? s) => $"\"{(s ?? string.Empty).Replace("\"", "\"\"")}\"";

            foreach (var m in list)
            {
                var price = m.Price.ToString(CultureInfo.InvariantCulture);
                var quantity = m.Quantity.ToString(CultureInfo.InvariantCulture);

                sw.WriteLine(string.Join(",",
                    m.Id,
                    Esc(m.Category),
                    Esc(m.Name),
                    Esc(m.Color),
                    price,
                    Esc(m.Unit),
                    quantity,
                    Esc(m.QuantityType),
                    Esc(m.Article),
                    Esc(m.Currency ?? "грн"),
                    Esc(m.Description)
                ));
            }
        }
    }
}
