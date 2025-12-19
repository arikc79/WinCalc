namespace WindowProfileCalculatorLibrary
{
    public class Material
    {
        public int Id { get; set; }

        // Зв'язок з базою (Foreign Key)
        public int CategoryId { get; set; }

        // Це поле заповнюється через JOIN (для відображення в таблиці XAML)
        public string Category { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public double Price { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}