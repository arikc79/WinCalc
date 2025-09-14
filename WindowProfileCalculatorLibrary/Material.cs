namespace WindowProfileCalculatorLibrary
{
    public class Material
    {
        public int Id { get; set; }
        public string Category { get; set; } // "профіль", "скло", "фурнітура" тощо
        public string Name { get; set; } // Наприклад, "Єврокоробка 5-камерний профіль"
        public string Color { get; set; } // Колір, якщо застосовно
        public double Price { get; set; } // Ціна за одиницю
        public string Unit { get; set; } // Одиниця виміру: "м.пог.", "м²", "шт"
        public string QuantityType { get; set; } // Тип кількості: "довжина", "площа", "шт"
        public string Description { get; set; } // Опис
    }
}