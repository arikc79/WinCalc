using System;
using System.Collections.Generic;
using System.Windows;
using WinCalc.Security;
using WindowProfileCalculatorLibrary;
using Microsoft.Win32;

namespace WinCalc
{
    public partial class MaterialsWindow : Window
    {
        private readonly DataAccess _dataAccess = new();
        private List<Material> _materials = new();

        public MaterialsWindow()
        {
            InitializeComponent();
            LoadMaterials();
        }

        private void LoadMaterials()
        {
            try
            {
                _materials = _dataAccess.GetAllMaterials();
                dgMaterials.ItemsSource = _materials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження матеріалів: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 💾 Зберегти зміни
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            int updated = 0;

            foreach (var mat in _materials)
            {
                if (_dataAccess.UpdateMaterial(mat))
                    updated++;
            }

            MessageBox.Show($"✅ Зміни збережено ({updated})", "Успіх",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ➕ Додати новий матеріал
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newMat = new Material
                {
                    Category = "Нова категорія",
                    Name = "Новий матеріал",
                    Color = "",
                    Price = 0,
                    Unit = "шт",
                    Description = ""
                };

                if (_dataAccess.AddMaterial(newMat))
                {
                    LoadMaterials();
                    MessageBox.Show("✅ Новий матеріал додано!", "Успіх",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка додавання: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ❌ Закрити
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 📁 Імпорт з CSV
        private void btnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Виберіть файл матеріалів (CSV)"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;

                    //  Читаємо файл через ваш готовий імпортер
                    var (importedMaterials, skipped) = CsvMaterialImporter.Import(filePath);

                    if (importedMaterials.Count == 0)
                    {
                        MessageBox.Show("Файл порожній або не містить коректних даних.",
                                        "Імпорт", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int addedCount = 0;
                    foreach (var mat in importedMaterials)
                    {
                        if (_dataAccess.AddMaterial(mat))
                            addedCount++;
                    }

                    LoadMaterials();
                    AppAudit.MaterialsImport(AppSession.CurrentUser?.Username ?? "?", addedCount, 0);

                    string summary = skipped > 0
                        ? $"✅ Імпортовано {addedCount} матеріалів.\n⚠️ Пропущено рядків: {skipped} (неправильний формат)."
                        : $"✅ Успішно імпортовано {addedCount} матеріалів.";
                    MessageBox.Show(summary, "Імпорт завершено", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при імпорті: {ex.Message}",
                                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
