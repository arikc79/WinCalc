using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindowProfileCalculatorLibrary;
using WinCalc.Security;

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
                    MessageBox.Show("Будь ласка, введіть коректні розміри.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string brand = cmbBrand.SelectedItem?.ToString() ?? "";
                string thickness = cmbProfileThickness.SelectedItem?.ToString() ?? "";
                string glass = cmbGlassPack.SelectedItem?.ToString() ?? "";

                bool hasMosquito = chkMosquito.IsChecked == true;
                bool handlePremium = rbHandlePremium.IsChecked == true;
                bool sill300 = rbSill300.IsChecked == true;
                bool drain200 = rbDrain200.IsChecked == true;

                // 🔹 Отримання базових матеріалів
                var profile = _dataAccess.GetMaterialByCategoryAndBrand("Профіль", brand);
                var handle = _dataAccess.GetMaterialByCategory("Ручка", handlePremium ? "Преміум" : "Стандарт");
                var sill = _dataAccess.GetMaterialByCategory("Підвіконня", sill300 ? "Білий 300мм" : "Білий 200мм");
                var drain = _dataAccess.GetMaterialByCategory("Відлив", drain200 ? "Білий 200мм" : "Білий 150мм");
                var glassPack = _dataAccess.GetMaterialByCategory("Склопакет", glass);

                if (profile == null || handle == null || sill == null || drain == null || glassPack == null)
                {
                    MessageBox.Show("Не знайдено деякі матеріали у базі даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔹 Формула (умовно): площа * (профіль + склопакет) + комплектуючі
                double areaM2 = (width / 1000.0) * (height / 1000.0);
                double perimeter = 2 * ((width / 1000.0) + (height / 1000.0));

                double cost =
                    areaM2 * glassPack.Price +
                    perimeter * profile.Price +
                    handle.Price +
                    sill.Price +
                    drain.Price;

                if (hasMosquito)
                {
                    var mosquito = _dataAccess.GetMaterialByCategory("Москітна сітка", "Стандарт");
                    if (mosquito != null)
                        cost += areaM2 * mosquito.Price;
                }

                lblResult.Text = $"Орієнтовна вартість: {cost:F2} грн";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка під час розрахунку: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===============================================================
        //  ДОДАТКОВІ КНОПКИ
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

        private void btnManageUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin))
            {
                MessageBox.Show("Доступ дозволено лише адміністратору.", "Доступ заборонено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new ChangePasswordWindow(AppSession.CurrentUser?.Username ?? "admin");
            win.ShowDialog();
        }

        private void btnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функція імпорту CSV поки не реалізована.");
        }

        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "materials_export.csv");
                _dataAccess.ExportToCsv(exportPath);
                MessageBox.Show($"Файл успішно експортовано: {exportPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка експорту: {ex.Message}");
            }
        }

        private void btnDeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функція видалення матеріалів доступна у каталозі матеріалів.");
        }
    }
}
