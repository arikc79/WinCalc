using System;
using System.Windows;
using System.Windows.Controls;
using WinCalc.Security;
using WinCalc.Services;
using WindowPaswoord.Models;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private Obchyslennya calculator = new Obchyslennya();
        private DataAccess dataAccess = new DataAccess();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            // Ініціалізація ComboBox для брендів/профілів
            cmbWindowType.ItemsSource = new[] { "1. Одностулкове", "2. Ділене навпіл", "3. Ділене на 3", "4. 4 секції", "5. 5 секцій" };
            cmbBrand.ItemsSource = new[] { "Rehau", "Steko", "Veka", "Openteck" };
            cmbProfile.ItemsSource = new[] { "Basic-Design (4)", "Euro 70 (5)", "Delight (6)", "Synego (7)" };
            cmbGlassPack.ItemsSource = new[] { "Однокамерний", "Двокамерний", "Триплекс" };
        }

        // 📌 Подія зміни вибраного зображення
        private void lstImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstImages.SelectedItem is ListBoxItem selectedItem)
            {
                if (selectedItem.Content is Image image)
                {
                    imgSelected.Source = image.Source;
                    if (selectedItem.Tag != null)
                        cmbWindowType.SelectedIndex = int.Parse(selectedItem.Tag.ToString()) - 1;
                }
            }
        }

        // 📌 Подія зміни бренду → оновлення списку профілів
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

        // 📌 Кнопка "Розрахувати вартість"
        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (!Authorization.CanCalculate(AppSession.CurrentUser))
            {
                MessageBox.Show("Недостатньо прав (доступно для admin/manager).");
                return;
            }

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
                        length = calculator.CalculateProfileLengthType1(width, height, frameWidth, overlap, weldingAllowance); break;
                    case 2:
                        length = calculator.CalculateProfileLengthType2(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance); break;
                    case 3:
                        length = calculator.CalculateProfileLengthType3(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance); break;
                    case 4:
                        length = calculator.CalculateProfileLengthType4(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance); break;
                    case 5:
                        length = calculator.CalculateProfileLengthType5(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance); break;
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

        // 📌 Кнопка "Test CRUD" (тільки для перевірки)
        private async void btnTestCrud_Click(object sender, RoutedEventArgs e)
        {
            var auth = new AuthService();
            var (okReg, errReg) = await auth.RegisterAsync("testuser", "Test#123", Roles.Manager);
            if (!okReg && errReg != "Пользователь уже существует")
                MessageBox.Show(errReg ?? "Ошибка регистрации пользователя");

            var (okLogin, _, errLogin) = await auth.LoginAsync("testuser", "Test#123");
            if (!okLogin)
                MessageBox.Show(errLogin ?? "Ошибка входа пользователя");

            if (!Authorization.CanManageMaterials(AppSession.CurrentUser))
            {
                MessageBox.Show("Операції з матеріалами доступні тільки адміністратору.");
                return;
            }

            dataAccess.CreateMaterial("testcat", "testmat", "red", 100.0, "m", "length", "test desc");
            var materials = dataAccess.ReadMaterials();
            foreach (var mat in materials)
                Console.WriteLine($"Material: {mat.Category}, {mat.Name}, {mat.Price}");
        }

        // 📌 Завантаження при старті вікна
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await new AuthService().EnsureAdminSeedAsync();

            if (!AppSession.IsAuthenticated)
            {
                var login = new LoginWindow();
                var ok = login.ShowDialog() == true;
                if (!ok) { Close(); return; }
            }

            dgMaterials.ItemsSource = dataAccess.ReadMaterials();
            dgMaterials.IsReadOnly = !AppSession.IsInRole(Roles.Admin);

            if (AppSession.IsInRole(Roles.Admin))
            {
                var userStore = new WinCalc.Storage.SqliteUserStore();
                dgUsers.ItemsSource = await userStore.GetAllAsync();
                dgUsers.IsReadOnly = false;
            }

            ApplyRoleUi();
        }

        // 📌 Збереження змін у матеріалах
        private void dgMaterials_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            if (e.Row.Item is Material mat)
            {
                try
                {
                    dataAccess.UpdateMaterial(mat.Id, mat.Category, mat.Name, mat.Color,
                        mat.Price, mat.Unit, mat.QuantityType, mat.Description);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка збереження матеріалу: {ex.Message}");
                }
            }
        }

        // Збереження змін у користувачах (асинхронно, без зациклення)
        private async void dgUsers_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Виконуємо після того як WPF реально збереже дані в об’єкт User
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    if (e.Row.Item is User user)
                    {
                        try
                        {
                            var userStore = new WinCalc.Storage.SqliteUserStore();

                            if (string.IsNullOrWhiteSpace(user.Username))
                            {
                                MessageBox.Show("Логін не може бути порожнім!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (user.Id == 0)
                            {
                                // Новий користувач → INSERT
                                var created = await userStore.CreateAsync(user);
                                user.Id = created.Id; // Оновлюємо Id з БД
                            }
                            else
                            {
                                // Існуючий користувач → UPDATE
                                await userStore.UpdateAsync(user);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Помилка збереження користувача: {ex.Message}");
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        // Приховування вкладок з Tag="AdminOnly"
        private void ApplyRoleUi()
        {
            SetVisibilityByTag(this, "AdminOnly", AppSession.IsInRole(Roles.Admin));
        }

        private static void SetVisibilityByTag(DependencyObject root, string tag, bool visible)
        {
            if (root is FrameworkElement fe && fe.Tag?.ToString() == tag)
                fe.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
                SetVisibilityByTag(System.Windows.Media.VisualTreeHelper.GetChild(root, i), tag, visible);
        }
    }
}
