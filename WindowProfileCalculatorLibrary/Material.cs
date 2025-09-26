namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Модель матеріалу в БД.
    /// </summary>
    public class Material
    {
        public int Id { get; set; }

        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public string Color { get; set; } = "";

        public double Price { get; set; } = 0;   // ціна
        public string Unit { get; set; } = "шт"; // одиниця виміру (шт/м/м²)

        public double Quantity { get; set; } = 0;     // НОВЕ: кількість на складі
        public string QuantityType { get; set; } = ""; // тип кількості (шт/м/м²)

        public string Article { get; set; } = "";   // НОВЕ: артикул/код
        public string Currency { get; set; } = "";  // НОВЕ: валюта (UAH/EUR…)

        public string Description { get; set; } = "";
    }
}
