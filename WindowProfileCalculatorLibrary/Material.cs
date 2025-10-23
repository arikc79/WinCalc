namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Модель одного матеріалу з бази даних.
    /// </summary>
    public class Material
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;   // Напр.: "Профіль", "Ручка", "Підвіконня"
        public string Name { get; set; } = string.Empty;       // Напр.: "Steko S500", "Преміум", "200 мм"
        public string? Color { get; set; }
        public double Price { get; set; }                      // Ціна
        public string Unit { get; set; } = string.Empty;       // Напр.: "м.п.", "м²", "шт."
      
        public string? Description { get; set; }
    }
}
