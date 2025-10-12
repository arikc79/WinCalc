// WinCalc/UserManagementWindow.xaml.cs
using System;
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

        public UserManagementWindow()
        {
            InitializeComponent();
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
                MessageBox.Show($"Помилка завантаження користувачів: {ex.Message}");
            }
        }

        private async void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var login = Microsoft.VisualBasic.Interaction.InputBox("Введіть логін нового користувача:", "Новий користувач");
            if (string.IsNullOrWhiteSpace(login)) return;

            var pass = Microsoft.VisualBasic.Interaction.InputBox($"Введіть пароль для {login}:", "Пароль користувача");
            if (string.IsNullOrWhiteSpace(pass)) return;

            var (ok, err) = await _auth.RegisterAsync(login, pass, Roles.Manager);
            if (ok)
            {
                MessageBox.Show($"✅ Користувача '{login}' створено.");
                AppAudit.LoginOk(AppSession.CurrentUser?.Username ?? "?");
                LoadUsersAsync();
            }
            else
                MessageBox.Show($"Помилка: {err}");
        }


        private void btnManageUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsInRole(Roles.Admin))
            {
                MessageBox.Show("Доступ дозволений лише адміністратору.");
                return;
            }

            new UserManagementWindow().ShowDialog();
        }


        private async void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not User user)
            {
                MessageBox.Show("Оберіть користувача для зміни пароля.");
                return;
            }

            var dlg = new ChangePasswordWindow(user.Username);
            if (dlg.ShowDialog() == true)
            {
                await _store.UpdatePasswordAsync(user.Id, dlg.NewPassword);
                AppAudit.MaterialsImport(AppSession.CurrentUser?.Username ?? "?", 0, 0);
                MessageBox.Show($"✅ Пароль для {user.Username} оновлено.");
            }
        }

        private async void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not User user)
            {
                MessageBox.Show("Оберіть користувача для видалення.");
                return;
            }

            if (MessageBox.Show($"Видалити користувача '{user.Username}'?",
                                "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _store.DeleteAsync(user.Id);
                AppAudit.MaterialDelete(AppSession.CurrentUser?.Username ?? "?", user.Id, user.Username);
                MessageBox.Show("🗑️ Користувача видалено.");
                LoadUsersAsync();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadUsersAsync();
    }
}
