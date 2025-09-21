using System.Windows;

namespace WinCalc
{
    public partial class ChangePasswordWindow : Window
    {
        public string NewPassword { get; private set; } = "";

        public ChangePasswordWindow(string username)
        {
            InitializeComponent();
            txtUser.Text = username;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pb1.Password) || string.IsNullOrWhiteSpace(pb2.Password))
            {
                MessageBox.Show("Заповніть обидва поля.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (pb1.Password != pb2.Password)
            {
                MessageBox.Show("Паролі не співпадають.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewPassword = pb1.Password;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
