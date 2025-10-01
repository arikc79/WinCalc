using System;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;

using WinCalc.Security;
using WinCalc.Services;
using WinCalc.Storage;              
using WindowPaswoord.Models;       
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    public partial class MainWindow : Window
    {
        private readonly Obchyslennya calculator = new();
        private readonly DataAccess dataAccess = new();

        public bool LookupEditMode
        {
            get => _lookupEditMode;
            set
            {
                _lookupEditMode = value;
                // оновлюємо грід, щоб він перемалювався з новим IsEditable
                dgMaterials.Items.Refresh();
            }
        }
        private bool _lookupEditMode = false;


        // ====== Колекції для комбо у "Матеріали"
        public ObservableCollection<string> MaterialCategories { get; }
            = new() { "профіль", "скло", "фурнітура", "підвіконня", "відлив", "ущільнювач", "аксесуари" };

        public ObservableCollection<string> MaterialNames { get; }
            = new()
            {
                "Basic-Design (4)", "Euro 70 (5)", "Delight (6)", "Synego (7)",
                "Однокамерний", "Двокамерний", "Триплекс",
                "Ручка стандартна", "Петля комплект", "Мікроліфт",
                "Підвіконник ПВХ 200 мм", "Підвіконник ПВХ 300 мм",
                "Відлив 150 мм", "Відлив 200 мм",
                "Ущільнювач універсальний", "Москітна сітка",
            };

        public ObservableCollection<string> Units { get; } = new() { "м.пог.", "м²", "шт" };
        public ObservableCollection<string> Currencies { get; } = new() { "грн", "USD", "EUR" };

        // анти-реентрантний прапорець для збереження користувачів

        private bool _isSavingUserRow;

        // ====== Для вкладки "Користувачі"
        public ObservableCollection<string> RolesList { get; } = new() { Roles.Admin, Roles.Manager };
        private ObservableCollection<User> _users = new();


        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            Loaded += MainWindow_Loaded;

            // Прив'язка головної картинки
            imgSelected = FindName("imgSelected") as Image;
            if (imgSelected == null)
            {
                var border = FindName("borderWithImage") as Border;
                if (border != null)
                {
                    imgSelected = border.Child as Image ?? new Image { Width = 200, Height = 200 };
                    border.Child = imgSelected;
                }
            }
            imgSelected!.Visibility = Visibility.Visible;

            // Ініціалізація ComboBox
            cmbWindowType.ItemsSource = new[] { "1. Одностулкове", "2. Ділене навпіл", "3. Ділене на 3", "4. 4 секції", "5. 5 секцій" };
            cmbBrand.ItemsSource = new[] { "Rehau", "Steko", "Veka", "Openteck" };
            cmbProfile.ItemsSource = new[] { "Basic-Design (4)", "Euro 70 (5)", "Delight (6)", "Synego (7)" };
            cmbGlassPack.ItemsSource = new[] { "Однокамерний", "Двокамерний", "Триплекс" };
        }

        private void btnToggleLookupEdit_Click(object sender, RoutedEventArgs e)
        {
            LookupEditMode = !LookupEditMode;
            MessageBox.Show(LookupEditMode
                ? "Увімкнено режим редагування довідників (можна вводити нові значення)."
                : "Режим редагування вимкнено (можна тільки вибирати зі списків).",
                "Режим довідників", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ========== UI: РОЗРАХУНОК ==========
        private void lstImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstImages.SelectedItem is ListBoxItem li && li.Content is Image img)
            {
                imgSelected.Source = img.Source;
                if (li.Tag != null && int.TryParse(li.Tag.ToString(), out var n))
                    cmbWindowType.SelectedIndex = Math.Max(0, n - 1);
            }
        }

        private void cmbBrand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedBrand = cmbBrand.SelectedItem?.ToString();
            if (selectedBrand == null) return;

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
                if (cmbProfile.SelectedItem is string profile)
                {
                    if (profile.Contains("S500") || profile.Contains("Euro 70")) pricePerMeter = 425;
                    else if (profile.Contains("Synego") || profile.Contains("Softline 82 MD")) pricePerMeter = 1096;
                }

                bool includeSill = chkSill.IsChecked == true;
                bool includeDrain = chkDrain.IsChecked == true;

                double cost = length * pricePerMeter;
                lblResult.Content = $"За вибраними параметрами: {cost:F2} грн (Довжина: {length:F3} м)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== ЖИТТЄВИЙ ЦИКЛ ==========
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await new AuthService().EnsureAdminSeedAsync();

            if (!AppSession.IsAuthenticated)
            {
                var login = new LoginWindow();
                var ok = login.ShowDialog() == true;
                if (!ok) { Close(); return; }
            }

            RefreshMaterials();
            dgMaterials.IsReadOnly = !AppSession.IsInRole(Roles.Admin);

            SyncMaterialLookupsFromDb();

            await LoadUsersAsync();

            ApplyRoleUi();
        }

        // ========== МАТЕРІАЛИ ==========
        private void RefreshMaterials()
        {
            dgMaterials.ItemsSource = null;
            dgMaterials.ItemsSource = dataAccess.ReadMaterials();
        }

       
        private void dgMaterials_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row?.Item is not Material m) return;

            // ВАЖЛИВО: не викликати тут CommitEdit, це спричиняє рекурсію!
          
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    dataAccess.UpdateMaterial(m);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка збереження матеріалу: {ex.Message}");
                }
            }), DispatcherPriority.Background);
        }


        private void dgMaterials_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && !dgMaterials.IsReadOnly)
            {
                e.Handled = true;
                btnDeleteMaterial_Click(sender, new RoutedEventArgs());
            }
        }

        private void btnDeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            var selected = dgMaterials.SelectedItems.Cast<Material>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Виберіть хоча б один матеріал.", "Інформація");
                return;
            }
            if (MessageBox.Show($"Видалити {selected.Count} матеріал(и)?", "Підтвердження",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var m in selected) dataAccess.DeleteMaterial(m.Id);
            RefreshMaterials();
        }

        private void SyncMaterialLookupsFromDb()
        {
            var mats = dgMaterials.ItemsSource as IEnumerable<Material>;
            if (mats == null) mats = dataAccess.ReadMaterials();

            MergeInto(MaterialCategories, mats.Select(m => m.Category));
            MergeInto(MaterialNames, mats.Select(m => m.Name));
            MergeInto(Units, mats.Select(m => m.Unit));
        }

        private static void MergeInto(ObservableCollection<string> target, IEnumerable<string> values)
        {
            foreach (var v in values.Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Select(s => s.Trim())
                                    .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!target.Contains(v))
                    target.Add(v);
            }
        }

        private void btnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Обрати CSV з матеріалами",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() != true) return;

                var items = CsvMaterialImporter.Import(dlg.FileName);
                dataAccess.BulkUpsertMaterials(items);
                RefreshMaterials();
                MessageBox.Show($"Імпортовано {items.Count} позицій.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка імпорту: {ex.Message}");
            }
        }

        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Зберегти CSV",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = "materials.csv",
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog() != true) return;

                dataAccess.ExportMaterialsToCsv(dlg.FileName);
                MessageBox.Show("Експорт завершено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка експорту: {ex.Message}");
            }
        }

        // ======== Коли грід уже перейшов у режим редагування — відкриваємо ComboBox ========
        private void dgMaterials_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            // Працюємо тільки з шаблонними колонками
            if (e.Column is DataGridTemplateColumn)
            {
                // ComboBox знаходиться всередині e.EditingElement
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var combo = FindVisualChild<ComboBox>(e.EditingElement);
                    if (combo != null)
                    {
                        combo.Focus();
                        combo.IsDropDownOpen = true;
                    }
                }), DispatcherPriority.Background);
            }
        }

        // ========== КОРИСТУВАЧІ ==========
        private async Task LoadUsersAsync()
        {
            var store = new SqliteUserStore();
            var all = await store.GetAllAsync();
            _users = new ObservableCollection<User>(all);
            dgUsers.ItemsSource = _users;
            dgUsers.IsReadOnly = !AppSession.IsInRole(Roles.Admin);
        }

        private void dgUsers_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row?.Item is not User user) return;

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (_isSavingUserRow) return;
                _isSavingUserRow = true;
                try { await SaveUserAsync(user); }
                finally { _isSavingUserRow = false; }
            }), DispatcherPriority.Background);
        }

        private async Task SaveUserAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                MessageBox.Show("Логін не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(user.Role) || (user.Role != Roles.Admin && user.Role != Roles.Manager))
            {
                MessageBox.Show("Роль має бути 'admin' або 'manager'.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var store = new SqliteUserStore();

                if (user.Id == 0)
                {
                    user.PasswordHash = PasswordHasher.Hash("12345");
                    var created = await store.CreateAsync(user);
                    user.Id = created.Id;
                }
                else
                {
                    await store.UpdateAsync(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження користувача: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            var selected = dgUsers.SelectedItems.Cast<User>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Виберіть хоча б одного користувача.", "Інформація");
                return;
            }
            if (MessageBox.Show($"Видалити {selected.Count} користувача(ів)?", "Підтвердження",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var store = new SqliteUserStore();
                foreach (var u in selected)
                {
                    if (AppSession.CurrentUser?.Id == u.Id) continue;
                    await store.DeleteAsync(u.Id);
                    _users.Remove(u);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка видалення: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin)) return;

            if (dgUsers.SelectedItem is not User user)
            {
                MessageBox.Show("Оберіть користувача.", "Інформація");
                return;
            }

            var dlg = new ChangePasswordWindow(user.Username);
            if (dlg.ShowDialog() == true)
            {
                var newPass = dlg.NewPassword;
                if (string.IsNullOrWhiteSpace(newPass))
                {
                    MessageBox.Show("Пароль не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    var store = new SqliteUserStore();
                    await store.UpdatePasswordAsync(user.Id, PasswordHasher.Hash(newPass));
                    MessageBox.Show("Пароль оновлено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка оновлення пароля: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ===== Helpers =====
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void ApplyRoleUi()
        {
            SetVisibilityByTag(this, "AdminOnly", AppSession.IsInRole(Roles.Admin));
        }

        private static void SetVisibilityByTag(DependencyObject root, string tag, bool visible)
        {
            if (root is FrameworkElement fe && fe.Tag?.ToString() == tag)
                fe.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
                SetVisibilityByTag(VisualTreeHelper.GetChild(root, i), tag, visible);
        }
    }
}
