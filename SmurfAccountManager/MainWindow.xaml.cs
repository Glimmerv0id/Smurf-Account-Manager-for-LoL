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
            LoggerService.Info("========================================");
            LoggerService.Info($"[App] Smurf Account Manager started");
            LoggerService.Info($"[App] Log directory: {LoggerService.GetLogDirectory()}");
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            LoggerService.Debug("[App] Loading configuration...");
            _config = StorageService.LoadConfig();
            LoggerService.Info($"[App] Loaded {_config.Accounts.Count} accounts");
            
            // Clear expired penalties
            foreach (var account in _config.Accounts)
            {
                ClearExpiredPenalties(account);
            }
            
            // Detect punishments from recent logs on startup
            LoggerService.Debug("[App] Running punishment detection on startup...");
            RiotLogDetectionService.DetectPunishments(_config.Accounts, _config.RiotClientLogsPath);
            
            // Save any penalty updates
            StorageService.SaveConfig(_config);
            
            var sortedAccounts = _config.Accounts.OrderBy(a => a.DisplayOrder).ToList();
            AccountsPanel.ItemsSource = sortedAccounts;
        }
        
        private void ClearExpiredPenalties(Account account)
        {
            var now = DateTime.Now;
            
            // LOW PRIORITY QUEUE: Never auto-clears
            // It only counts down when actively queuing (which we don't track)
            // So we leave LowPriorityMinutes unchanged
            
            // QUEUE LOCKOUT: Auto-clears when expired (always counting down)
            if (account.LockoutUntil.HasValue && account.LockoutUntil.Value <= now)
            {
                account.LockoutUntil = null;
            }
        }

        private void RefreshAccountDisplay()
        {
            // Refresh the account list display to show updated punishment data
            var sortedAccounts = _config.Accounts.OrderBy(a => a.DisplayOrder).ToList();
            AccountsPanel.ItemsSource = null; // Clear first
            AccountsPanel.ItemsSource = sortedAccounts; // Re-bind
            LoggerService.Debug("[UI] Account display refreshed");
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
                LoggerService.Info($"========================================");
                LoggerService.Info($"[Login] Starting login for account: {account.Username}");

                try
                {
                    button.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;

                    var password = EncryptionService.Decrypt(account.EncryptedPassword);
                    LoggerService.Debug($"[Login] Password decrypted successfully");
                    
                    var success = await LoginService.AutoLogin(account.Username, password, _config.RiotGamesPath, _config.LeagueClientLogsPath);

                    if (success)
                    {
                        LoggerService.Info($"[Login] AutoLogin succeeded, waiting for League Client window...");
                        
                        // Wait for League Client main window to appear (up to 30 seconds)
                        bool windowAppeared = await ProcessService.WaitForLeagueClientWindow(30);
                        
                        if (!windowAppeared)
                        {
                            LoggerService.Warning($"[Login] League Client window did not appear within 30 seconds");
                            MessageBox.Show("League Client window did not appear within 30 seconds.\n\nPlease check if League is starting correctly.", 
                                "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        LoggerService.Info($"[Login] League Client window appeared! Waiting 15 seconds for data to be written...");
                        
                        // Window appeared! Wait longer for NEW account data to be written to logs
                        // This is critical when an old client was just closed - need to wait for
                        // the NEW account's login data to appear, not just the window
                        await Task.Delay(15000);

                        // 1.1 Spec: Detect account info from League Client JSON logs with polling
                        bool detectionSuccess = false;
                        string debugInfo = "";
                        
                        if (string.IsNullOrEmpty(account.GameName) || string.IsNullOrEmpty(account.AccountId))
                        {
                            LoggerService.Info($"[Login] Account info missing, starting detection with 5 retry attempts...");
                            
                            // Try detection up to 5 times with 3-second delays (15 seconds total)
                            for (int attempt = 1; attempt <= 5; attempt++)
                            {
                                LoggerService.Debug($"[Login] Detection attempt {attempt}/5");
                                detectionSuccess = RiotLogDetectionService.DetectAccountInfo(account, _config.LeagueClientLogsPath, out debugInfo);
                                
                                if (detectionSuccess)
                                {
                                    LoggerService.Info($"[Login] ✓ Account detection successful on attempt {attempt}");
                                    MessageBox.Show($"Detection successful!\n\nAccountId: {account.AccountId}\nGameName: {account.GameName}\nTagLine: {account.TagLine}", 
                                        "Account Info Detected", MessageBoxButton.OK, MessageBoxImage.Information);
                                    break;
                                }
                                
                                // If not last attempt, wait before retrying
                                if (attempt < 5)
                                {
                                    LoggerService.Debug($"[Login] Detection failed on attempt {attempt}, waiting 3 seconds before retry...");
                                    await Task.Delay(3000);
                                }
                            }
                            
                            // If still not successful after all attempts, show debug info
                            if (!detectionSuccess)
                            {
                                LoggerService.Error($"[Login] Account detection failed after all 5 attempts");
                                LoggerService.Debug($"[Login] Debug info: {debugInfo}");
                                MessageBox.Show($"Failed to detect account info after 5 attempts.\n\nDebug Information:\n{debugInfo}\n\nCheck logs at:\n{LoggerService.GetLogDirectory()}", 
                                    "Detection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            LoggerService.Info($"[Login] Account info already exists, skipping detection");
                        }

                        // 1.1 Spec: Detect punishments from Riot Client logs for all accounts
                        LoggerService.Debug($"[Login] Checking for punishment events across all accounts...");
                        RiotLogDetectionService.DetectPunishments(_config.Accounts, _config.RiotClientLogsPath);
                        
                        // Log if the current account has punishments
                        if (account.LowPriorityMinutes.HasValue)
                            LoggerService.Info($"[Login] Low Priority Queue detected for {account.Username}: {account.LowPriorityMinutes} minutes");
                        if (account.LockoutUntil.HasValue)
                            LoggerService.Info($"[Login] Queue Lockout detected for {account.Username} until: {account.LockoutUntil}");

                        StorageService.SaveConfig(_config);
                        
                        // Refresh UI to show updated punishment data
                        RefreshAccountDisplay();
                        
                        LoggerService.Info($"[Login] Login process completed successfully");
                    }
                    else
                    {
                        LoggerService.Error($"[Login] AutoLogin failed - Riot Client did not start");
                        MessageBox.Show("Failed to start Riot Client. Please check your Riot Games path in Edit Accounts.", 
                            "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"[Login] Exception during login process", ex);
                    MessageBox.Show($"An error occurred: {ex.Message}\n\nCheck logs at:\n{LoggerService.GetLogDirectory()}", "Error", 
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
