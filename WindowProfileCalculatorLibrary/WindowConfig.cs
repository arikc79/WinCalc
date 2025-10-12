namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Модель конфігурації вікна для розрахунку вартості.
    /// Передається з MainWindow.xaml.cs у клас Obchyslennya.
    /// </summary>
    public class WindowConfig
    {
        // -------------------------------------------------------
        // Розміри
        // -------------------------------------------------------
        /// <summary>
        /// Ширина вікна, мм.
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// Висота вікна, мм.
        /// </summary>
        public decimal Height { get; set; }

        // -------------------------------------------------------
        // Конфігураційні параметри
        // -------------------------------------------------------

        /// <summary>
        /// Тип вікна (1-5 секцій, з відкриванням / без).
        /// </summary>
        public string WindowType { get; set; } = "Type1";

        /// <summary>
        /// Товщина профілю (наприклад: "60 мм", "70 мм", "82 мм").
        /// </summary>
        public string ProfileThickness { get; set; } = "70 мм";

        /// <summary>
        /// Назва бренду профілю (наприклад: Steko, Veka, Rehau).
        /// </summary>
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// Тип склопакету (наприклад: "1-камерний", "2-камерний").
        /// </summary>
        public string GlassType { get; set; } = string.Empty;

        /// <summary>
        /// Тип ручки ("Стандартна" або "Преміум").
        /// </summary>
        public string HandleType { get; set; } = string.Empty;

        /// <summary>
        /// Тип підвіконня ("200 мм" або "300 мм").
        /// </summary>
        public string SillType { get; set; } = string.Empty;

        /// <summary>
        /// Тип відливу ("150 мм" або "200 мм").
        /// </summary>
        public string DrainType { get; set; } = string.Empty;

        /// <summary>
        /// Чи є москітна сітка.
        /// </summary>
        public bool HasMosquito { get; set; }

        // -------------------------------------------------------
        // Параметри розрахунку
        // -------------------------------------------------------
        /// <summary>
        /// Ширина рами (мм).
        /// </summary>
        public decimal FrameWidth { get; set; } = 70;

        /// <summary>
        /// Ширина імпоста (мм).
        /// </summary>
        public decimal MidFrameWidth { get; set; } = 80;

        /// <summary>
        /// Перехлест стулки (мм).
        /// </summary>
        public decimal Overlap { get; set; } = 8;

        /// <summary>
        /// Допуск на зварювання (мм на кінець).
        /// </summary>
        public decimal WeldingAllowance { get; set; } = 2;
    }
}
