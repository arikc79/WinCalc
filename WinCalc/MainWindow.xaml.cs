using MaterialsFeatureLib;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinCalc.Security;
using WinCalc.Services;
using WinCalc.Storage;
using WindowPaswoord.Models;
using WindowProfileCalculatorLibrary;


namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private readonly Obchyslennya calculator = new Obchyslennya();
        private readonly DataAccess dataAccess = new DataAccess();
        private ObservableCollection<User> _users = new();
        private ObservableCollection<Material> _materials = new();

        // Фіксовані списки для комбобоксів у гріді матеріалів
        public System.Collections.ObjectModel.ObservableCollection<string> MaterialCategories { get; }
            = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                 "профіль",
                 "скло",
                 "фурнітура",
                 "підвіконня",
                 "відлив",
                 "ущільнювач",
                 "аксесуари"
            };

        public System.Collections.ObjectModel.ObservableCollection<string> Units { get; }
            = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "м.пог.",
                "м²",
                "шт"
            };

        public System.Collections.ObjectModel.ObservableCollection<string> Currencies { get; }
            = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "грн",
                "USD",
                "EUR"
            };

        // Загальний список назв (початковий варіант; можна винести у CSV/БД)
        public System.Collections.ObjectModel.ObservableCollection<string> MaterialNames { get; }
            = new System.Collections.ObjectModel.ObservableCollection<string>
            {
        // профіль
        "Basic-Design (4)",
        "Euro 70 (5)",
        "Delight (6)",
        "Synego (7)",
        // скло
        "Однокамерний",
        "Двокамерний",
        "Триплекс",
        // фурнітура
        "Ручка стандартна",
        "Петля комплект",
        "Мікроліфт",
        // підвіконня
        "Підвіконник ПВХ 200 мм",
        "Підвіконник ПВХ 300 мм",
        // відлив
        "Відлив 150 мм",
        "Відлив 200 мм",
        // ущільнювач/аксесуари
        "Ущільнювач універсальний",
        "Москітна сітка",
            };



        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += MainWindow_Loaded;

            // Ініціалізація ComboBox
            cmbWindowType.ItemsSource = new[] { "1. Одностулкове", "2. Ділене навпіл", "3. Ділене на 3", "4. 4 секції", "5. 5 секцій" };
            cmbBrand.ItemsSource = new[] { "Rehau", "Steko", "Veka", "Openteck" };
            cmbProfile.ItemsSource = new[] { "Basic-Design (4)", "Euro 70 (5)", "Delight (6)", "Synego (7)" };
            cmbGlassPack.ItemsSource = new[] { "Однокамерний", "Двокамерний", "Триплекс" };
        }



        // HELP: оновити таблицю матеріалів
        private void RefreshMaterials()
        {
            dgMaterials.ItemsSource = null;
            dgMaterials.ItemsSource = dataAccess.ReadMaterials();
            dgMaterials.IsReadOnly = !WinCalc.Security.AppSession.IsInRole(WinCalc.Security.Roles.Admin);
        }

        // IMPORT CSV
        private void btnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!WinCalc.Security.AppSession.IsInRole(WinCalc.Security.Roles.Admin))
            {
                MessageBox.Show("Імпорт доступний лише адміністратору.");
                return;
            }

            var ofd = new OpenFileDialog
            {
                Filter = "CSV files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*",
                Title = "Оберіть CSV з матеріалами"
            };
            if (ofd.ShowDialog() != true) return;

            try
            {
                var rows = CsvMaterialImporter.Import(ofd.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show("Файл не містить даних.");
                    return;
                }

                var (inserted, updated) = dataAccess.BulkUpsertMaterials(rows);
                RefreshMaterials();
                MessageBox.Show($"Імпорт завершено.\nДодано: {inserted}\nОновлено: {updated}", "Готово");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка імпорту: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // DELETE SELECTED
        private void btnDeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!WinCalc.Security.AppSession.IsInRole(WinCalc.Security.Roles.Admin))
            {
                MessageBox.Show("Видалення доступне лише адміністратору.");
                return;
            }

            var selected = dgMaterials.SelectedItems.Cast<WindowProfileCalculatorLibrary.Material>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Виберіть хоча б один матеріал.");
                return;
            }

            if (MessageBox.Show($"Видалити {selected.Count} запис(ів)?", "Підтвердження",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var m in selected)
            {
                try { dataAccess.DeleteMaterial(m.Id); }
                catch (Exception ex) { MessageBox.Show($"Не вдалося видалити ID={m.Id}: {ex.Message}"); }
            }

            RefreshMaterials();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await new AuthService().EnsureAdminSeedAsync();

            if (!AppSession.IsAuthenticated)
            {
                var login = new LoginWindow();
                var ok = login.ShowDialog() == true;
                if (!ok) { Close(); return; }
            }

            // Матеріали
            _materials = new ObservableCollection<Material>(dataAccess.ReadMaterials());
            dgMaterials.ItemsSource = dataAccess.ReadMaterials();
            dgMaterials.IsReadOnly = !AppSession.IsInRole(Roles.Admin);



            // Користувачі (тільки для admin)
            if (AppSession.IsInRole(Roles.Admin))
            {
                var userStore = new WinCalc.Storage.SqliteUserStore();
                var list = await userStore.GetAllAsync();
                _users = new ObservableCollection<User>(list);
                dgUsers.ItemsSource = _users;
                dgUsers.IsReadOnly = false;
            }

            ApplyRoleUi();
        }

        // ===== Розрахунок / зображення =====

        private void lstImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstImages.SelectedItem is ListBoxItem it && it.Content is Image img)
            {
                imgSelected.Source = img.Source;
                if (it.Tag != null) cmbWindowType.SelectedIndex = int.Parse(it.Tag.ToString()) - 1;
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

                double length = windowType switch
                {
                    1 => calculator.CalculateProfileLengthType1(width, height, frameWidth, overlap, weldingAllowance),
                    2 => calculator.CalculateProfileLengthType2(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance),
                    3 => calculator.CalculateProfileLengthType3(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance),
                    4 => calculator.CalculateProfileLengthType4(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance),
                    5 => calculator.CalculateProfileLengthType5(width, height, frameWidth, midFrameWidth, overlap, weldingAllowance),
                    _ => throw new ArgumentException("Невірний тип вікна.")
                };

                double pricePerMeter = 425;
                if (cmbProfile.SelectedItem != null)
                {
                    string profile = cmbProfile.SelectedItem.ToString();
                    if (profile.Contains("S500") || profile.Contains("Euro 70")) pricePerMeter = 425;
                    else if (profile.Contains("Synego") || profile.Contains("Softline 82 MD")) pricePerMeter = 1096;
                }

                double cost = length * pricePerMeter;
                lblResult.Content = $"За вибраними параметрами: {cost:F2} грн (Довжина: {length:F3} м)";

                // сохранить строку в отчёт
                ReportService.Append(new ReportRow
                {
                    User = AppSession.CurrentUser?.Username, 
                    WindowType = cmbWindowType.Text,
                    Brand = cmbBrand.Text,
                    Profile = cmbProfile.Text,
                    GlassPack = cmbGlassPack.Text,
                    Width = width,
                    Height = height,
                    Length = length,
                    PricePerMeter = pricePerMeter,
                    Cost = cost,
                    Sill = chkSill.IsChecked == true,
                    Drain = chkDrain.IsChecked == true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            foreach (var mat in dataAccess.ReadMaterials())
                Console.WriteLine($"Material: {mat.Category}, {mat.Name}, {mat.Price}");
        }

        // ===== Матеріали: збереження змін =====

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

        // ===== Користувачі: збереження змін (без рекурсії) =====

        private void dgUsers_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Відкласти виконання після завершення редагування
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    if (e.Row.Item is User user)
                    {
                        try
                        {
                            var userStore = new SqliteUserStore();

                            if (string.IsNullOrWhiteSpace(user.Username))
                            {
                                MessageBox.Show("Логін не може бути порожнім!", "Помилка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (user.Id == 0)
                            {
                                // Новий користувач → INSERT (якщо пароль не вказано — буде 12345)
                                var created = await userStore.CreateAsync(user);
                                user.Id = created.Id;
                            }
                            else
                            {
                                // Існуючий → UPDATE (Role/Login)
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

        // Кнопка "Змінити…" пароль 
        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            if (sender is Button btn && btn.DataContext is User user)
            {
                var dlg = new ChangePasswordWindow(user.Username);
                if (dlg.ShowDialog() == true)
                {
                    string newPlain = dlg.NewPassword;
                    var (ok, msg) = ValidatePassword(newPlain);
                    if (!ok)
                    {
                        MessageBox.Show(msg, "Пароль занадто слабкий",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var store = new SqliteUserStore();
                        await store.UpdatePasswordAsync(user.Id, newPlain);
                        MessageBox.Show($"Пароль для {user.Username} змінено.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не вдалося оновити пароль: {ex.Message}", "Помилка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Мінімальні вимоги до пароля
        private static (bool ok, string message) ValidatePassword(string pwd)
        {
            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 6)
                return (false, "Пароль має містити не менше 6 символів.");
            if (!Regex.IsMatch(pwd, @"[A-Za-z]"))
                return (false, "Пароль має містити принаймні одну літеру.");
            if (!Regex.IsMatch(pwd, @"\d"))
                return (false, "Пароль має містити принаймні одну цифру.");
            return (true, "");
        }

        // Приховування вкладок для ролей 

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


        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            if (sender is Button btn && btn.DataContext is User user)
            {
                // якщо рядок новий і ще не збережений — просто приберемо з гріда
                if (user.Id == 0)
                {
                    _users.Remove(user);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Видалити користувача \"{user.Username}\"?",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    var store = new WinCalc.Storage.SqliteUserStore();
                    await store.DeleteAsync(user.Id);   // БД
                    _users.Remove(user);                // UI — миттєво
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не вдалося видалити користувача: {ex.Message}",
                        "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // Видалення вибраних рядків
        private void btnDeleteSelectedMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) { MessageBox.Show("Лише для адміністратора"); return; }
            var selected = dgMaterials.SelectedItems.Cast<Material>().ToList();
            if (selected.Count == 0) return;

            var ask = MessageBox.Show($"Видалити вибрані матеріали ({selected.Count})?",
                                      "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (ask != MessageBoxResult.Yes) return;

            foreach (var m in selected)
            {
                try
                {
                    dataAccess.DeleteMaterial(m.Id);
                    _materials.Remove(m);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не вдалося видалити '{m.Name}': {ex.Message}");
                }
            }
        }

        // Швидке додавання порожнього рядка (адмін руками заповнить у гріді)
        private void btnAddMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) { MessageBox.Show("Лише для адміністратора"); return; }

            var m = new Material
            {
                // дефолти
                Category = "",
                Name = "Новий матеріал",
                Color = "",
                Price = 0,
                Unit = "шт",
                Quantity = 0,               
                QuantityType = "шт",           // або "м", "м²" 
                Description = ""
            };

            // пишемо в БД, щоб отримати Id, і додаємо в колекцію
            var created = dataAccess.CreateMaterial(m);
            _materials.Add(created);
            dgMaterials.SelectedItem = created;
            dgMaterials.ScrollIntoView(created);
        }
        private void dgMaterials_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                // Жмём Delete — удаляем выделенные записи той же логикой, что по кнопке
                btnDeleteMaterial_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"materials_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Title = "Зберегти список матеріалів"
            };
            if (sfd.ShowDialog() != true) return;

            try
            {
                var rows = dataAccess.ReadMaterials(); // всё, что сейчас в БД
                using var w = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);

                // шапка в формате, совместимом с импортом
                w.WriteLine("Категорія;Назва;Колір;Ціна;Одиниця;Кількість;ТипК-ті;Опис");

                foreach (var m in rows)
                {
                    string line = $"{m.Category};{m.Name};{m.Color};{m.Price};{m.Unit};{m.Quantity};{m.QuantityType};{m.Description}";
                    w.WriteLine(line);
                }

                MessageBox.Show("Експорт завершено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка експорту: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



    }
}
