using System;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Модель короткого звіту проекту для експорту у PDF.
    /// </summary>
    public class ProjectReportData
    {
        public string ProjectName { get; set; } = "Розрахунок вартості вікна";
        public string User { get; set; } = Environment.UserName;
        public string Profile { get; set; } = "";
        public string GlassPack { get; set; } = "";
        public string Color { get; set; } = "";
        public string Sill { get; set; } = "";
        public string Drain { get; set; } = "";
        public bool HasMosquito { get; set; }
        public double TotalPriceUAH { get; set; }
        public double TotalPriceEUR { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
