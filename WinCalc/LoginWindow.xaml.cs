
using System;
using System.Windows;
using WinCalc.Security;
using WinCalc.Services;
using WinCalc.Storage;
using WindowPaswoord.Models;

namespace WinCalc
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService = new();

        public LoginWindow()
        {
            InitializeComponent();
        }

        // 🔹 Вхід у систему
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var username = txtUsername.Text;
                var password = txtPassword.Password;

                var result = await _authService.LoginAsync(username, password);
                if (result.ok)
                {
                    // Якщо метод не повертає user — створюємо вручну
                    var user = result.user ?? new User
                    {
                        Username = username,
                        Role = Roles.Admin
                    };

                    AppSession.SetCurrentUser(user);

                    AppAudit.LoginOk(username);
                    DialogResult = true;
                    Close();
                }

                else
                {
                    LblStatus.Text = result.error ?? "Невірний логін або пароль.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка авторизації: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 створення менеджера (для першого запуску)
        private async void CreateManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var (ok, err) = await _authService.RegisterAsync("manager", "manager123", Roles.Manager);

                if (ok)
                {
                    MessageBox.Show("✅ Менеджера створено: логін 'manager', пароль 'manager123'",
                                    "Створення користувача", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(err ?? "Не вдалося створити менеджера.",
                                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка створення менеджера: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
