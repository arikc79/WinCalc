// WinCalc/DataAccessExtensions.cs
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    /// <summary>
    /// Розширення для роботи з матеріалами (видалення, експорт)
    /// </summary>
    public static class DataAccessExtensions
    {
        private static readonly string _dbPath = Path.Combine(AppContext.BaseDirectory, "window_calc.db");

        /// <summary>
        /// Видаляє матеріал з таблиці Materials за Id.
        /// </summary>
        public static void DeleteMaterial(this DataAccess da, int id)
        {
            try
            {
                using var con = new SqliteConnection($"Data Source={_dbPath}");
                con.Open();

                using var cmd = new SqliteCommand("DELETE FROM Materials WHERE Id=@id;", con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка при видаленні матеріалу ID={id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Експортує всі матеріали у CSV через CsvMaterialImporter.
        /// </summary>
        public static void ExportToCsv(this DataAccess da, string filePath)
        {
            var materials = da.GetAllMaterials();
            CsvMaterialImporter.Export(filePath, materials);
        }
    }
}
