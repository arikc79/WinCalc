using System.Windows;
using System.Windows.Controls;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private Obchyslennya calculator = new Obchyslennya();
      

        public MainWindow()
        {
            InitializeComponent();
            // Прив’язуємо imgSelected до елемента з XAML, шукаючи його в контексті
            imgSelected = this.FindName("imgSelected") as Image;
            if (imgSelected == null)
            {
                // Якщо не знайдено, спробуємо знайти в межах Border
                var border = this.FindName("borderWithImage") as Border; // Додаємо ім’я Border
                if (border != null)
                {
                    imgSelected = border.Child as Image;
                }
                if (imgSelected == null)
                {
                    throw new Exception("Елемент imgSelected не знайдено в XAML або в Border.");
                }
            }
            // Ініціалізація ComboBox
            // cmbConfiguration.ItemsSource = new[] { "Стандарт", "Преміум" };
            cmbWindowType.ItemsSource = new[] { "1. Одностулкове", "2. Ділене навпіл", "3. Ділене на 3", "4. 4 секції", "5. 5 секцій" };
            cmbBrand.ItemsSource = new[] { "Rehau", "Steko", "Veka", "Openteck" };
            cmbProfile.ItemsSource = new[] { "Basic-Design (4)", "Euro 70 (5)", "Delight (6)", "Synego (7)" };
            cmbGlassPack.ItemsSource = new[] { "Однокамерний", "Двокамерний", "Триплекс" };
        }

        private void lstImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstImages.SelectedItem is ListBoxItem selectedItem)
            {
                if (selectedItem.Content is Image image)
                {
                    imgSelected.Source = image.Source; // Відображаємо обране зображення в Border
                    cmbWindowType.SelectedIndex = int.Parse(selectedItem.Tag.ToString()) - 1; // Синхронізуємо з типом вікна
                }
            }
        }

        private void cmbBrand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedBrand = cmbBrand.SelectedItem?.ToString();
            if (selectedBrand != null)
            {
                switch (selectedBrand)
                {
                    case "Rehau":
                        cmbProfile.ItemsSource = new[] { "Basic-Design (4)", "Euro 70 (5)", "Delight (6)", "Synego (7)" };
                        break;
                    case "Steko":
                        cmbProfile.ItemsSource = new[] { "S400 (4)", "S500 (5)", "S600 (6)", "S700 (7)" };
                        break;
                    case "Veka":
                        cmbProfile.ItemsSource = new[] { "Euroline 58 (4)", "Softline 70 (5)", "Softline 82 (6)", "Softline 82 MD (7)" };
                        break;
                    case "Openteck":
                        cmbProfile.ItemsSource = new[] { "Elit 60 (4)", "Elit 65 (5)", "Elit Soft70 (6)", "Elit 80 (7)" };
                        break;
                }
                cmbProfile.SelectedIndex = 0;
            }
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double width = double.Parse(txtWidth.Text.Replace("Ширина мм.", "").Trim()) / 1000;
                double height = double.Parse(txtHeight.Text.Replace("Висота мм.", "").Trim()) / 1000;
                int windowType = cmbWindowType.SelectedIndex + 1;

                double frameWidth = 0.06;
                double midFrameWidth = 0.064;
                double overlap = 0.008;
                double weldingAllowance = 0.003;

                double length = 0;

                switch (windowType)
                {
                    case 1:
                        length = calculator.CalculateProfileLengthType1(width, height, frameWidth, overlap, weldingAllowance);
                        break;
                    case 2:
                        length = calculator.CalculateProfileLengthType2(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance);
                        break;
                    case 3:
                        length = calculator.CalculateProfileLengthType3(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance);
                        break;
                    case 4:
                        length = calculator.CalculateProfileLengthType4(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance);
                        break;
                    case 5:
                        length = calculator.CalculateProfileLengthType5(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance);
                        break;
                    default:
                        throw new ArgumentException("Невірний тип вікна.");
                }

                double pricePerMeter = 425;
                if (cmbProfile.SelectedItem != null)
                {
                    string profile = cmbProfile.SelectedItem.ToString();
                    if (profile.Contains("S500") || profile.Contains("Euro 70")) pricePerMeter = 425;
                    else if (profile.Contains("Synego") || profile.Contains("Softline 82 MD")) pricePerMeter = 1096;
                }

                double cost = length * pricePerMeter;

                lblResult.Content = $"За вибраними параметрами: {cost:F2} грн (Довжина: {length:F3} м)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}