using System.Windows;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;

namespace SmurfAccountManager.Views
{
    public partial class TooltipWindow : Window
    {
        public TooltipWindow(Account account)
        {
            InitializeComponent();

            UsernameText.Text = $"username: {account.Username}";
            
            var password = EncryptionService.Decrypt(account.EncryptedPassword);
            PasswordText.Text = $"password: {new string('*', password.Length)}";

            // 1.1 Spec: Show Riot ID (gameName#tagLine)
            var riotId = account.FullRiotId;
            if (!string.IsNullOrEmpty(riotId))
            {
                SummonerNameText.Text = $"riot id: {riotId}";
                SummonerNameText.Visibility = Visibility.Visible;
            }
            else
            {
                SummonerNameText.Visibility = Visibility.Collapsed;
            }

            // 1.1 Spec: Show queue lockout remaining time
            var queueLockout = account.QueueLockoutRemaining;
            if (!string.IsNullOrEmpty(queueLockout))
            {
                QueueLockoutText.Text = $"queue lockout: {queueLockout}";
                QueueLockoutText.Visibility = Visibility.Visible;
            }
            else
            {
                QueueLockoutText.Visibility = Visibility.Collapsed;
            }

            // 1.1 Spec: Show low priority queue minutes
            if (account.LowPriorityMinutes.HasValue && account.LowPriorityMinutes.Value > 0)
            {
                LowPriorityText.Text = $"low priority queue: {account.LowPriorityMinutes.Value} minutes";
                LowPriorityText.Visibility = Visibility.Visible;
            }
            else
            {
                LowPriorityText.Visibility = Visibility.Collapsed;
            }
        }
    }
}
