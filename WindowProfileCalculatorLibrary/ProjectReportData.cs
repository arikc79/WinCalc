using System;

namespace WindowProfileCalculatorLibrary
{
    public class ProjectReportData
    {
        public string ProjectName { get; set; } = "Розрахунок вартості вікна";
        public string User { get; set; } = Environment.UserName;

        //  Основні характеристики
        public string Brand { get; set; } = "";                 // Наприклад: Rehau
        public string ProfileThickness { get; set; } = "";      // Кількість камер (з ComboBox)
        public double Width { get; set; }                       // Ширина
        public double Height { get; set; }                      // Висота
        public string WindowType { get; set; } = "";            // Тип вікна (з ComboBox)
        public string HandleType { get; set; } = "";            // Стандартна / Преміум

        //  Матеріали
        public string GlassPack { get; set; } = "";             // Тип склопакету
        public string Color { get; set; } = "";                 // Колір
        public string Sill { get; set; } = "";                  // Підвіконня
        public string Drain { get; set; } = "";                 // Відлив
        public bool HasMosquito { get; set; }                   // Москітна сітка

        //  Підсумкові дані
        public double TotalPriceUAH { get; set; }
        public double TotalPriceEUR { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
