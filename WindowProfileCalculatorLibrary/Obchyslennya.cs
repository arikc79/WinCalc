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
        private static readonly string _dbPath = "window_calc.db";
        private readonly DataAccess _dataAccess = new();

        // =====================================================================
        // ІНІЦІАЛІЗАЦІЯ БАЗИ ДАНИХ
        // =====================================================================
        /// <summary>
        /// Створює таблиці Materials і Users у базі даних, якщо вони ще не існують.
        /// </summary>
        public static void CreateTables()
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
                decimal width = config.Width;
                decimal height = config.Height;
                const decimal frameWidth = 60m;

                int impostCount = ResolveImpostCount(config.WindowType);
                decimal sectionCount = impostCount + 1;
                decimal framePerimeter = 2m * (width + height - 2m * frameWidth);
                decimal sashPerimeterSingle = 2m * ((width / sectionCount) + height - 2m * frameWidth);
                decimal sashPerimeterTotal = sashPerimeterSingle * config.SashCount;
                decimal impostLength = impostCount * (height - 2m * frameWidth);
                decimal glassArea = (width / 1000m) * (height / 1000m);
                int glassMultiplier = ResolveGlassMultiplier(config.GlassType);

                decimal total = 0m;

                var profile = _dataAccess.GetMaterialByCategoryAndBrand("Профіль", config.Brand);
                decimal profilePrice = (decimal)(profile?.Price ?? 0);
                if (profilePrice > 0)
                {
                    total += (framePerimeter / 1000m) * profilePrice;
                    total += (sashPerimeterTotal / 1000m) * profilePrice;
                    total += (impostLength / 1000m) * profilePrice;
                }

                var arm = _dataAccess.GetMaterialByCategory("Армування", "Стандарт");
                var seal = _dataAccess.GetMaterialByCategory("Ущільнювач скла", "Стандарт");
                decimal armPrice = (decimal)(arm?.Price ?? 0);
                decimal sealPrice = (decimal)(seal?.Price ?? 0);
                if (armPrice > 0 || sealPrice > 0)
                {
                    total += ((framePerimeter + sashPerimeterTotal + impostLength) / 1000m) * (armPrice + sealPrice);
                }

                var glass = _dataAccess.GetMaterialByCategory("Склопакет", config.GlassType);
                decimal glassPrice = (decimal)(glass?.Price ?? 0);
                if (glassPrice > 0)
                {
                    total += glassArea * glassMultiplier * glassPrice;
                }

                var handle = _dataAccess.GetMaterialByCategory("Ручка", config.HandleType);
                var hinge = _dataAccess.GetMaterialByCategory("Петлі комплект", "Стандарт");
                decimal handlePrice = (decimal)(handle?.Price ?? 0);
                decimal hingePrice = (decimal)(hinge?.Price ?? 0);
                if (handlePrice > 0 || hingePrice > 0)
                {
                    total += config.SashCount * (handlePrice + hingePrice);
                }

                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", config.SillType);
                var drain = _dataAccess.GetMaterialByCategory("Відлив", config.DrainType);
                decimal sillPrice = (decimal)(sill?.Price ?? 0);
                decimal drainPrice = (decimal)(drain?.Price ?? 0);
                if (sillPrice > 0 || drainPrice > 0)
                {
                    total += (width / 1000m) * (sillPrice + drainPrice);
                }

                if (config.HasMosquito)
                {
                    var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");
                    if (mosquito != null)
                    {
                        total += glassArea * (decimal)mosquito.Price;
                    }
                }

                return Math.Round(total, 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка у CalculateWindowPrice: {ex.Message}");
                return 0m;
            }
        }

        public static int ResolveGlassMultiplier(string glassType)
        {
            if (string.IsNullOrWhiteSpace(glassType))
                return 2;

            if (glassType.Contains("2", StringComparison.OrdinalIgnoreCase))
                return 3;

            if (glassType.Contains("енергозбер", StringComparison.OrdinalIgnoreCase))
                return 4;

            return 2;
        }

        private static int ResolveImpostCount(string windowType)
        {
            if (string.IsNullOrWhiteSpace(windowType))
                return 0;

            if (windowType.Contains("Одностулков", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (windowType.Contains("Двостулков", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (windowType.Contains("Трипіль", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (windowType.Contains("Балкон", StringComparison.OrdinalIgnoreCase))
                return 3;

            return 4;
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
