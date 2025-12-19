
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
        public static void CreateTables()
        {
            // Используем единый инициализатор, который создаёт все таблиці і наповнює їх
            DataInitializer.InsertInitialData();
        }

        // =====================================================================
        // РОЗРАХУНОК ВАРТОСТІ ВІКНА
        // =====================================================================
        public decimal CalculateWindowPrice(WindowConfig config)
        {
            try
            {
                // Перевод размеров в метры (для удобства расчётов)
                decimal widthM = config.Width / 1000m;
                decimal heightM = config.Height / 1000m;

                // Получаем материалы по категориям (используем корректные имена категорий/наименований)
                var profile = _dataAccess.GetMaterialByCategoryAndBrand("Профіль", config.Brand);
                var glass = _dataAccess.GetMaterialByCategory("Склопакет", config.GlassType);
                var hardware = _dataAccess.GetMaterialByCategory("Фурнітура", "Vorne");
                var reinforcement = _dataAccess.GetMaterialByCategory("Армування", "П-подібне");
                var rubber = _dataAccess.GetMaterialByCategory("Ущільнювач", "Гумовий");
                var hinge = _dataAccess.GetMaterialByCategory("Петлі", "Комплект");
                var handle = _dataAccess.GetMaterialByCategory("Ручка", config.HandleType);

                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", config.SillType);
                var drain = _dataAccess.GetMaterialByCategory("Відлив", config.DrainType);

                // Цены (по умолчанию 0 если нет записи)
                decimal priceProfile = (decimal)(profile?.Price ?? 0);
                decimal priceGlass = (decimal)(glass?.Price ?? 0);
                decimal priceHardware = (decimal)(hardware?.Price ?? 0);
                decimal priceReinf = (decimal)(reinforcement?.Price ?? 0);
                decimal priceRubber = (decimal)(rubber?.Price ?? 0);
                decimal priceHinge = (decimal)(hinge?.Price ?? 0);
                decimal priceHandle = (decimal)(handle?.Price ?? 0);

                decimal priceSill = (decimal)(sill?.Price ?? 0);   // грн/м.п.
                decimal priceDrain = (decimal)(drain?.Price ?? 0); // грн/м.п.

                // Геометрия и длины (в метрах)
                // Простой расчёт периметра рамы и створок (приближённо, как в старой версии)
                double wM = (double)widthM;
                double hM = (double)heightM;

                // Периметр рами + створок (в метрах)
                double framePerimeter = (wM * 2.0) + (hM * 2.0);
                double sashPerimeter = (wM + hM) * 2.0 * config.SashCount;

                // Длина армування — считаем как общая длина профілів (приближённо)
                double totalProfileLength = framePerimeter + sashPerimeter;

                // Площадь стекла (м²)
                decimal glassArea = widthM * heightM;
                int glassMultiplier = ResolveGlassMultiplier(config.GlassType);

                decimal total = 0m;

                // 1) Профіль — цена за метр погонный
                if (priceProfile > 0)
                {
                    total += (decimal)totalProfileLength * priceProfile;
                }

                // 2) Склопакет — цена за м2 * количество стекол (множитель)
                if (priceGlass > 0)
                {
                    total += glassArea * glassMultiplier * priceGlass;
                }

                // 3) Фурнітура / ручки / петлі — штучно
                if (priceHardware > 0)
                {
                    total += config.SashCount * priceHardware;
                }

                if (priceHandle > 0 || priceHinge > 0)
                {
                    total += config.SashCount * (priceHandle + priceHinge);
                }

                // 4) Армування і ущільнювачі (за метр погонний)
                if (priceReinf > 0 || priceRubber > 0)
                {
                    total += (decimal)totalProfileLength * (priceReinf + priceRubber);
                }

                // 5) Підвіконня та відливи — по довжині вікна (ширина в метрах) * грн/м.п.
                if (priceSill > 0)
                {
                    total += widthM * priceSill;
                }

                if (priceDrain > 0)
                {
                    total += widthM * priceDrain;
                }

                // 6) Москітна сітка — враховується по площі; ціна не змінюється (як просили)
                if (config.HasMosquito)
                {
                    var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");
                    decimal priceMosq = (decimal)(mosquito?.Price ?? 0);
                    if (priceMosq > 0)
                    {
                        total += glassArea * priceMosq;
                    }
                }

                return Math.Round(total, 2);
            }
            catch (Exception ex)
            {
                // Лог в консоль для диагностики
                Console.WriteLine($"Помилка у CalculateWindowPrice: {ex.Message}");
                return 0m;
            }
        }

        public static int ResolveGlassMultiplier(string glassType)
        {
            if (string.IsNullOrWhiteSpace(glassType))
                return 2;

            if (glassType.Contains("2-камер", StringComparison.OrdinalIgnoreCase) ||
                glassType.Contains("2-камерний", StringComparison.OrdinalIgnoreCase)) return 3;

            if (glassType.Contains("Енергозбереж", StringComparison.OrdinalIgnoreCase) ||
                glassType.Contains("Енергозберігаючий", StringComparison.OrdinalIgnoreCase)) return 4;

            return 2;
        }
    }
}