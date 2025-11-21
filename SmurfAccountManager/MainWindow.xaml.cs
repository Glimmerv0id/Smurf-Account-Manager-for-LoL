using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;
using SmurfAccountManager.Views;

namespace SmurfAccountManager
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;
        private TooltipWindow? _tooltipWindow;

        public MainWindow()
        {
            InitializeComponent();
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            _config = StorageService.LoadConfig();
            
            // Sync penalties from logs on startup for accounts with AccountId
            foreach (var account in _config.Accounts.Where(a => !string.IsNullOrEmpty(a.AccountId)))
            {
                RiotLogDetectionService.GlobalSyncPunishments(account, _config.RiotClientLogsPath);
                
                // Clear expired penalties
                ClearExpiredPenalties(account);
            }
            
            // Save any penalty updates
            StorageService.SaveConfig(_config);
            
            var sortedAccounts = _config.Accounts.OrderBy(a => a.DisplayOrder).ToList();
            AccountsPanel.ItemsSource = sortedAccounts;
        }
        
        private void ClearExpiredPenalties(Account account)
        {
            var now = DateTime.Now;
            
            // LOW PRIORITY QUEUE: Never clears - stays forever!
            // (No clearing logic for LowPrioUntil)
            
            // QUEUE LOCKOUT: Clears when time is up
            if (account.LockoutUntil.HasValue && account.LockoutUntil.Value <= now)
            {
                account.LockoutUntil = null;
            }
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
            Application.Current.Shutdown();
        }

        private void EditAccountsButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditAccountsWindow(_config);
            editWindow.Owner = this;
            editWindow.ShowDialog();
            LoadAccounts();
        }

        private async void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Account account)
            {
                CloseTooltip();

                try
                {
                    button.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;

                    var password = EncryptionService.Decrypt(account.EncryptedPassword);
                    var success = await LoginService.AutoLogin(account.Username, password, _config.RiotGamesPath, _config.LeagueClientLogsPath);

                    if (success)
                    {
                        // Wait for League Client main window to appear (up to 30 seconds)
                        bool windowAppeared = await ProcessService.WaitForLeagueClientWindow(30);
                        
                        if (!windowAppeared)
                        {
                            MessageBox.Show("League Client window did not appear within 30 seconds.\n\nPlease check if League is starting correctly.", 
                                "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Window appeared! Wait longer for NEW account data to be written to logs
                        // This is critical when an old client was just closed - need to wait for
                        // the NEW account's login data to appear, not just the window
                        await Task.Delay(15000);

                        // 1.1 Spec: Detect account info from League Client JSON logs with polling
                        bool detectionSuccess = false;
                        string debugInfo = "";
                        
                        if (string.IsNullOrEmpty(account.GameName) || string.IsNullOrEmpty(account.AccountId))
                        {
                            // Try detection up to 5 times with 3-second delays (15 seconds total)
                            for (int attempt = 1; attempt <= 5; attempt++)
                            {
                                detectionSuccess = RiotLogDetectionService.DetectAccountInfo(account, _config.LeagueClientLogsPath, out debugInfo);
                                
                                if (detectionSuccess)
                                {
                                    MessageBox.Show($"Detection successful!\n\nAccountId: {account.AccountId}\nGameName: {account.GameName}\nTagLine: {account.TagLine}", 
                                        "Account Info Detected", MessageBoxButton.OK, MessageBoxImage.Information);
                                    break;
                                }
                                
                                // If not last attempt, wait before retrying
                                if (attempt < 5)
                                {
                                    await Task.Delay(3000);
                                }
                            }
                            
                            // If still not successful after all attempts, show debug info
                            if (!detectionSuccess)
                            {
                                MessageBox.Show($"Failed to detect account info after 5 attempts.\n\nDebug Information:\n{debugInfo}", 
                                    "Detection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        // 1.1 Spec: Detect punishments from Riot Client logs (new segment only)
                        RiotLogDetectionService.DetectPunishments(account, _config.RiotClientLogsPath);

                        StorageService.SaveConfig(_config);
                    }
                    else
                    {
                        MessageBox.Show("Failed to start Riot Client. Please check your Riot Games path in Edit Accounts.", 
                            "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    button.IsEnabled = true;
                }
            }
        }

        private void AccountButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Account account)
            {
                CloseTooltip();

                var position = button.PointToScreen(new Point(button.ActualWidth + 5, 0));
                _tooltipWindow = new TooltipWindow(account);
                _tooltipWindow.Left = position.X;
                _tooltipWindow.Top = position.Y;
                _tooltipWindow.Show();
            }
        }

        private void AccountButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseTooltip();
        }

        private void CloseTooltip()
        {
            if (_tooltipWindow != null)
            {
                _tooltipWindow.Close();
                _tooltipWindow = null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            CloseTooltip();
            base.OnClosed(e);
        }
    }
}
