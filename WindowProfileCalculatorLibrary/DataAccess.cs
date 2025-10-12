using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Клас для доступу до бази даних SQLite.
    /// Містить CRUD-операції і запити для матеріалів і користувачів.
    /// </summary>
    public class DataAccess
    {
        private readonly string _dbPath = "window_calc.db";

        // =====================================================================
        // МАТЕРІАЛИ
        // =====================================================================

        /// <summary>
        /// Отримати всі матеріали.
        /// </summary>
        public List<Material> GetAllMaterials()
        {
            var materials = new List<Material>();

            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
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
                        QuantityType = reader.GetString(6),
                        Description = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
            }
            return materials;
        }

        /// <summary>
        /// Отримати унікальні бренди (з таблиці Materials, Category = "Профіль").
        /// </summary>
        public List<string> GetDistinctBrands()
        {
            var brands = new List<string>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = new SqliteCommand("SELECT DISTINCT Name FROM Materials WHERE Category='Профіль'", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                brands.Add(reader.GetString(0));
            return brands;
        }

        /// <summary>
        /// Отримати унікальні товщини профілю (якщо є поле QuantityType або Description з “70 мм” тощо).
        /// </summary>
        public List<string> GetDistinctProfileThicknesses()
        {
            var result = new List<string>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = new SqliteCommand("SELECT DISTINCT Description FROM Materials WHERE Category='Профіль'", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var val = reader.IsDBNull(0) ? "" : reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(val))
                    result.Add(val);
            }
            return result;
        }

        /// <summary>
        /// Знайти матеріал за категорією та брендом.
        /// </summary>
        public Material? GetMaterialByCategoryAndBrand(string category, string brand)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var cmd = new SqliteCommand(
                "SELECT * FROM Materials WHERE Category=@Category AND Name=@Brand LIMIT 1", connection);
            cmd.Parameters.AddWithValue("@Category", category);
            cmd.Parameters.AddWithValue("@Brand", brand);

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
                    QuantityType = reader.GetString(6),
                    Description = reader.IsDBNull(7) ? null : reader.GetString(7)
                };
            }
            return null;
        }

        /// <summary>
        /// Знайти матеріал за категорією і назвою (наприклад "Ручка", "Преміум").
        /// </summary>
        public Material? GetMaterialByCategory(string category, string name)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var cmd = new SqliteCommand(
                "SELECT * FROM Materials WHERE Category=@Category AND Name=@Name LIMIT 1", connection);
            cmd.Parameters.AddWithValue("@Category", category);
            cmd.Parameters.AddWithValue("@Name", name);

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
                    QuantityType = reader.GetString(6),
                    Description = reader.IsDBNull(7) ? null : reader.GetString(7)
                };
            }
            return null;
        }

        // =====================================================================
        // КОРИСТУВАЧІ
        // =====================================================================
        public List<(int Id, string Login, string Password, string Role)> GetAllUsers()
        {
            var users = new List<(int, string, string, string)>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = new SqliteCommand("SELECT * FROM Users", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                users.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
            return users;
        }

        // =====================================================================
        // ЕКСПОРТ / ІМПОРТ CSV
        // =====================================================================
        public void ExportToCsv(string filePath)
        {
            var materials = GetAllMaterials();
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("Id,Category,Name,Color,Price,Unit,QuantityType,Description");

            foreach (var m in materials)
            {
                writer.WriteLine($"{m.Id},{m.Category},{m.Name},{m.Color},{m.Price},{m.Unit},{m.QuantityType},{m.Description}");
            }
        }

        public void DeleteMaterial(int id)
        {
            using var con = new SqliteConnection($"Data Source={_dbPath}");
            con.Open();
            var cmd = new SqliteCommand("DELETE FROM Materials WHERE Id=@id", con);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

    }
}
