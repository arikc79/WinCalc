using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace WindowProfileCalculatorLibrary
{
    public class DataAccess
    {
        private readonly string _dbPath = "window_calc.db";
        private SqliteConnection CreateConnection() => new SqliteConnection($"Data Source={_dbPath}");

        // ---------------- USERS (як було) ----------------

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
                    users.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
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

        // ---------------- MATERIALS ----------------

        // CHANGE: автододавання нових колонок у таблицю (міграція на льоту)
        private void EnsureColumns()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();

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

                AddColumnIfMissing("Quantity", "REAL", "0");
                AddColumnIfMissing("Article", "TEXT", "''");
                AddColumnIfMissing("Currency", "TEXT", "''");
            }
            catch { /* silent */ }
        }

        // CHANGE: читаємо з урахуванням нових полів
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

        // CHANGE: Create із новими полями
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
                BindMaterial(command, m);

                var id = (long)command.ExecuteScalar();
                m.Id = (int)id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating material: {ex.Message}");
            }
            return m;
        }

        // BACK-COMPAT: старий виклик на 7 аргументів (щоб не ламався існуючий код)
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

        // CHANGE: Update із новими полями
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
                BindMaterial(command, m);
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating material: {ex.Message}");
                return false;
            }
        }

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
                Quantity = 0,
                QuantityType = quantityType,
                Article = "",
                Currency = "",
                Description = description
            };
            return UpdateMaterial(m);
        }

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

        // NEW: пакетний upsert для CSV-імпорту
        public (int inserted, int updated) BulkUpsertMaterials(IEnumerable<Material> items)
        {
            EnsureColumns();
            int ins = 0, upd = 0;

            using var connection = CreateConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();

            const string sqlFindByArticle = @"SELECT Id FROM Materials WHERE Article=@article LIMIT 1;";
            const string sqlFindByNameCat = @"SELECT Id FROM Materials WHERE Name=@name AND Category=@category LIMIT 1;";

            const string sqlInsert = @"
                INSERT INTO Materials (Category, Name, Color, Price, Unit, Quantity, QuantityType, Article, Currency, Description)
                VALUES (@category, @name, @color, @price, @unit, @quantity, @quantityType, @article, @currency, @description);";

            const string sqlUpdate = @"
                UPDATE Materials SET
                    Category=@category, Name=@name, Color=@color, Price=@price,
                    Unit=@unit, Quantity=@quantity, QuantityType=@quantityType,
                    Article=@article, Currency=@currency, Description=@description
                WHERE Id=@id;";

            foreach (var m in items)
            {
                int id = 0;

                // 1) спроба знайти по артикулу
                if (!string.IsNullOrWhiteSpace(m.Article))
                {
                    using var f1 = new SqliteCommand(sqlFindByArticle, connection, tx);
                    f1.Parameters.AddWithValue("@article", m.Article);
                    var obj = f1.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value) id = Convert.ToInt32(obj);
                }

                // 2) якщо не знайшли — шукаємо по (Name, Category)
                if (id == 0 && !string.IsNullOrWhiteSpace(m.Name))
                {
                    using var f2 = new SqliteCommand(sqlFindByNameCat, connection, tx);
                    f2.Parameters.AddWithValue("@name", m.Name);
                    f2.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
                    var obj = f2.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value) id = Convert.ToInt32(obj);
                }

                if (id == 0)
                {
                    using var insCmd = new SqliteCommand(sqlInsert, connection, tx);
                    BindMaterial(insCmd, m);
                    insCmd.ExecuteNonQuery();
                    ins++;
                }
                else
                {
                    using var updCmd = new SqliteCommand(sqlUpdate, connection, tx);
                    updCmd.Parameters.AddWithValue("@id", id);
                    BindMaterial(updCmd, m);
                    updCmd.ExecuteNonQuery();
                    upd++;
                }
            }

            tx.Commit();
            return (ins, upd);
        }

        // HELPERS

        private static void BindMaterial(SqliteCommand cmd, Material m)
        {
            cmd.Parameters.AddWithValue("@category", (object?)m.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", (object?)m.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@color", (object?)m.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@price", m.Price);
            cmd.Parameters.AddWithValue("@unit", (object?)m.Unit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@quantity", m.Quantity);
            cmd.Parameters.AddWithValue("@quantityType", (object?)m.QuantityType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@article", (object?)m.Article ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@currency", (object?)m.Currency ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description", (object?)m.Description ?? DBNull.Value);
        }
    }
}
