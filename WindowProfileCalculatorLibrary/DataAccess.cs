using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using WinCalc.Common;


namespace WindowProfileCalculatorLibrary
{
    public class DataAccess
    {
        private string ConnectionString => DbConfig.ConnectionString;

        // =====================================================================
        // ЧИТАННЯ МАТЕРІАЛІВ (READ)
        // =====================================================================

        public List<Material> GetAllMaterials()
        {
            var materials = new List<Material>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                // Объединяем записи из всех специализированных таблиц
                string sql = @"
                    SELECT Id, 'Профіль' AS CategoryName, Name, Color, Price, Unit, Description FROM Profiles
                    UNION ALL
                    SELECT Id, 'Склопакет' AS CategoryName, Name, Color, Price, Unit, Description FROM GlassPacks
                    UNION ALL
                    SELECT Id, 'Фурнітура' AS CategoryName, Name, Color, Price, Unit, Description FROM Fittings
                    UNION ALL
                    SELECT Id, 'Армування' AS CategoryName, Name, Color, Price, Unit, Description FROM Reinforcements
                    UNION ALL
                    SELECT Id, 'Ущільнювач' AS CategoryName, Name, Color, Price, Unit, Description FROM Seals
                    UNION ALL
                    SELECT Id, 'Москітна сітка' AS CategoryName, Name, Color, Price, Unit, Description FROM Accessories
                    ;";

                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    materials.Add(MapMaterialFromSpecific(reader));
                }
            }
            return materials;
        }

        public Material? GetMaterialByCategory(string categoryName, string nameFilter = "")
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            string table = categoryName switch
            {
                "Профіль" => "Profiles",
                "Склопакет" => "GlassPacks",
                "Фурнітура" => "Fittings",
                "Армування" => "Reinforcements",
                "Ущільнювач" => "Seals",
                "Москітна сітка" => "Accessories",
                _ => "Materials" // fallback
            };

            string sql = $"SELECT Id, Name, Color, Price, Unit, Description FROM {table} WHERE 1=1";

            if (!string.IsNullOrEmpty(nameFilter))
                sql += " AND Name LIKE @filter";

            sql += " LIMIT 1";

            using var cmd = new SqliteCommand(sql, conn);
            if (!string.IsNullOrEmpty(nameFilter))
                cmd.Parameters.AddWithValue("@filter", $"%{nameFilter}%");

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapMaterialFromSpecific(reader, categoryName);
            }

            return null;
        }

        public Material? GetMaterialByCategoryAndBrand(string categoryName, string brand)
        {
            return GetMaterialByCategory(categoryName, brand);
        }

        // =====================================================================
        // ДОПОМІЖНІ МЕТОДИ (HELPER METHODS)
        // =====================================================================

        public List<string> GetDistinctBrands()
        {
            var list = new List<string>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            string sql = "SELECT DISTINCT Name FROM Profiles";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(reader.GetString(0));
            return list;
        }

        public List<string> GetDistinctProfileThicknesses()
        {
            // Повертаємо приклад значень; при потребі можна парсити Description або MaterialAttributes
            return new List<string> { "60 мм", "70 мм", "80 мм" };
        }

        public List<Category> GetAllCategories()
        {
            var list = new List<Category>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqliteCommand("SELECT Id, Name FROM Categories", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Category { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return list;
        }

        // =====================================================================
        // СТВОРЕННЯ ТА РЕДАГУВАННЯ (CREATE / UPDATE)
        // =====================================================================

        public bool AddMaterial(Material m)
        {
            // Если категория указана — попробуем определить целевую таблицу
            string table = CategoryToTable(m.Category);
            if (table == null) return false;

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            string sql = $@"INSERT INTO {table} (Name, Color, Price, Unit, Description) 
                           VALUES (@n, @c, @p, @u, @d)";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@n", m.Name);
            cmd.Parameters.AddWithValue("@c", (object?)m.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p", m.Price);
            cmd.Parameters.AddWithValue("@u", m.Unit);
            cmd.Parameters.AddWithValue("@d", (object?)m.Description ?? DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateMaterial(Material m)
        {
            string table = CategoryToTable(m.Category);
            if (table == null) return false;

            try
            {
                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();

                string sql = $@"
                    UPDATE {table}
                    SET Name=@n, Color=@col, Price=@p, Unit=@u, Description=@d
                    WHERE Id=@id;";

                using var cmd = new SqliteCommand(sql, conn);
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

        // =====================================================================
        // ПРИВАТНІ ДОПОМІЖНІ МЕТОДИ
        // =====================================================================

        private Material MapMaterialFromSpecific(SqliteDataReader reader, string categoryName = null)
        {
            // reader columns: Id, [maybe CategoryName], Name, Color, Price, Unit, Description
            // We handle both: unioned query in GetAllMaterials returns (Id, CategoryName, Name, Color, Price, Unit, Description)
            var mat = new Material();
            int ordinal = 0;

            if (reader.FieldCount == 7)
            {
                // Id, CategoryName, Name, Color, Price, Unit, Description
                mat.Id = reader.GetInt32(0);
                mat.Category = reader.GetString(1);
                mat.Name = reader.GetString(2);
                mat.Color = reader.IsDBNull(3) ? null : reader.GetString(3);
                mat.Price = reader.GetDouble(4);
                mat.Unit = reader.GetString(5);
                mat.Description = reader.IsDBNull(6) ? null : reader.GetString(6);
                mat.CategoryId = GetCategoryIdByName(mat.Category);
            }
            else
            {
                // Id, Name, Color, Price, Unit, Description
                mat.Id = reader.GetInt32(0);
                mat.Category = categoryName ?? string.Empty;
                mat.Name = reader.GetString(1);
                mat.Color = reader.IsDBNull(2) ? null : reader.GetString(2);
                mat.Price = reader.GetDouble(3);
                mat.Unit = reader.GetString(4);
                mat.Description = reader.IsDBNull(5) ? null : reader.GetString(5);
                mat.CategoryId = GetCategoryIdByName(mat.Category);
            }

            return mat;
        }

        private int GetCategoryIdByName(string name)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqliteCommand("SELECT Id FROM Categories WHERE Name = @n", conn);
            cmd.Parameters.AddWithValue("@n", name);
            var res = cmd.ExecuteScalar();
            return res != null ? Convert.ToInt32(res) : -1;
        }

        private string? CategoryToTable(string? category)
        {
            return category switch
            {
                "Профіль" => "Profiles",
                "Склопакет" => "GlassPacks",
                "Фурнітура" => "Fittings",
                "Армування" => "Reinforcements",
                "Ущільнювач" => "Seals",
                "Москітна сітка" => "Accessories",
                _ => "Materials"
            };
        }

        // =====================================================================
        // ІСТОРІЯ РОЗРАХУНКІВ
        // =====================================================================
        public void SaveCalculation(Calculation calc)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            string sql = @"INSERT INTO Calculations (UserId, TotalPrice, Width, Height, WindowType) 
                           VALUES (@uid, @total, @w, @h, @type)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", calc.UserId);
            cmd.Parameters.AddWithValue("@total", calc.TotalPrice);
            cmd.Parameters.AddWithValue("@w", calc.Width);
            cmd.Parameters.AddWithValue("@h", calc.Height);
            cmd.Parameters.AddWithValue("@type", calc.WindowType);
            cmd.ExecuteNonQuery();
        }
    }
}