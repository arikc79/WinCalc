using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using WinCalc.Security;
using WinCalc.Services;
using WinCalc.Storage;
using WindowPaswoord.Models;

namespace WinCalc
{
    public partial class UserManagementWindow : Window
    {
        private readonly SqliteUserStore _store = new();
        private readonly AuthService _auth = new();

        public List<string> RolesList { get; } = new() { Roles.Admin, Roles.Manager };

        public UserManagementWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadUsersAsync();
        }

        private async void LoadUsersAsync()
        {
            try
            {
                dgUsers.ItemsSource = await _store.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження користувачів: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            // . Запитуємо Логін через наше нове вікно
            var dialogLogin = new SimpleInputDialog("Введіть логін нового користувача:", "Новий користувач");
            if (dialogLogin.ShowDialog() != true) return; // Якщо натиснули "Скасувати"

            string login = dialogLogin.Answer.Trim();
            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Логін не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //  Запитуємо Пароль
            var dialogPass = new SimpleInputDialog($"Введіть пароль для {login}:", "Створення пароля");
            if (dialogPass.ShowDialog() != true) return;

            string pass = dialogPass.Answer.Trim();
            if (string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Пароль не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Створюємо користувача
            try
            {
                var (ok, err) = await _auth.RegisterAsync(login, pass, Roles.Manager);
                if (ok)
                {
                    MessageBox.Show($"✅ Користувача '{login}' успішно створено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUsersAsync(); // Оновлюємо таблицю
                }
                else
                {
                    MessageBox.Show($"❌ Помилка: {err}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критична помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not User user)
            {
                MessageBox.Show("Оберіть користувача для зміни пароля.",
                                "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new ChangePasswordWindow(user.Username);
            if (dlg.ShowDialog() == true)
            {
                await _store.UpdatePasswordAsync(user.Id, dlg.NewPassword);
                AppAudit.RoleChanged(AppSession.CurrentUser?.Username ?? "?", user.Username, "PasswordChanged");
                MessageBox.Show($"✅ Пароль для користувача {user.Username} оновлено.",
                                "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not User user)
            {
                MessageBox.Show("Оберіть користувача для видалення.",
                                "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Видалити користувача '{user.Username}'?",
                                "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _store.DeleteAsync(user.Id);
                AppAudit.MaterialDelete(AppSession.CurrentUser?.Username ?? "?", user.Id, user.Username);
                MessageBox.Show("🗑️ Користувача видалено.",
                                "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsersAsync();
            }
        }

        private async void dgUsers_RowEditEnding(object sender, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
        {
            if (e.Row.Item is not User user) return;

            try
            {
                await _store.UpdateAsync(user);

                // 🔹 аудит зміни ролі
                string currentAdmin = AppSession.CurrentUser?.Username ?? "unknown";
                AppAudit.RoleChanged(currentAdmin, user.Username, user.Role);

                MessageBox.Show($"✅ Роль користувача '{user.Username}' змінено на '{user.Role}'.",
                                "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при збереженні ролі: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadUsersAsync();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
