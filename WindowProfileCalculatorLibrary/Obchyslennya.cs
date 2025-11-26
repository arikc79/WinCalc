using System;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Головний клас логіки обчислень.
    /// </summary>
    public class Obchyslennya
    {
        // ✅ ТЕПЕР ШЛЯХ ІДЕ ЧЕРЕЗ DbConfig
        // Але оскільки DataAccess теж використовує DbConfig, тут можна просто ініціалізувати DataAccess
        private readonly DataAccess _dataAccess = new();

        // Старий метод CreateTables можна видалити або перенаправити на DataInitializer,
        // оскільки ми тепер використовуємо DataInitializer.InsertInitialData()
        public static void CreateTables()
        {
            DataInitializer.InsertInitialData();
        }

        // =====================================================================
        // ЛОГІКА РОЗРАХУНКУ (Без змін, окрім того, що DB шлях тепер правильний у DataAccess)
        // =====================================================================

        public decimal CalculateWindowPrice(WindowConfig config)
        {
            // 1. Отримуємо ціни матеріалів з БД
            var profile = _dataAccess.GetMaterialByCategory("Профіль", config.Brand);
            var glass = _dataAccess.GetMaterialByCategory("Склопакет", config.GlassType);
            var hardware = _dataAccess.GetMaterialByCategory("Фурнітура", "Vorne"); // або логіка вибору фурнітури
            var reinforcement = _dataAccess.GetMaterialByCategory("Армування", "П-подібне");
            var rubber = _dataAccess.GetMaterialByCategory("Ущільнювач", "Гумовий");
            var screw = _dataAccess.GetMaterialByCategory("Шуруп", "Саморіз");

            // Перевірка на null і підстановка дефолтних цін (0), якщо не знайдено
            decimal priceProfile = (decimal)(profile?.Price ?? 0);
            decimal priceGlass = (decimal)(glass?.Price ?? 0);
            decimal priceHardware = (decimal)(hardware?.Price ?? 0);
            decimal priceReinf = (decimal)(reinforcement?.Price ?? 0);
            decimal priceRubber = (decimal)(rubber?.Price ?? 0);
            decimal priceScrew = (decimal)(screw?.Price ?? 0);

            // Переводимо розміри в метри
            double wM = (double)config.Width / 1000.0;
            double hM = (double)config.Height / 1000.0;

            // Розрахунок довжини профілю (спрощений, для прикладу)
            // Периметр рами + стулки
            double profileLength = (wM * 2 + hM * 2) + (config.SashCount * (wM + hM));

            // Площа скла
            double glassArea = wM * hM;

            // Рахуємо суму
            decimal total = 0;
            total += (decimal)profileLength * priceProfile;
            total += (decimal)glassArea * priceGlass;
            total += config.SashCount * priceHardware;
            total += (decimal)profileLength * priceReinf; // Армування йде всередину профілю
            total += (decimal)profileLength * 2 * priceRubber; // Ущільнювач з двох сторін

            // Додаткові елементи
            if (config.HasMosquito)
            {
                var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка");
                total += (decimal)glassArea * (decimal)(mosquito?.Price ?? 0);
            }

            return total;
        }

        public static int ResolveGlassMultiplier(string glassType)
        {
            if (glassType.Contains("1-камерний")) return 2; // 2 скла
            if (glassType.Contains("2-камерний")) return 3; // 3 скла
            return 2;
        }

        // ... (Методи розрахунку геометрії можна залишити як є)
    }
}