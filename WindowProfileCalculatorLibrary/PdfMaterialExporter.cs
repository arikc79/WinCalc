using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.IO;
using System.Windows.Media;

namespace WindowProfileCalculatorLibrary
{
    public static class PdfMaterialExporter
    {
        public static void ExportProjectReport(string filePath, ProjectReportData data, string? logoPath = null)
        {
            GlobalFontSettings.FontResolver = ReportService.SegoeFontResolver.Instance;

            var doc = new PdfDocument { Info = { Title = data.ProjectName } };
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // === Шрифти ===
            var titleFont = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
            var sectionFont = new XFont("Segoe UI", 13, XFontStyleEx.Bold);
            var labelFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            var textFont = new XFont("Segoe UI", 11, XFontStyleEx.Regular);
            var footerFont = new XFont("Segoe UI", 9, XFontStyleEx.Italic);

            double margin = 60;
            double y = margin;

            // === Логотип ===
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    using var logo = XImage.FromFile(logoPath);
                    double ratio = Math.Min(150.0 / logo.PixelWidth, 150.0 / logo.PixelHeight);
                    double logoWidth = logo.PixelWidth * ratio;
                    double logoHeight = logo.PixelHeight * ratio;
                    gfx.DrawImage(logo, margin, y, logoWidth, logoHeight);
                }
                catch { }
            }

            // === Заголовок ===
            double textStartX = margin + 180;
            gfx.DrawString("WinCalc — Розрахунок вартості вікна", titleFont, XBrushes.DarkSlateBlue,
                new XRect(textStartX, y + 20, page.Width - textStartX - margin, 40), XStringFormats.TopLeft);

            y += 70;
            // === Дата, номер, користувач ===

            // Директорія для збереження лічильника
            string reportsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinCalcReports");
            Directory.CreateDirectory(reportsDir);
            string counterFile = Path.Combine(reportsDir, "counter.txt");

            int reportNumber = 1;
            if (File.Exists(counterFile))
            {
                if (int.TryParse(File.ReadAllText(counterFile).Trim(), out int parsed))
                    reportNumber = parsed + 1;
            }
            File.WriteAllText(counterFile, reportNumber.ToString());

            string reportId = reportNumber.ToString("D6");
            string user = !string.IsNullOrWhiteSpace(data.User) ? data.User : "admin";

            // === Координати правого блоку ===
            double rightBlockWidth = 260; // ширина блоку тексту
            double rightBlockX = page.Width - margin - rightBlockWidth;
            double rightBlockY = y + 15; // вертикальний відступ від логотипу

            gfx.DrawString($"Розрахунок № {reportId}", textFont, XBrushes.Gray,
                new XRect(rightBlockX, rightBlockY, rightBlockWidth, 20), XStringFormats.TopRight);
            rightBlockY += 18;

            gfx.DrawString($"Дата створення: {data.CreatedAt:dd.MM.yyyy HH:mm}", textFont, XBrushes.Gray,
                new XRect(rightBlockX, rightBlockY, rightBlockWidth, 20), XStringFormats.TopRight);
            rightBlockY += 18;

            gfx.DrawString($"Користувач: {user}", textFont, XBrushes.Gray,
                new XRect(rightBlockX, rightBlockY, rightBlockWidth, 20), XStringFormats.TopRight);

            // зсуваємо Y нижче для наступних секцій
            y += 80;

            // горизонтальна розділова лінія під шапкою
            gfx.DrawLine(new XPen(XColors.LightGray, 1.3), margin, y, page.Width - margin, y);
            y += 25;



            // === Розділювач ===
            DrawSeparator(gfx, page, margin, ref y);

            // === Розділ 1 — Основні параметри ===
            gfx.DrawString("🧱 Основні параметри", sectionFont, XBrushes.DarkSlateBlue,
                new XRect(margin, y, page.Width - margin * 2, 25), XStringFormats.TopLeft);
            y += 28;

            DrawLine(gfx, labelFont, textFont, margin, ref y, "Бренд", data.Brand);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Кількість камер", data.ProfileThickness);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Розмір вікна", $"{data.Width} × {data.Height} мм");
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Тип вікна", data.WindowType);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Тип ручки", data.HandleType);

            DrawSeparator(gfx, page, margin, ref y);

            // === Розділ 2 — Матеріали ===
            gfx.DrawString("🪟 Матеріали", sectionFont, XBrushes.DarkSlateBlue,
                new XRect(margin, y, page.Width - margin * 2, 25), XStringFormats.TopLeft);
            y += 28;

            DrawLine(gfx, labelFont, textFont, margin, ref y, "Склопакет", data.GlassPack);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Колір", data.Color);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Підвіконник", data.Sill);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Відлив", data.Drain);
            DrawLine(gfx, labelFont, textFont, margin, ref y, "Москітна сітка", data.HasMosquito ? "Так" : "Ні");

            DrawSeparator(gfx, page, margin, ref y);

            // === Розділ 3 — Підсумок ===
            gfx.DrawString("💰 Підсумок", sectionFont, XBrushes.DarkSlateBlue,
                new XRect(margin, y, page.Width - margin * 2, 25), XStringFormats.TopLeft);
            y += 30;

            gfx.DrawRectangle(XBrushes.AliceBlue, margin - 5, y - 5, page.Width - margin * 2 + 10, 40);
            gfx.DrawString($"Загальна вартість: {data.TotalPriceUAH:0.00} грн / €{data.TotalPriceEUR:0.00}",
                labelFont, XBrushes.DarkBlue,
                new XRect(margin + 10, y + 8, page.Width - margin * 2, 20), XStringFormats.TopLeft);

            // === Футер ===
            gfx.DrawLine(XPens.LightGray, margin, page.Height - 60, page.Width - margin, page.Height - 60);
            gfx.DrawString($"WinCalc © {DateTime.Now.Year} | Автоматичний розрахунок віконних конструкцій",
                footerFont, XBrushes.Gray,
                new XRect(margin, page.Height - 50, page.Width - margin * 2, 20), XStringFormats.TopLeft);

            doc.Save(filePath);
            doc.Close();
        }

        private static void DrawLine(XGraphics gfx, XFont labelFont, XFont textFont, double margin, ref double y, string label, string value)
        {
            gfx.DrawString(label + ":", labelFont, XBrushes.Black,
                new XRect(margin, y, 180, 20), XStringFormats.TopLeft);
            gfx.DrawString(value, textFont, XBrushes.Black,
                new XRect(margin + 180, y, 300, 20), XStringFormats.TopLeft);
            y += 22;
        }

        private static void DrawSeparator(XGraphics gfx, PdfPage page, double margin, ref double y)
        {
            y += 10;
            gfx.DrawLine(new XPen(XColors.LightGray, 1.5), margin, y, page.Width - margin, y);
            y += 15;
        }
    }
}
