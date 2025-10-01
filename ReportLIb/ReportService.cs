using System.Globalization;
using System.IO;
using System.Text;

namespace WinCalc
{
    public static class ReportService
    {
        public static readonly string DirectoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinCalcReports");

        public static readonly string FilePath = Path.Combine(DirectoryPath, "reports.csv");

        private const string Header =
            "Date;User;WindowType;Brand;Profile;GlassPack;Width_m;Height_m;Length_m;PricePerMeter;Cost;Sill;Drain";

        public static void Append(ReportRow r)
        {
            Directory.CreateDirectory(DirectoryPath);

            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, Header + Environment.NewLine, Encoding.UTF8);

            var I = CultureInfo.InvariantCulture;
            string esc(string? s) => (s ?? "").Replace(";", "|");

            var line = string.Join(";",
                r.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                esc(r.User),
                esc(r.WindowType),
                esc(r.Brand),
                esc(r.Profile),
                esc(r.GlassPack),
                r.Width.ToString(I),
                r.Height.ToString(I),
                r.Length.ToString(I),
                r.PricePerMeter.ToString(I),
                r.Cost.ToString(I),
                r.Sill ? "1" : "0",
                r.Drain ? "1" : "0"
            );

            File.AppendAllText(FilePath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

   
}
