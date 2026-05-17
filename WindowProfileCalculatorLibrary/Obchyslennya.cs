
using System;
using WinCalc.Common;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Головний клас логіки обчислень і ініціалізації БД.
    /// </summary>
    public class Obchyslennya
    {
        private readonly DataAccess _dataAccess = new();

        // =====================================================================
        // ІНІЦІАЛІЗАЦІЯ БАЗИ ДАНИХ (перенаправляємо на DataInitializer)
        // =====================================================================
        public static void CreateTables() => DataInitializer.InsertInitialData();

        // =====================================================================
        // РОЗРАХУНОК ВАРТОСТІ ВІКНА
        // =====================================================================
        public decimal CalculateWindowPrice(WindowConfig config)
        {
            try
            {
                decimal widthM = config.Width / 1000m;
                decimal heightM = config.Height / 1000m;

                var profile = _dataAccess.GetMaterialByCategory("Профіль", config.Brand);
                var glass = _dataAccess.GetMaterialByCategory("Склопакет", config.GlassType);
                var hardware = _dataAccess.GetMaterialByCategory("Фурнітура", "Vorne");
                var reinforcement = _dataAccess.GetMaterialByCategory("Армування", "П-подібне");
                var rubber = _dataAccess.GetMaterialByCategory("Ущільнювач", "Гумовий");
                var hinge = _dataAccess.GetMaterialByCategory("Петлі", "Комплект");
                var handle = _dataAccess.GetMaterialByCategory("Ручка", config.HandleType);
                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", config.SillType);
                var drain = _dataAccess.GetMaterialByCategory("Відлив", config.DrainType);

                decimal priceProfile = (decimal)(profile?.Price ?? 0);
                decimal priceGlass = (decimal)(glass?.Price ?? 0);
                decimal priceHardware = (decimal)(hardware?.Price ?? 0);
                decimal priceReinf = (decimal)(reinforcement?.Price ?? 0);
                decimal priceRubber = (decimal)(rubber?.Price ?? 0);
                decimal priceHinge = (decimal)(hinge?.Price ?? 0);
                decimal priceHandle = (decimal)(handle?.Price ?? 0);
                decimal priceSill = (decimal)(sill?.Price ?? 0);
                decimal priceDrain = (decimal)(drain?.Price ?? 0);

                decimal framePerimeter = (widthM + heightM) * 2m;
                decimal sashPerimeter = (widthM + heightM) * 2m * config.SashCount;
                decimal totalProfileLength = framePerimeter + sashPerimeter;

                decimal glassArea = widthM * heightM;
                int glassMultiplier = ResolveGlassMultiplier(config.GlassType);

                decimal total = 0m;

                total += totalProfileLength * priceProfile;
                total += glassArea * glassMultiplier * priceGlass;
                total += config.SashCount * (priceHardware + priceHandle + priceHinge);
                total += totalProfileLength * (priceReinf + priceRubber);
                total += widthM * (priceSill + priceDrain);

                if (config.HasMosquito)
                {
                    var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");
                    total += glassArea * (decimal)(mosquito?.Price ?? 0);
                }

                return Math.Round(total, 2);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка розрахунку: {ex.Message}", ex);
            }
        }

        public static int ResolveGlassMultiplier(string glassType)
        {
            if (string.IsNullOrWhiteSpace(glassType))
                return 2;

            if (glassType.Contains("2-камер", StringComparison.OrdinalIgnoreCase)) return 3;
            if (glassType.Contains("Енергозбереж", StringComparison.OrdinalIgnoreCase)) return 4;

            return 2;
        }
    }
}