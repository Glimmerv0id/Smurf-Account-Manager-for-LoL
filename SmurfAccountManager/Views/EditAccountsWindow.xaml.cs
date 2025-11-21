using System.Linq;
using System.Windows;
using System.Windows.Input;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;

namespace SmurfAccountManager.Views
{
    public partial class EditAccountsWindow : Window
    {
        private AppConfig _config;

        public EditAccountsWindow(AppConfig config)
        {
            InitializeComponent();
            _config = config;
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            var sortedAccounts = _config.Accounts.OrderBy(a => a.DisplayOrder).ToList();
            AccountsList.ItemsSource = sortedAccounts;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PathsButton_Click(object sender, RoutedEventArgs e)
        {
            var pathDialog = new PathRequirementDialog(_config.RiotGamesPath);
            pathDialog.Owner = this;
            
            if (pathDialog.ShowDialog() == true)
            {
                _config.RiotGamesPath = pathDialog.RiotGamesPath;
                StorageService.SaveConfig(_config);
            }
        }

        private void NewAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var accountDialog = new AccountDialog(null, _config.Accounts.Count);
            accountDialog.Owner = this;
            
            if (accountDialog.ShowDialog() == true && accountDialog.ResultAccount != null)
            {
                _config.Accounts.Add(accountDialog.ResultAccount);
                StorageService.SaveConfig(_config);
                LoadAccounts();
            }
        }

        private void EditAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Account account)
            {
                var accountDialog = new AccountDialog(account, account.DisplayOrder);
                accountDialog.Owner = this;
                
                if (accountDialog.ShowDialog() == true && accountDialog.ResultAccount != null)
                {
                    var index = _config.Accounts.IndexOf(account);
                    if (index >= 0)
                    {
                        _config.Accounts[index] = accountDialog.ResultAccount;
                        StorageService.SaveConfig(_config);
                        LoadAccounts();
                    }
                }
            }
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Account account)
            {
                var result = MessageBox.Show(
                    $"Delete account '{account.Username}'?",
                    "Delete Account",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _config.Accounts.Remove(account);
                    
                    // Reorder remaining accounts
                    for (int i = 0; i < _config.Accounts.Count; i++)
                    {
                        _config.Accounts[i].DisplayOrder = i;
                    }
                    
                    StorageService.SaveConfig(_config);
                    LoadAccounts();
                }
            }
        }
    }
}
