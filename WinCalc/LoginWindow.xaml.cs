using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinCalc.Security;
using WinCalc.Services;
using WindowPaswoord.Models;

namespace WinCalc
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService = new();
        private static readonly Regex validChars = new(@"^[A-Za-zА-Яа-яІіЇїЄєҐґ]+$");

        public LoginWindow()
        {
            InitializeComponent();
        }

        //  Вхід у систему
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            //  Перевірка логіну
            string username = txtUsername.Text.Trim();
            var password = txtPassword.Password.Trim();

            // якщо пусто
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Будь ласка, введіть логін.", "Помилка авторизації",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // регулярки для кирилиці / латиниці
            bool isCyrillic = Regex.IsMatch(username, @"^[А-Яа-яЇїІіЄєҐґ]+$");
            bool isLatin = Regex.IsMatch(username, @"^[A-Za-z]+$");

            // Якщо логін НЕ кирилиця І НЕ латиниця 
            if (!isCyrillic && !isLatin)
            {
                MessageBox.Show("Логін має містити лише кирилицю або лише латиницю.\nЗмішаний варіант не дозволено.",
                    "Помилка введення", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
         

            try
            {
                // аварійний логін
                if (username == "admin" && password == "admin")
                {
                    AppSession.SetCurrentUser(new User { Username = "admin", Role = Roles.Admin });
                    AppAudit.LoginOk(username);
                    MessageBox.Show("✅ Вхід виконано успішно як адміністратор",
                                    "Авторизація", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                    return;
                }

                //  Виклик асинхронної авторизації
                var result = await _authService.LoginAsync(username, password);

                if (result.ok)
                {
                    var user = result.user ?? new User
                    {
                        Username = username,
                        Role = Roles.Manager
                    };

                    AppSession.SetCurrentUser(user);
                    AppAudit.LoginOk(username);

                    MessageBox.Show($"✅ Вхід виконано успішно як {user.Role}",
                                    "Авторизація", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    LblStatus.Text = result.error ?? "❌ Невірний логін або пароль.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка авторизації: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        // Підказка при наведенні
        private void Username_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox tb)
            {
                bool isValid = validChars.IsMatch(e.Text);
                e.Handled = !isValid;

                // якщо користувач натиснув не ту клавішу — тимчасово підсвічуємо червоним
                if (!isValid)
                {
                    tb.BorderBrush = Brushes.IndianRed;
                    tb.ToolTip = " Можна вводити лише кирилицю або латиницю, без цифр!";
                }
                else
                {
                    tb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00BCD4"));
                    tb.ToolTip = "✅ Дозволено вводити лише кирилицю або лише латиницю (без цифр)";
                }
            }
        }


        //  створення менеджера (для першого запуску)
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

        // кнопка HElP
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            var help = new HelpWindow
            {
                Owner = this
            };
            help.ShowDialog();
        }


    }
}
