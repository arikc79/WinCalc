using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace WindowProfileCalculatorLibrary
{
    public static class PdfMaterialExporter
    {
        public static void Export(string filePath, List<Material> materials, string? logoPath = null)
        {
            if (materials == null || materials.Count == 0)
                throw new ArgumentException("❌ Немає матеріалів для експорту.");

            var document = new PdfDocument
            {
                Info = { Title = "Каталог матеріалів" }
            };

            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // === Шрифти ===
            var titleFont = new XFont("Segoe UI", 16, XFontStyleEx.Bold);
            var headerFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var textFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var italicFont = new XFont("Segoe UI", 8, XFontStyleEx.Italic);

            double margin = 40;
            double y = margin;

            // === Логотип ===
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                var logo = XImage.FromFile(logoPath);
                gfx.DrawImage(logo, margin, y, 80, 40);
            }

            // === Заголовок ===
            gfx.DrawString("Каталог матеріалів", titleFont, XBrushes.DarkBlue,
                new XRect(0, y, page.Width, 60), XStringFormats.TopCenter);
            y += 60;

            // === Параметри таблиці ===
            string[] headers = { "Категорія", "Назва", "Колір", "Ціна (грн)", "Одиниця", "Опис" };
            double[] colWidths = { 80, 120, 70, 60, 70, 150 };
            double tableStartX = margin;

            DrawTableHeader(gfx, headers, colWidths, tableStartX, ref y, headerFont, null);

            bool alternate = false;
            foreach (var m in materials)
            {
                if (y > page.Height - 80)
                {
                    // нова сторінка
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = margin;
                    DrawTableHeader(gfx, headers, colWidths, tableStartX, ref y, headerFont, null);
                }

                DrawTableRow(
                    gfx,
                    new[]
                    {
                        m.Category,
                        m.Name,
                        m.Color ?? "",
                        m.Price.ToString("0.00"),
                        m.Unit,
                        m.Description ?? ""
                    },
                    colWidths,
                    tableStartX,
                    ref y,
                    textFont,
                    alternate ? XBrushes.WhiteSmoke : XBrushes.White
                );
                alternate = !alternate;
            }

            // === Нижній колонтитул ===
            y = page.Height - 40;
            gfx.DrawLine(XPens.Gray, margin, y, page.Width - margin, y);
            gfx.DrawString($"Експортовано: {DateTime.Now:dd.MM.yyyy HH:mm}",
                italicFont, XBrushes.Gray,
                new XRect(margin, y + 5, page.Width - margin * 2, 20),
                XStringFormats.TopLeft);

            // === Зберегти ===
            document.Save(filePath);
            document.Close();

            Console.WriteLine($"✅ PDF успішно створено: {Path.GetFileName(filePath)}");

            try { Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }); }
            catch { /* ігноруємо */ }
        }

        // =====================================================================
        // === Таблиця ===
        // =====================================================================
        private static void DrawTableHeader(
            XGraphics gfx,
            string[] headers,
            double[] widths,
            double startX,
            ref double y,
            XFont font,
            PdfPage _)
        {
            double x = startX;
            double height = 25;

            gfx.DrawRectangle(XBrushes.LightGray, startX - 2, y - 2, Sum(widths) + 4, height + 4);
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawString(headers[i], font, XBrushes.Black,
                    new XRect(x + 3, y + 5, widths[i], height), XStringFormats.TopLeft);
                x += widths[i];
            }

            y += height;
        }

        private static void DrawTableRow(
            XGraphics gfx,
            string[] values,
            double[] widths,
            double startX,
            ref double y,
            XFont font,
            XBrush background)
        {
            double x = startX;
            double rowHeight = 20;

            gfx.DrawRectangle(background, startX - 2, y, Sum(widths) + 4, rowHeight);

            for (int i = 0; i < values.Length; i++)
            {
                string text = values[i] ?? "";
                var rect = new XRect(x + 3, y + 3, widths[i], rowHeight);
                gfx.DrawString(WrapText(gfx, text, font, widths[i]), font, XBrushes.Black, rect, XStringFormats.TopLeft);
                x += widths[i];
            }

            y += rowHeight;
        }

        // =====================================================================
        // === Утиліти ===
        // =====================================================================
        private static double Sum(double[] arr)
        {
            double total = 0;
            foreach (double d in arr)
                total += d;
            return total;
        }

        private static string WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var words = text.Split(' ');
            string line = "";
            string result = "";

            foreach (var word in words)
            {
                string testLine = (line.Length == 0 ? word : line + " " + word);
                if (gfx.MeasureString(testLine, font).Width > maxWidth)
                {
                    result += line + "\n";
                    line = word;
                }
                else
                {
                    line = testLine;
                }
            }

            result += line;
            return result.Trim();
        }
    }
}
