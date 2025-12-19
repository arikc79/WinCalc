using System;

namespace WindowProfileCalculatorLibrary
{
    public class Calculation
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Хто робив розрахунок
        public string Username { get; set; } = string.Empty; // Для відображення
        public DateTime Date { get; set; } = DateTime.Now;
        public double TotalPrice { get; set; }

        // Деталі вікна
        public double Width { get; set; }
        public double Height { get; set; }
        public string WindowType { get; set; } = string.Empty;
    }
}