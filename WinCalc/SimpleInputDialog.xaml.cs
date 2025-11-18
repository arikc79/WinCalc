using System.Windows;

namespace WinCalc
{
    public partial class SimpleInputDialog : Window
    {
        public string Answer { get; private set; } = string.Empty;

        public SimpleInputDialog(string question, string title = "Введення", string defaultAnswer = "")
        {
            InitializeComponent();
            lblQuestion.Text = question;
            Title = title;
            txtAnswer.Text = defaultAnswer;

            // Фокус на полі вводу відразу при відкритті
            Loaded += (s, e) => txtAnswer.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Answer = txtAnswer.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}