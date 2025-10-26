namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Конфігурація параметрів вікна для розрахунку.
    /// </summary>
    public class WindowConfig
    {
        public decimal Width { get; set; }       // мм
        public decimal Height { get; set; }      // мм
        public string Brand { get; set; } = "";
        public string GlassType { get; set; } = "";
        public string HandleType { get; set; } = "";
        public string SillType { get; set; } = "";
        public string DrainType { get; set; } = "";
        public bool HasMosquito { get; set; }
    }
}
