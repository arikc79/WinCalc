using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Клас для імпорту та експорту матеріалів у форматі CSV.
    /// </summary>
    public static class CsvMaterialImporter
    {
        // Заголовок CSV, який записується при експорті
        private static readonly string[] CsvHeader =
        {
            "Category", "Name", "Color", "Price", "Unit",  "Description"
        };

        // =====================================================================
        // ІМПОРТ МАТЕРІАЛІВ
        // =====================================================================
        /// <summary>
        /// Імпортує список матеріалів із CSV-файлу.
        /// Очікується заголовок:
        /// Category;Name;Color;Price;Unit;QuantityType;Description
        /// </summary>
        public static List<Material> Import(string filePath)
        {
            var materials = new List<Material>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"❌ Файл не знайдено: {filePath}");

            using var reader = new StreamReader(filePath);
            string? headerLine = reader.ReadLine();

            if (headerLine == null)
                throw new InvalidDataException("❌ CSV файл порожній або має пошкоджену структуру.");

            int lineNum = 1;
            while (!reader.EndOfStream)
            {
                lineNum++;
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(';', StringSplitOptions.TrimEntries);
                if (parts.Length < 6)
                {
                    Console.WriteLine($"⚠️ Пропущено рядок {lineNum}: неправильний формат CSV.");
                    continue;
                }

                // Безпечне зчитування ціни
                decimal priceValue = 0m;
                if (double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDouble))
                    priceValue = (decimal)parsedDouble;
                else if (decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.CurrentCulture, out var parsedDecimal))
                    priceValue = parsedDecimal;

                try
                {
                    var material = new Material
                    {
                        Category = parts[0],
                        Name = parts[1],
                        Color = parts[2],
                        Price = (double)priceValue,
                        Unit = parts[4],
                        Description = parts[5]
                    };

                    materials.Add(material);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Помилка у рядку {lineNum}: {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Імпортовано {materials.Count} матеріалів із CSV: {Path.GetFileName(filePath)}");
            return materials;
        }

        // =====================================================================
        // ЕКСПОРТ МАТЕРІАЛІВ
        // =====================================================================
        /// <summary>
        /// Експортує список матеріалів у CSV-файл.
        /// </summary>
        public static void Export(string filePath, List<Material> materials)
        {
            if (materials == null || materials.Count == 0)
                throw new ArgumentException("❌ Немає матеріалів для експорту.");

            using var writer = new StreamWriter(filePath, false);
            writer.WriteLine(string.Join(';', CsvHeader));

            foreach (var m in materials)
            {
                // Форматуємо ціну як інваріантну (через крапку)
                string priceStr = m.Price.ToString(CultureInfo.InvariantCulture);

                // Екрануємо спецсимволи
                string line = string.Join(';', new[]
                {
                    EscapeCsv(m.Category),
                    EscapeCsv(m.Name),
                    EscapeCsv(m.Color ?? ""),
                    priceStr,
                    EscapeCsv(m.Unit),                                 
                    EscapeCsv(m.Description ?? "")
                });

                writer.WriteLine(line);
            }

            writer.Flush();
            Console.WriteLine($"✅ Експортовано {materials.Count} матеріалів у файл: {Path.GetFileName(filePath)}");
        }

        // =====================================================================
        // ДОПОМІЖНІ МЕТОДИ
        // =====================================================================
        /// <summary>
        /// Екранує значення для CSV (якщо містить ; або лапки).
        /// </summary>
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(';') || value.Contains('"'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}
