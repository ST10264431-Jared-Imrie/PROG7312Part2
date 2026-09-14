using System;
using System.Windows;

namespace SmartX.Dashboard
{
    public partial class LoginWindow : Window
    {
        // Pre-configured dummy credentials
        private const string DummyUser1 = "admin@smartx.io";
        private const string DummyUser2 = "admin";
        private const string DummyPass1 = "AdminPassword123!";
        private const string DummyPass2 = "admin123";

        public LoginWindow()
        {
            InitializeComponent();
            // Pre-fill password box with default dummy password for convenience
            PasswordBox.Password = DummyPass1;
        }

        /// <summary>
        /// Auto-fills the credentials inputs with valid demo account credentials.
        /// </summary>
        private void AutoFill_Click(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Text = DummyUser1;
            PasswordBox.Password = DummyPass1;
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Validates login input against demo credentials and launches MainWindow console.
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredUser = UsernameTextBox.Text.Trim();
            string enteredPass = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(enteredUser) || string.IsNullOrWhiteSpace(enteredPass))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            bool isValidUser = string.Equals(enteredUser, DummyUser1, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(enteredUser, DummyUser2, StringComparison.OrdinalIgnoreCase);

            bool isValidPass = string.Equals(enteredPass, DummyPass1, StringComparison.Ordinal) ||
                              string.Equals(enteredPass, DummyPass2, StringComparison.Ordinal);

            if (isValidUser && isValidPass)
            {
                // Launch Main Dashboard Console Window
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Close login window
                this.Close();
            }
            else
            {
                ShowError("Invalid credentials! Please use Username: admin@smartx.io and Password: AdminPassword123!");
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }
    }
}
