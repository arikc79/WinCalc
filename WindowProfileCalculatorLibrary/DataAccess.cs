using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Клас для доступу до бази даних SQLite.
    /// Містить CRUD-операції і запити для матеріалів.
    /// </summary>
    public class DataAccess
    {
        // ✅ ТЕПЕР БЕРЕМО ШЛЯХ З КОНФІГА
        private string ConnectionString => DbConfig.ConnectionString;

        // =====================================================================
        // МАТЕРІАЛИ
        // =====================================================================

        /// <summary>
        /// Отримати всі матеріали.
        /// </summary>
        public List<Material> GetAllMaterials()
        {
            var materials = new List<Material>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT * FROM Materials", connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    materials.Add(new Material
                    {
                        Id = reader.GetInt32(0),
                        Category = reader.GetString(1),
                        Name = reader.GetString(2),
                        Color = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Price = reader.GetDouble(4),
                        Unit = reader.GetString(5),
                        Description = reader.IsDBNull(6) ? null : reader.GetString(6)
                    });
                }
            }
            return materials;
        }

        public Material? GetMaterialByCategory(string category, string nameFilter = "")
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string query = "SELECT * FROM Materials WHERE Category = @c";
            if (!string.IsNullOrEmpty(nameFilter))
                query += " AND Name LIKE @n";
            query += " LIMIT 1";

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@c", category);
            if (!string.IsNullOrEmpty(nameFilter))
                cmd.Parameters.AddWithValue("@n", $"%{nameFilter}%");

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Material
                {
                    Id = reader.GetInt32(0),
                    Category = reader.GetString(1),
                    Name = reader.GetString(2),
                    Color = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Price = reader.GetDouble(4),
                    Unit = reader.GetString(5),
                    Description = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
            }
            return null;
        }

        public List<string> GetDistinctBrands()
        {
            var list = new List<string>();
            using var con = new SqliteConnection(ConnectionString);
            con.Open();
            var cmd = new SqliteCommand("SELECT DISTINCT Name FROM Materials WHERE Category='Профіль'", con);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
            return list;
        }

        public List<string> GetDistinctProfileThicknesses()
        {
            // Приклад: якщо товщина зберігається в описі або окремо
            // Поки повертаємо заглушку або логіку вибірки
            return new List<string> { "3-камерний", "4-камерний", "5-камерний" };
        }

        public bool UpdateMaterial(Material m)
        {
            try
            {
                using var con = new SqliteConnection(ConnectionString);
                con.Open();

                var cmd = new SqliteCommand(@"
                    UPDATE Materials 
                    SET Category=@c, Name=@n, Color=@col, Price=@p, Unit=@u, Description=@d
                    WHERE Id=@id;", con);

                cmd.Parameters.AddWithValue("@c", m.Category);
                cmd.Parameters.AddWithValue("@n", m.Name);
                cmd.Parameters.AddWithValue("@col", (object?)m.Color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", m.Price);
                cmd.Parameters.AddWithValue("@u", m.Unit);
                cmd.Parameters.AddWithValue("@d", (object?)m.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", m.Id);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ UpdateMaterial error: " + ex.Message);
                return false;
            }
        }

        public bool AddMaterial(Material m)
        {
            try
            {
                using var con = new SqliteConnection(ConnectionString);
                con.Open();

                var cmd = new SqliteCommand(@"
            INSERT INTO Materials (Category, Name, Color, Price, Unit, Description)
            VALUES (@c, @n, @col, @p, @u, @d);", con);

                cmd.Parameters.AddWithValue("@c", m.Category);
                cmd.Parameters.AddWithValue("@n", m.Name);
                cmd.Parameters.AddWithValue("@col", (object?)m.Color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", m.Price);
                cmd.Parameters.AddWithValue("@u", m.Unit);
                cmd.Parameters.AddWithValue("@d", (object?)m.Description ?? DBNull.Value);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ AddMaterial error: " + ex.Message);
                return false;
            }
        }
    }
}