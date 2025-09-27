namespace WindowProfileCalculatorLibrary
{
    // додано Quantity, Article, Currency (для складу та ідентифікації)
    public class Material
    {
        public int Id { get; set; }

        public string Category { get; set; }      // "профіль", "скло", "фурнітура" тощо
        public string Name { get; set; }          // Наприклад, "Єврокоробка 5-камерний профіль"
        public string Color { get; set; }         // Колір, якщо застосовно

        public double Price { get; set; }         // Ціна за одиницю
        public string Unit { get; set; }          // "м.пог.", "м²", "шт"

        public double Quantity { get; set; }      // Кількість на складі (в одиницях Unit)
        public string QuantityType { get; set; }  // "довжина", "площа", "шт"

        public string Article { get; set; }       // Артикул (ключ для upsert)
        public string Currency { get; set; }      // Валюта (грн, EUR, ...)

        public string Description { get; set; }   // Опис
    }
}
