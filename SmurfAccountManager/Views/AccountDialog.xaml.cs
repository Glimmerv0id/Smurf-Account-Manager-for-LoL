using System.Windows;
using System.Windows.Input;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;

namespace SmurfAccountManager.Views
{
    public partial class AccountDialog : Window
    {
        private Account? _originalAccount;
        private int _displayOrder;

        public Account? ResultAccount { get; private set; }

        public AccountDialog(Account? account, int displayOrder)
        {
            InitializeComponent();
            _originalAccount = account;
            _displayOrder = displayOrder;

            if (account != null)
            {
                UsernameTextBox.Text = account.Username;
                var password = EncryptionService.Decrypt(account.EncryptedPassword);
                PasswordBox.Password = password;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Password cannot be empty.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultAccount = new Account
            {
                Id = _originalAccount?.Id ?? System.Guid.NewGuid().ToString(),
                Username = UsernameTextBox.Text.Trim(),
                EncryptedPassword = EncryptionService.Encrypt(PasswordBox.Password),
                AccountId = _originalAccount?.AccountId ?? string.Empty,
                GameName = _originalAccount?.GameName ?? string.Empty,
                TagLine = _originalAccount?.TagLine ?? string.Empty,
                LowPrioUntil = _originalAccount?.LowPrioUntil,
                LockoutUntil = _originalAccount?.LockoutUntil,
                DisplayOrder = _displayOrder
            };

            DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
