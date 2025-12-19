using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔹 Наповнення ComboBox типів вікон
                cmbWindowType.ItemsSource = new List<string>(_windowImageMap.Keys);
                cmbWindowType.SelectedIndex = 0;

                // 🔹 Отримуємо зіставлені назви профілів (повні назви з таблиці Profiles)
                var profileFullNames = _dataAccess.GetDistinctBrands(); // повертає Name з Profiles
                // Парсимо бренди (перше слово до пробілу)
                var brands = profileFullNames
                    .Select(n => (n ?? string.Empty).Split(' ')[0])
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                cmbBrand.ItemsSource = brands;
                if (brands.Count > 0)
                    cmbBrand.SelectedIndex = 0;

                // 🔹 Заповнимо товщини для поточного бренду
                if (cmbBrand.SelectedItem is string selBrand)
                    PopulateProfileThicknesses(selBrand);

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

        // Обработчик, добавленный для XAML
        private void cmbBrand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBrand.SelectedItem is string brand)
            {
                PopulateProfileThicknesses(brand);
            }
        }

        private void PopulateProfileThicknesses(string brand)
        {
            try
            {
                var profileFullNames = _dataAccess.GetDistinctBrands();
                // варіанти суфіксів для даного бренду
                var variants = profileFullNames
                    .Where(n => n != null && n.StartsWith(brand + " "))
                    .Select(n => n.Substring(brand.Length + 1).Trim()) // суфікс: "4-камерний", "6i" і т.д.
                    .Distinct()
                    .ToList();

                // Перетворюємо суфікс у число камер для відображення: 4,5,6,7
                var numbers = variants
                    .Select(v => VariantToNumber(v))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                // Якщо немає варіантів у спеціальних таблицях, fallback — стандартні
                if (numbers.Count == 0)
                {
                    numbers = new List<string> { "4", "5", "6", "7" };
                }

                cmbProfileThickness.ItemsSource = numbers;
                if (numbers.Count > 0)
                    cmbProfileThickness.SelectedIndex = 0;
            }
            catch
            {
                // не фатально
                cmbProfileThickness.ItemsSource = new List<string> { "4", "5", "6", "7" };
                cmbProfileThickness.SelectedIndex = 0;
            }
        }

        private static string VariantToNumber(string variant)
        {
            if (string.IsNullOrEmpty(variant)) return string.Empty;
            if (variant.Contains("4")) return "4";
            if (variant.Contains("5")) return "5";
            if (variant.Contains("6")) return "6";
            if (variant.Contains("7")) return "7";
            return string.Empty;
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
                LoadDefaultPreview();
            }
        }

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

            // Бренд (наприклад "WDS")
            string brandShort = cmbBrand.SelectedItem?.ToString() ?? cmbBrand.Text ?? string.Empty;

            // Число камер (4,5,6,7)
            string chambersNumber = cmbProfileThickness.SelectedItem?.ToString() ?? cmbProfileThickness.Text ?? string.Empty;

            // Перетворюємо назад у суфікс, що зберігається в базі
            string suffix = chambersNumber switch
            {
                "4" => "4-камерний",
                "5" => "5-камерний",
                "6" => "6i",
                "7" => "7i",
                _ => chambersNumber
            };

            // Повна назва профілю, яку будемо передавати в обчислення (йде в GetMaterialByCategory як filter)
            string profileFullName = string.IsNullOrWhiteSpace(brandShort) ? string.Empty : $"{brandShort} {suffix}";

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
                Brand = profileFullName,
                GlassType = glassType,
                HandleType = handlePremium ? "Преміум (Hoppe)" : "Стандартна",
                SillType = sill300 ? "300 мм" : "200 мм",
                DrainType = drain200 ? "300 мм" : "200 мм",
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
                if config == null)
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
        // ===============================================================
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
