using System;
using System.Collections.Generic;
using System.Windows;
using WindowProfileCalculatorLibrary;

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
            MessageBox.Show("Функція імпорту CSV поки не реалізована.",
                      "Імпорт CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        

    }
}
