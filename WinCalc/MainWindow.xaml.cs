using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WinCalc.Security;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private readonly DataAccess _dataAccess = new DataAccess();
        private readonly Obchyslennya _calculator = new Obchyslennya();

        // 🗂️ Мапінг назв вікон → файлів у /Image/
        private readonly Dictionary<string, string> _windowImageMap = new()
        {
            ["Одностулкове вікно"] = "1.png",
            ["Двостулкове вікно"] = "2.png",
            ["Трипільне вікно"] = "3.png",
            ["Балконний блок"] = "4.png",
            ["Індивідуальне"] = "5.png"
        };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        // ===============================================================
        //  ІНІЦІАЛІЗАЦІЯ ПРИ ЗАПУСКУ
        // ===============================================================
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔹 Наповнення ComboBox типів вікон
                cmbWindowType.ItemsSource = new List<string>(_windowImageMap.Keys);
                cmbWindowType.SelectedIndex = 0;

                // 🔹 Наповнення брендів профілів із БД
                var brands = _dataAccess.GetDistinctBrands();
                cmbBrand.ItemsSource = brands;
                if (brands.Count > 0)
                    cmbBrand.SelectedIndex = 0;

                // 🔹 Наповнення товщин профілю
                var thicknesses = _dataAccess.GetDistinctProfileThicknesses();
                cmbProfileThickness.ItemsSource = thicknesses;
                if (thicknesses.Count > 0)
                    cmbProfileThickness.SelectedIndex = 0;

                // 🔹 Наповнення склопакетів
                cmbGlassPack.ItemsSource = new List<string> { "1-камерний", "2-камерний", "Енергозберігаючий" };
                cmbGlassPack.SelectedIndex = 0;

                // 🔹 Початкове зображення
                LoadWindowPreview("Одностулкове вікно");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при ініціалізації: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // ===============================================================
        //  ТИП ВІКНА → ЗАВАНТАЖЕННЯ КАРТИНКИ
        // ===============================================================
        private void cmbWindowType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbWindowType.SelectedItem is string selectedType && _windowImageMap.ContainsKey(selectedType))
            {
                LoadWindowPreview(selectedType);
            }
            else
            {
                // fallback — якщо користувач вибрав невідомий тип
                LoadDefaultPreview();
            }
        }


        // ===============================================================
        //  ЗАВАНТАЖЕННЯ ЗОБРАЖЕННЯ З ПАПКИ Image
        // ===============================================================
        private void LoadWindowPreview(string windowType)
        {
            try
            {
                string imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image");
                string fileName = _windowImageMap.ContainsKey(windowType) ? _windowImageMap[windowType] : "default.png";
                string imagePath = Path.Combine(imgDir, fileName);

                if (!File.Exists(imagePath))
                    imagePath = Path.Combine(imgDir, "default.png");

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgPreview.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні зображення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // ===============================================================
        // Завантаження зображення за замовчуванням
        // ===============================================================
        private void LoadDefaultPreview()
        {
            try
            {
                string imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image");
                string imagePath = Path.Combine(imgDir, "default.png");

                if (File.Exists(imagePath))
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                    imgPreview.Source = bitmap;
                }
            }
            catch { /* нічого не робимо */ }
        }



        // ===============================================================
        // 🧩 ЗБІР КОНФІГУРАЦІЇ ВІКНА
        // ===============================================================
        private WindowConfig? BuildWindowConfig(out string? errorMessage)
        {
            errorMessage = null;

            if (!decimal.TryParse(txtWidth.Text, out decimal width) ||
                !decimal.TryParse(txtHeight.Text, out decimal height))
            {
                errorMessage = "Будь ласка, введіть коректні розміри.";
                return null;
            }

            string windowType = cmbWindowType.SelectedItem?.ToString() ?? "Одностулкове вікно";
            string brand = string.IsNullOrWhiteSpace(cmbBrand.Text) ? "" : cmbBrand.Text;
            string glassType = string.IsNullOrWhiteSpace(cmbGlassPack.Text) ? "" : cmbGlassPack.Text;

            int sashCount = 1;
            if (cmbSashCount.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content?.ToString(), out int parsedSash))
            {
                sashCount = parsedSash;
            }

            bool sill300 = rbSill300.IsChecked == true;
            bool drain200 = rbDrain200.IsChecked == true;
            bool handlePremium = rbHandlePremium.IsChecked == true;
            bool hasMosquito = chkMosquito.IsChecked == true;

            return new WindowConfig
            {
                Width = width,
                Height = height,
                WindowType = windowType,
                SashCount = sashCount,
                Brand = brand,
                GlassType = glassType,
                HandleType = handlePremium ? "Преміум" : "Стандарт",
                SillType = sill300 ? "Білий 300мм" : "Білий 200мм",
                DrainType = drain200 ? "Білий 200мм" : "Білий 150мм",
                HasMosquito = hasMosquito
            };
        }


        // ===============================================================
        // 🧮 РОЗРАХУНОК ВАРТОСТІ ВІКНА
        // ===============================================================
        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = BuildWindowConfig(out string? errorMessage);
                if (config == null)
                {
                    MessageBox.Show(errorMessage ?? "Будь ласка, введіть коректні дані.", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal total = _calculator.CalculateWindowPrice(config);

                var handle = _dataAccess.GetMaterialByCategory("Ручка", config.HandleType) ?? new Material { Price = 0 };
                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", config.SillType) ?? new Material { Price = 0 };
                var drain = _dataAccess.GetMaterialByCategory("Відлив", config.DrainType) ?? new Material { Price = 0 };
                var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");

                int glassMultiplier = Obchyslennya.ResolveGlassMultiplier(config.GlassType);
                decimal glassArea = (config.Width / 1000m) * (config.Height / 1000m);
                decimal mosquitoCost = (config.HasMosquito && mosquito != null)
                    ? glassArea * (decimal)mosquito.Price
                    : 0m;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine(
                    $"Тип вікна: {config.WindowType}, " +
                    $"Бренд профілю: {config.Brand}, " +
                    $"Склопакет: {config.GlassType} ({glassMultiplier}-шар.)");
                sb.AppendLine();

                sb.Append("Ручки: ").Append($"{config.SashCount} × {handle.Price:F2} грн, ");
                sb.Append("Підвіконня: ").Append($"{sill.Price:F2} грн/м, ");
                sb.Append("Відлив: ").Append($"{drain.Price:F2} грн/м");
                if (config.HasMosquito && mosquito != null)
                {
                    sb.Append($", Москітна сітка: {mosquitoCost:F2} грн");
                }
                sb.AppendLine();

                sb.AppendLine($"ЗАГАЛЬНА ВАРТІСТЬ: {total:F2} грн");

                lblResult.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка під час розрахунку: {ex.Message}",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // ===============================================================
        // 🛑 Валідація  ПОЛЯ РОЗМІРІВ
        private static readonly Regex _numericRegex = new Regex("^[0-9]+$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_numericRegex.IsMatch(e.Text);

            if (e.Handled && sender is TextBox tb)
            {
                tb.ToolTip = "❌ Дозволено вводити лише цілі числа";
                tb.ToolTipOpening += (_, _) =>
                {
                    tb.ToolTip = "❌ Дозволено вводити лише цілі числа";
                };
            }
        }



        // ===============================================================
        // 🛠️ Керування матеріалами
        // ===============================================================
        private void btnMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin))
            {
                MessageBox.Show("Доступ дозволено лише адміністратору.", "Доступ заборонено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new MaterialsWindow();
            win.ShowDialog();
        }


        // ===============================================================
        // 👥 Керування користувачами
        // ===============================================================
        private void btnManageUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin))
            {
                MessageBox.Show("Доступ дозволено лише адміністратору.", "Доступ заборонено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new UserManagementWindow();
            win.ShowDialog();




        }


        // ===============================================================
        // 📤 Експорт у PDF
        // ===============================================================
        private void btnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = BuildWindowConfig(out string? errorMessage);
                if (config == null)
                {
                    MessageBox.Show(errorMessage ?? "Будь ласка, введіть розміри перед експортом!", "Увага",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal totalUAH = _calculator.CalculateWindowPrice(config);
                decimal eurRate = 41.5m;
                decimal totalEUR = Math.Round(totalUAH / eurRate, 2);

                string sillDisplay = config.SillType.Contains("300") ? "300 мм" : "200 мм";
                string drainDisplay = config.DrainType.Contains("200") ? "200 мм" : "150 мм";

                var data = new ProjectReportData
                {
                    ProjectName = "Розрахунок вартості вікна",
                    User = AppSession.CurrentUser?.Username ?? "admin",
                    Brand = config.Brand,
                    ProfileThickness = cmbProfileThickness.Text,
                    WindowType = cmbWindowType.Text,
                    Width = double.TryParse(txtWidth.Text, out double w) ? w : 0,
                    Height = double.TryParse(txtHeight.Text, out double h) ? h : 0,
                    HandleType = rbHandlePremium.IsChecked == true ? "Преміум" : "Стандарт",
                    GlassPack = config.GlassType,
                    Color = "Білий",
                    HasMosquito = config.HasMosquito,
                    Sill = sillDisplay,
                    Drain = drainDisplay,
                    TotalPriceUAH = (double)totalUAH,
                    TotalPriceEUR = (double)totalEUR
                };

                string pathToFile = ReportService.ExportPdfReport(data);
                MessageBox.Show($"✅ Звіт створено:\n{pathToFile}", "Експорт PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Помилка експорту: {ex.Message}",
                    "WinCalc", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===============================================================
        //  Вихід
        // ===============================================================
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void chkMosquito_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
