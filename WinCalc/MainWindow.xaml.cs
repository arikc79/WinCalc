using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using WinCalc.Security;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private readonly DataAccess _dataAccess = new();
        private readonly Obchyslennya _calculator = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadMaterialsToGrid();
            InitializeComboboxes();
        }

        // ============================ ІНІЦІАЛІЗАЦІЯ ============================

        private void InitializeComboboxes()
        {
            cmbWindowType.ItemsSource = new List<string> { "Одностулкове", "Двостулкове", "Тристулкове", "Балконний блок" };
            cmbWindowType.SelectedIndex = 0;

            cmbBrand.ItemsSource = _dataAccess.GetDistinctBrands();
            if (cmbBrand.Items.Count > 0)
                cmbBrand.SelectedIndex = 0;

            cmbProfileThickness.ItemsSource = _dataAccess.GetDistinctProfileThicknesses();
            if (cmbProfileThickness.Items.Count > 0)
                cmbProfileThickness.SelectedIndex = 0;

            cmbGlassPack.ItemsSource = new List<string> { "1-камерний", "2-камерний", "Енергозберігаючий" };
            cmbGlassPack.SelectedIndex = 0;
        }

        private void LoadMaterialsToGrid()
        {
            try
            {
                var materials = _dataAccess.GetAllMaterials();
                dgMaterials.ItemsSource = materials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні матеріалів: {ex.Message}");
            }
        }

        // ============================ РОЗРАХУНОК ============================

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Перевірка введених даних
                if (!double.TryParse(txtWidth.Text, out double width) || !double.TryParse(txtHeight.Text, out double height))
                {
                    MessageBox.Show("Невірно вказано ширину або висоту.");
                    return;
                }

                // Формування конфігурації
                var config = new WindowConfig
                {
                    Brand = cmbBrand.SelectedItem?.ToString() ?? "",
                    ProfileThickness = cmbProfileThickness.SelectedItem?.ToString() ?? "",
                    GlassType = cmbGlassPack.SelectedItem?.ToString() ?? "",
                    WindowType = cmbWindowType.SelectedItem?.ToString() ?? "",
                    Width = (decimal)width,
                    Height = (decimal)height,
                    HandleType = rbHandlePremium.IsChecked == true ? "Преміум" : "Стандартна",
                    SillType = rbSill300.IsChecked == true ? "300 мм" : "200 мм",
                    DrainType = rbDrain200.IsChecked == true ? "200 мм" : "150 мм",
                    HasMosquito = chkMosquito.IsChecked == true
                };

                // Обчислення вартості
                decimal totalPrice = _calculator.CalculateWindowPrice(config);

                // Формуємо опис результату
                string resultDescription =
                    $"Профіль: {config.Brand} ({config.ProfileThickness})\n" +
                    $"Тип склопакету: {config.GlassType}\n" +
                    $"Ручка: {config.HandleType}\n" +
                    $"Підвіконня: {config.SillType}\n" +
                    $"Відлив: {config.DrainType}\n" +
                    (config.HasMosquito ? "Москітна сітка: так\n" : "") +
                    $"Загальна площа: {Math.Round((width * height) / 1_000_000, 2)} м²\n" +
                    $"----------------------------------\n" +
                    $"Вартість без встановлення: {totalPrice:F2} грн";

                lblResult.Text = resultDescription;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при розрахунку: {ex.Message}");
            }
        }

        // ============================ CSV ============================

        private void btnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV файли (*.csv)|*.csv",
                    Title = "Імпорт матеріалів з CSV"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Використовуємо правильний метод імпорту
                    var imported = CsvMaterialImporter.Import(dialog.FileName);

                    MessageBox.Show($"Імпортовано {imported.Count} матеріалів з файлу:\n{System.IO.Path.GetFileName(dialog.FileName)}",
                                    "Імпорт завершено успішно", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Оновлюємо таблицю
                    LoadMaterialsToGrid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка імпорту CSV: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файли (*.csv)|*.csv",
                    Title = "Збереження списку матеріалів"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Отримуємо матеріали з бази даних
                    var materials = _dataAccess.GetAllMaterials();

                    // Викликаємо експорт CSV
                    CsvMaterialImporter.Export(dialog.FileName, materials);

                    MessageBox.Show("Експорт завершено успішно!",
                                    "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка експорту CSV: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // ============================ РЕДАГУВАННЯ МАТЕРІАЛІВ ============================


        private void btnMaterials_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔐 Тільки для адміністратора
                if (!AppSession.IsInRole(Roles.Admin))

                {
                    MessageBox.Show("Доступ дозволено лише адміністратору!", "Обмеження",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var win = new MaterialsWindow();
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка відкриття: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void btnDeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgMaterials.SelectedItem is WindowProfileCalculatorLibrary.Material selected)
                {
                    _dataAccess.DeleteMaterial(selected.Id);
                    LoadMaterialsToGrid();
                }
                else
                {
                    MessageBox.Show("Оберіть матеріал для видалення.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка видалення: {ex.Message}");
            }
        }

        // ============================ КОРИСТУВАЧІ ============================

        private void btnManageUsers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Перевірка ролі користувача
                if (!AppSession.IsInRole(Roles.Admin))
                {
                    MessageBox.Show("Доступ дозволений лише адміністратору.",
                                    "Обмеження доступу", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Відкрити вікно керування користувачами
                var wnd = new UserManagementWindow();
                wnd.Owner = this;
                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка відкриття панелі користувачів: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void dgUsers_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            MessageBox.Show("Редагування користувача — логіка залишена з попередньої реалізації.");
        }
    }
}
