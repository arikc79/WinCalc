using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.IO;

namespace WindowProfileCalculatorLibrary
{
    public static class PdfMaterialExporter
    {
       
        public static void ExportProjectReport(string filePath, ProjectReportData data, string? logoPath = null)
        {
            // === Реєстрація шрифту ===
            GlobalFontSettings.FontResolver = SegoeFontResolver.Instance;

            var doc = new PdfDocument { Info = { Title = data.ProjectName } };
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // === Шрифти ===
            var titleFont = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
            var labelFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            var textFont = new XFont("Segoe UI", 11, XFontStyleEx.Regular);
            var footerFont = new XFont("Segoe UI", 9, XFontStyleEx.Italic);

            double margin = 60;
            double y = margin;

            // === Логотип ===
            bool logoFound = false;
            double logoWidth = 0;
            double logoHeight = 0;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    string tempLogo = Path.Combine(Path.GetTempPath(), "win_calc_logo.jpg");
                    File.Copy(logoPath, tempLogo, true);

                    using (var logo = XImage.FromFile(tempLogo))
                    {
                        double maxWidth = 200;
                        double maxHeight = 200;
                        double ratio = Math.Min(maxWidth / logo.PixelWidth, maxHeight / logo.PixelHeight);
                        logoWidth = logo.PixelWidth * ratio;
                        logoHeight = logo.PixelHeight * ratio;

                        gfx.DrawImage(logo, margin, y, logoWidth, logoHeight);
                        logoFound = true;
                    }
                }
                catch (Exception ex)
                {
                    gfx.DrawString($"⚠️ Помилка логотипу: {ex.Message}", textFont, XBrushes.Red,
                        new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft);
                }
            }

            if (!logoFound)
            {
                gfx.DrawString($"⚠️ Логотип не знайдено: {logoPath}", textFont, XBrushes.Red,
                    new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft);
            }

            // === Координати тексту праворуч від логотипа ===
            double textStartX = margin + logoWidth + 30;
            double textY = y + 10;

            // === Заголовок ===
            gfx.DrawString(data.ProjectName, titleFont, XBrushes.DarkBlue,
                new XRect(textStartX, textY, page.Width - textStartX - margin, 40), XStringFormats.TopLeft);
            textY += 40;

            // === Дата ===
            gfx.DrawString($"Дата створення: {data.CreatedAt:dd.MM.yyyy  HH:mm}", textFont, XBrushes.Gray,
                new XRect(textStartX, textY, page.Width - textStartX - margin, 20), XStringFormats.TopLeft);
            textY += 22;

            // === Лічильник розрахунків ===
            string reportsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinCalcReports");
            Directory.CreateDirectory(reportsDir);
            string counterFile = Path.Combine(reportsDir, "counter.txt");

            int reportNumber = 1;
            try
            {
                if (File.Exists(counterFile))
                {
                    string? content = File.ReadAllText(counterFile).Trim();
                    if (int.TryParse(content, out int parsed))
                        reportNumber = parsed + 1;
                }
                File.WriteAllText(counterFile, reportNumber.ToString());
            }
            catch
            {
                // ігноруємо помилки з файлом
            }

            string reportId = reportNumber.ToString("D6");

            // 🧠 нове — беремо користувача з data.User (а не з Windows)
            string user = !string.IsNullOrWhiteSpace(data.User) ? data.User : "admin";

            gfx.DrawString($"Розрахунок № {reportId}", textFont, XBrushes.Gray,
                new XRect(textStartX, textY, page.Width - textStartX - margin, 20), XStringFormats.TopLeft);
            textY += 22;

            gfx.DrawString($"Користувач: {user}", textFont, XBrushes.Gray,
                new XRect(textStartX, textY, page.Width - textStartX - margin, 20), XStringFormats.TopLeft);
            textY += 25;

            // === Інформаційні поля ===
            void DrawLine(string label, string value)
            {
                gfx.DrawString(label + ":", labelFont, XBrushes.Black,
                    new XRect(textStartX, textY, 160, 20), XStringFormats.TopLeft);
                gfx.DrawString(value, textFont, XBrushes.Black,
                    new XRect(textStartX + 160, textY, page.Width - textStartX - margin, 20), XStringFormats.TopLeft);
                textY += 22;
            }

            DrawLine("Бренд профілю", data.Profile);
            DrawLine("Склопакет", data.GlassPack);
            DrawLine("Колір", data.Color);
            DrawLine("Підвіконник", data.Sill);
            DrawLine("Відлив", data.Drain);
            DrawLine("Москітна сітка", data.HasMosquito ? "Так" : "Ні");

            // === Розділювальна лінія ===
            textY += 20;
            gfx.DrawLine(XPens.Gray, margin, textY, page.Width - margin, textY);
            textY += 25;

            // === Ціна ===
            gfx.DrawRectangle(XBrushes.AliceBlue, margin - 5, textY - 5, page.Width - margin * 2 + 10, 40);
            gfx.DrawString($"Загальна вартість: {data.TotalPriceUAH:0.00} грн  /  €{data.TotalPriceEUR:0.00}",
                labelFont, XBrushes.DarkBlue,
                new XRect(margin, textY + 5, page.Width - margin * 2, 20), XStringFormats.TopLeft);

            // === Футер ===
            gfx.DrawLine(XPens.LightGray, margin, page.Height - 60, page.Width - margin, page.Height - 60);
            gfx.DrawString($"WinCalc © {DateTime.Now.Year}  |  Автоматичний розрахунок віконних конструкцій",
                footerFont, XBrushes.Gray,
                new XRect(margin, page.Height - 50, page.Width - margin * 2, 20), XStringFormats.TopLeft);

            doc.Save(filePath);
            doc.Close();
        }
    }




}