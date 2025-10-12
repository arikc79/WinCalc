using System;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Головний клас логіки обчислень і ініціалізації БД.
    /// </summary>
    public class Obchyslennya
    {
        private readonly string _dbPath = "window_calc.db";
        private readonly DataAccess _dataAccess = new();

        // =====================================================================
        // ІНІЦІАЛІЗАЦІЯ БАЗИ ДАНИХ
        // =====================================================================
        /// <summary>
        /// Створює таблиці Materials і Users у базі даних, якщо вони ще не існують.
        /// </summary>
        public void CreateTables()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    Console.WriteLine("Connection opened in CreateTables.");

                    // ---- Таблиця користувачів ----
                    string createUsersTable = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Login TEXT NOT NULL,
                            Password TEXT NOT NULL,
                            Role TEXT NOT NULL
                        )";
                    using (var command = new SqliteCommand(createUsersTable, connection))
                        command.ExecuteNonQuery();

                    // ---- Таблиця матеріалів ----
                    string createMaterialsTable = @"
                        CREATE TABLE IF NOT EXISTS Materials (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Category TEXT NOT NULL,
                            Name TEXT NOT NULL,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            QuantityType TEXT NOT NULL,
                            Description TEXT
                        )";
                    using (var command = new SqliteCommand(createMaterialsTable, connection))
                        command.ExecuteNonQuery();

                    connection.Close();
                    Console.WriteLine("CreateTables completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateTables: {ex.Message}");
            }
        }

        // =====================================================================
        // РОЗРАХУНОК ВАРТОСТІ ВІКНА
        // =====================================================================
        /// <summary>
        /// Обчислює загальну вартість вікна з урахуванням усіх матеріалів.
        /// </summary>
        public decimal CalculateWindowPrice(WindowConfig config)
        {
            try
            {
                decimal area = (config.Width * config.Height) / 1_000_000m;  // м²
                decimal perimeter = ((config.Width + config.Height) * 2) / 1000m; // м

                decimal total = 0m;

                // ---------- 1. Профіль ----------
                var profile = _dataAccess.GetMaterialByCategoryAndBrand("Профіль", config.Brand);
                if (profile != null)
                    total += perimeter * (decimal)profile.Price;

                // ---------- 2. Склопакет ----------
                var glass = _dataAccess.GetMaterialByCategory("Склопакет", config.GlassType);
                if (glass != null)
                    total += area * (decimal)glass.Price;

                // ---------- 3. Ручка ----------
                var handle = _dataAccess.GetMaterialByCategory("Ручка", config.HandleType);
                if (handle != null)
                    total += (decimal)handle.Price;

                // ---------- 4. Підвіконня ----------
                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", config.SillType);
                if (sill != null)
                    total += (config.Width / 1000m) * (decimal)sill.Price;

                // ---------- 5. Відлив ----------
                var drain = _dataAccess.GetMaterialByCategory("Відлив", config.DrainType);
                if (drain != null)
                    total += (config.Width / 1000m) * (decimal)drain.Price;

                // ---------- 6. Москітна сітка ----------
                if (config.HasMosquito)
                {
                    var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");
                    if (mosquito != null)
                        total += area * (decimal)mosquito.Price;
                }

                // ---------- 7. Монтаж / запас ----------
                total *= 1.1m;

                return Math.Round(total, 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка у CalculateWindowPrice: {ex.Message}");
                return 0m;
            }
        }

        // =====================================================================
        // ОБЧИСЛЕННЯ ДОВЖИН ПРОФІЛЮ (залишаємо як було)
        // =====================================================================
        public double CalculateProfileLengthType1(double width, double height, double frameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("Усі параметри повинні бути додатними.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double sashWidth = width - 2 * frameWidth + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            return framePerimeter + sashPerimeter;
        }

        public double CalculateProfileLengthType2(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("Усі параметри повинні бути додатними.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = (height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4;
            double sashWidth = width / 2 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            return framePerimeter + mullionLength + sashPerimeter;
        }

        public double CalculateProfileLengthType3(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("Усі параметри повинні бути додатними.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = 2 * ((height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4);
            double sashWidth = width / 3 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            return framePerimeter + mullionLength + sashPerimeter;
        }

        public double CalculateProfileLengthType4(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("Усі параметри повинні бути додатними.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = 3 * ((height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4);
            double sashWidth = width / 4 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            double totalSashLength = 2 * sashPerimeter;
            return framePerimeter + mullionLength + totalSashLength;
        }

        public double CalculateProfileLengthType5(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("Усі параметри повинні бути додатними.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = 4 * ((height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4);
            double sashWidth = width / 5 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            double totalSashLength = 2 * sashPerimeter;
            return framePerimeter + mullionLength + totalSashLength;
        }
    }
}
