using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WinCalc.Security;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private readonly DataAccess _dataAccess = new DataAccess();

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
        //  ЗМІНА ТИПУ ВІКНА → ЗАВАНТАЖЕННЯ КАРТИНКИ
        // ===============================================================
        private void cmbWindowType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbWindowType.SelectedItem is string selectedType && _windowImageMap.ContainsKey(selectedType))
            {
                LoadWindowPreview(selectedType);
            }
            else
            {
                // 🔸 fallback — якщо користувач вибрав невідомий тип
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
        //  КНОПКА РОЗРАХУНКУ
        // ===============================================================
        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(txtWidth.Text, out double width) ||
                    !double.TryParse(txtHeight.Text, out double height))
                {
                    MessageBox.Show("Будь ласка, введіть коректні розміри.", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string windowType = cmbWindowType.SelectedItem?.ToString() ?? "Одностулкове вікно";
                string brand = cmbBrand.SelectedItem?.ToString() ?? "";
                string glassType = cmbGlassPack.SelectedItem?.ToString() ?? "";
                bool sill300 = rbSill300.IsChecked == true;
                bool drain200 = rbDrain200.IsChecked == true;
                bool handlePremium = rbHandlePremium.IsChecked == true;
                bool hasMosquito = chkMosquito.IsChecked == true;

                int sashCount = int.Parse(((ComboBoxItem)cmbSashCount.SelectedItem).Content.ToString()!);

                // ==============================================================
                // 1️⃣ Визначення кількості імпостів по типу вікна
                // ==============================================================
                int impostCount = windowType switch
                {
                    string s when s.Contains("Одностулкове") => 0,
                    string s when s.Contains("Двостулкове") => 1,
                    string s when s.Contains("Трипільне") => 2,
                    string s when s.Contains("Балконний") => 3,
                    _ => 4
                };

                const double frameWidth = 60; // мм
                double framePerimeter = 2 * (width + height - 2 * frameWidth);
                double sashPerimeter = 2 * ((width / (impostCount + 1)) + height - 2 * frameWidth);
                double impostLength = impostCount * (height - 2 * frameWidth);
                double glassArea = (width / 1000.0) * (height / 1000.0);

                // ==============================================================
                // 2️⃣ Завантаження матеріалів
                // ==============================================================
                var profile = _dataAccess.GetMaterialByCategoryAndBrand("Профіль", brand);
                var glass = _dataAccess.GetMaterialByCategory("Склопакет", glassType);
                var arm = _dataAccess.GetMaterialByCategory("Армування", "Стандарт");
                var seal = _dataAccess.GetMaterialByCategory("Ущільнювач скла", "Стандарт");
                var handle = _dataAccess.GetMaterialByCategory("Ручка", handlePremium ? "Преміум" : "Стандарт");
                var hinge = _dataAccess.GetMaterialByCategory("Петлі комплект", "Стандарт");
                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", sill300 ? "Білий 300мм" : "Білий 200мм");
                var drain = _dataAccess.GetMaterialByCategory("Відлив", drain200 ? "Білий 200мм" : "Білий 150мм");

                profile ??= new Material { Price = 0 };
                glass ??= new Material { Price = 0 };
                arm ??= new Material { Price = 0 };
                seal ??= new Material { Price = 0 };
                handle ??= new Material { Price = 0 };
                hinge ??= new Material { Price = 0 };
                sill ??= new Material { Price = 0 };
                drain ??= new Material { Price = 0 };


                // ==============================================================
                // 3️⃣ Коефіцієнт скла (множник)
                // ==============================================================
                int glassMultiplier = glassType.Contains("2") ? 3 :
                                      glassType.Contains("Енергозберігаючий") ? 4 : 2;

                // ==============================================================
                // 4️⃣ Розрахунок вартості
                // ==============================================================
                double total =
                    (framePerimeter / 1000.0) * profile.Price +
                    (sashPerimeter / 1000.0) * profile.Price * sashCount +
                    (impostLength / 1000.0) * profile.Price +
                    ((framePerimeter + sashPerimeter * sashCount + impostLength) / 1000.0) * (arm.Price + seal.Price) +
                    (glassArea * glassMultiplier * glass.Price) +
                    (sashCount * (handle.Price + hinge.Price)) +
                    ((width / 1000.0) * (sill.Price + drain.Price));

                // 🔹 Москітна сітка (якщо є)
                var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");
                if (hasMosquito && mosquito != null)
                {
                    total += glassArea * mosquito.Price;
                }


                // ==============================================================
                // 5️⃣ Форматований звіт
                // ==============================================================
                var sb = new System.Text.StringBuilder();

                // 🔹 Основна інформація — все в одному рядку
                sb.AppendLine(
                    $"Тип вікна: {windowType}, " +
                    $"Бренд профілю: {brand}, " +
                    $"Склопакет: {glassType} ({glassMultiplier}-шар.), "
                );

                           sb.AppendLine();

                // 🔹 Комплектуючі
                
                sb.Append("Ручки: ").Append($"{sashCount} × {handle.Price:F2} грн, ");                 
                sb.Append("Підвіконня: ").Append($"{sill.Price:F2} грн/м, ");
                sb.Append("Відлив: ").Append($"{drain.Price:F2} грн/м");
                if (hasMosquito && mosquito != null)
                    sb.Append($", Москітна сітка: {glassArea * mosquito.Price:F2} грн");                                                                                             
               
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

        // 👥 Керування користувачами
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

        // 👥 Керування користувачами
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
        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calculation_report.pdf");
                var sb = new StringBuilder();
                sb.AppendLine("=== WinCalc Звіт про обчислення ===");
                sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Користувач: {AppSession.CurrentUser?.Username ?? "Невідомий"}");
                sb.AppendLine();
                sb.AppendLine($"Тип вікна: {cmbWindowType.SelectedItem}");
                sb.AppendLine($"Бренд: {cmbBrand.SelectedItem}");
                sb.AppendLine($"Профіль: {cmbProfileThickness.SelectedItem}");
                sb.AppendLine($"Склопакет: {cmbGlassPack.SelectedItem}");
                sb.AppendLine($"Підвіконня: {(rbSill300.IsChecked == true ? "Білий 300мм" : "Білий 200мм")}");
                sb.AppendLine($"Відлив: {(rbDrain200.IsChecked == true ? "Білий 200мм" : "Білий 150мм")}");
                sb.AppendLine($"Москітна сітка: {(chkMosquito.IsChecked == true ? "Так" : "Ні")}");
                sb.AppendLine();
                sb.AppendLine($"Розміри: {txtWidth.Text} мм x {txtHeight.Text} мм");
                sb.AppendLine(lblResult.Text);

                File.WriteAllText(pdfPath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"✅ Звіт збережено як PDF: {pdfPath}", "Експорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка експорту PDF: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
