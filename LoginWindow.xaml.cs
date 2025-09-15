using System.Windows;
using WinCalc.Security;
using WinCalc.Services;

namespace WinCalc
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth = new();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var login = TbLogin.Text?.Trim();
            var pass = TbPass.Password;

            var (ok, user, err) = await _auth.LoginAsync(login!, pass);
            if (!ok)
            {
                LblStatus.Text = err;
                return;
            }

            AppSession.SignIn(user!);
            DialogResult = true;
            Close();
        }

        private async void CreateManager_Click(object sender, RoutedEventArgs e)
        {
            var login = TbLogin.Text?.Trim();
            var pass = TbPass.Password;

            var (ok, err) = await _auth.RegisterAsync(login!, pass, Roles.Manager);
            LblStatus.Text = ok ? "Создан пользователь-менеджер" : err;
        }
    }
}
