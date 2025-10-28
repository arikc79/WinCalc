using Markdig.Wpf;
using System.IO;
using System.Windows;

namespace WinCalc
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            LoadMarkdown();
        }

        private void LoadMarkdown()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "help_ua.md");
                if (!File.Exists(path))
                {
                    DocViewer.Document = Markdown.ToFlowDocument("# Помилка\nФайл довідки не знайдено.");
                    return;
                }

                string markdown = File.ReadAllText(path);
                var doc = Markdown.ToFlowDocument(markdown);
                doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                doc.FontSize = 14;

                DocViewer.Document = doc;
            }
            catch (Exception ex)
            {
                DocViewer.Document = Markdown.ToFlowDocument($"# Помилка\n{ex.Message}");
            }
        }
    }
}
