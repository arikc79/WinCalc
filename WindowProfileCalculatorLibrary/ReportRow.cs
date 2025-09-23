using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowProfileCalculatorLibrary
{
    public class ReportRow
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string? User { get; set; }
        public string? WindowType { get; set; }
        public string? Brand { get; set; }
        public string? Profile { get; set; }
        public string? GlassPack { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
        public double PricePerMeter { get; set; }
        public double Cost { get; set; }
        public bool Sill { get; set; }
        public bool Drain { get; set; }
    }
}
