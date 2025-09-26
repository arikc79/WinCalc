using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace WindowProfileCalculatorLibrary
{
    public class DataAccess
    {
        // Єдиний шлях до БД
        private readonly string _dbPath = "window_calc.db";
        private SqliteConnection CreateConnection() => new SqliteConnection($"Data Source={_dbPath}");

        // ------------------------
        // USERS (як було)
        // ------------------------
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

        // =======================================================
        // MATERIALS (виправлено мепінг + додано нові поля)
        // =======================================================

        /// <summary>
        /// Разова «міграція» схеми: додає відсутні колонки.
        /// </summary>
        private void EnsureColumns()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();

                // читаємо опис таблиці
                using var cmdInfo = new SqliteCommand("PRAGMA table_info(Materials);", connection);
                var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var rd = cmdInfo.ExecuteReader())
                {
                    while (rd.Read()) cols.Add(rd.GetString(1));
                }

                void AddColumnIfMissing(string name, string sqlType, string defaultSql = null)
                {
                    if (cols.Contains(name)) return;
                    var alter = $"ALTER TABLE Materials ADD COLUMN {name} {sqlType}" +
                                (string.IsNullOrWhiteSpace(defaultSql) ? ";" : $" DEFAULT {defaultSql};");
                    using var cmd = new SqliteCommand(alter, connection);
                    cmd.ExecuteNonQuery();
                }

                // НОВІ/оновлені колонки:
                AddColumnIfMissing("Quantity", "REAL", "0");
                AddColumnIfMissing("Article", "TEXT", "''");
                AddColumnIfMissing("Currency", "TEXT", "''");
                // QuantityType/Description/Unit/etc. у вас вже були — лишаємо.
            }
            catch
            {
                // тихо, аби не завалити запуск (опціонально логувати)
            }
        }

        /// <summary>Зчитати всі матеріали.</summary>
        public List<Material> ReadMaterials()
        {
            EnsureColumns();

            var list = new List<Material>();
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                const string query = @"
                    SELECT Id, Category, Name, Color, Price, Unit,
                           Quantity, QuantityType, Article, Currency, Description
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
                        Currency = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        Description = reader.IsDBNull(10) ? "" : reader.GetString(10)
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

        /// <summary>Створити матеріал і повернути його з Id.</summary>
        public Material CreateMaterial(Material m)
        {
            try
            {
                EnsureColumns();

                using var connection = CreateConnection();
                connection.Open();
                const string query = @"
                    INSERT INTO Materials
                        (Category, Name, Color, Price, Unit,
                         Quantity, QuantityType, Article, Currency, Description)
                    VALUES
                        (@category, @name, @color, @price, @unit,
                         @quantity, @quantityType, @article, @currency, @description);
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
                command.Parameters.AddWithValue("@currency", (object?)m.Currency ?? DBNull.Value);
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

        /// <summary>Сумісність зі старими викликами CreateMaterial(...).</summary>
        public int CreateMaterial(string category, string name, string color, double price, string unit, string quantityType, string description)
        {
            EnsureColumns();
            var m = new Material
            {
                Category = category,
                Name = name,
                Color = color,
                Price = price,
                Unit = unit,
                Quantity = 0,
                QuantityType = quantityType,
                Article = "",
                Currency = "",
                Description = description
            };
            return CreateMaterial(m).Id;
        }

        /// <summary>Оновити матеріал.</summary>
        public bool UpdateMaterial(Material m)
        {
            EnsureColumns();
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
                command.Parameters.AddWithValue("@currency", (object?)m.Currency ?? DBNull.Value);
                command.Parameters.AddWithValue("@description", (object?)m.Description ?? DBNull.Value);

                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating material: {ex.Message}");
                return false;
            }
        }

        /// <summary>Сумісність зі старими викликами UpdateMaterial(...).</summary>
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
                QuantityType = quantityType,
                // для сумісності — Quantity/Article/Currency залишимо дефолтними
                Quantity = 0,
                Article = "",
                Currency = "",
                Description = description
            };
            return UpdateMaterial(m);
        }

        /// <summary>Видалити матеріал за Id.</summary>
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
    }
}
