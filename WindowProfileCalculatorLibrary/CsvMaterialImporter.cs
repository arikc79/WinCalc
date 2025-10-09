using System.Globalization;
using Microsoft.VisualBasic.FileIO;


namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Імпортер CSV з гнучким мапінгом заголовків (укр/англ/регістр/пробіли/коми/крапки з комою/таб).
    /// </summary>
    public static class CsvMaterialImporter
    {
        // Мапа можливих назв колонок → канонічна назва (властивість Material)
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["category"] = "Category",
            ["категорія"] = "Category",

            ["name"] = "Name",
            ["назва"] = "Name",

            ["color"] = "Color",
            ["колір"] = "Color",

            ["price"] = "Price",
            ["ціна"] = "Price",

            ["unit"] = "Unit",
            ["одиниця"] = "Unit",

            ["quantity"] = "Quantity",
            ["кількість"] = "Quantity",

            ["quantitytype"] = "QuantityType",
            ["типкількості"] = "QuantityType",

            ["article"] = "Article",
            ["артикул"] = "Article",

            ["currency"] = "Currency",
            ["валюта"] = "Currency",

            ["description"] = "Description",
            ["опис"] = "Description",
        };

        public static List<Material> Import(string path)
        {
            var result = new List<Material>();

            using var parser = new TextFieldParser(path);
            parser.SetDelimiters(new[] { ",", ";", "\t" });
            parser.HasFieldsEnclosedInQuotes = true;

            if (parser.EndOfData) return result;

            var header = Normalize(parser.ReadFields());
            var idx = BuildIndex(header);

            while (!parser.EndOfData)
            {
                var raw = parser.ReadFields();
                if (raw == null || raw.Length == 0) continue;
                var row = SafeRow(header, raw);

                var m = new Material
                {
                    Category = Get(row, idx, "Category"),
                    Name = Get(row, idx, "Name"),
                    Color = Get(row, idx, "Color"),
                    Unit = Get(row, idx, "Unit"),
                    QuantityType = Get(row, idx, "QuantityType"),
                    Article = Get(row, idx, "Article"),
                    Currency = Get(row, idx, "Currency"),
                    Description = Get(row, idx, "Description"),

                    Price = ToDouble(Get(row, idx, "Price")),
                    Quantity = ToDouble(Get(row, idx, "Quantity")),
                };

                // якщо порожній  пропускаємо
                if (string.IsNullOrWhiteSpace(m.Name) && string.IsNullOrWhiteSpace(m.Article))
                    continue;

                result.Add(m);
            }

            return result;
        }

        // ---- helpers ----

        private static string[] Normalize(string[]? fields)
        {
            fields ??= Array.Empty<string>();
            var norm = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                norm[i] = (fields[i] ?? "").Trim();
            return norm;
        }

        private static Dictionary<string, int> BuildIndex(string[] header)
        {
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < header.Length; i++)
            {
                var key = (header[i] ?? "")
                    .Replace(" ", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("_", "", StringComparison.OrdinalIgnoreCase)
                    .ToLowerInvariant();

                if (Map.TryGetValue(key, out var canon))
                    if (!idx.ContainsKey(canon)) idx[canon] = i;
            }

            return idx;
        }

        private static string[] SafeRow(string[] header, string[] raw)
        {
            if (raw.Length >= header.Length) return raw;

            var tmp = new string[header.Length];
            Array.Copy(raw, tmp, raw.Length);
            for (int i = raw.Length; i < header.Length; i++) tmp[i] = "";
            return tmp;
        }

        private static string Get(string[] row, Dictionary<string, int> idx, string col)
            => idx.TryGetValue(col, out var i) ? (row[i] ?? "").Trim() : "";

        private static double ToDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            var repl = s.Replace(',', '.');
            if (double.TryParse(repl, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                return d;

            return 0;
        }
    }
}
