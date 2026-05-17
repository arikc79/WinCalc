using System;
using System.Diagnostics;
using System.IO;
using PdfSharp.Fonts;
namespace WindowProfileCalculatorLibrary
{
    public static class ReportService
    {
        public static readonly string ReportsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinCalcReports");



        // метод  Експорт PDF
        public static string ExportPdfReport(ProjectReportData data)
        {
            Directory.CreateDirectory(ReportsDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string filePath = Path.Combine(ReportsDirectory, $"project_report_{timestamp}.pdf");

            // === Абсолютний шлях до логотипу у теці виконання програми ===
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string logoPath = Path.Combine(exeDir, "Image", "logo.jpg");

            PdfMaterialExporter.ExportProjectReport(filePath, data, logoPath);

            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не вдалося автоматично відкрити PDF: {ex.Message}");
            }

            return filePath;
        }

    }
}
