using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;

namespace SmurfAccountManager.Views
{
    public partial class ExportDialog : Window
    {
        private readonly List<Account> _accounts;
        private readonly string _generatedPassword;

        public ExportDialog(List<Account> accounts)
        {
            InitializeComponent();
            _accounts = accounts;
            
            // Generate a secure password
            _generatedPassword = ExportImportService.GenerateExportPassword();
            PasswordTextBox.Text = _generatedPassword;
            
            LoggerService.Info($"[ExportDialog] Export dialog opened for {accounts.Count} accounts");
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_generatedPassword);
                MessageBox.Show("Password copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoggerService.Debug("[ExportDialog] Password copied to clipboard");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoggerService.Error("[ExportDialog] Failed to copy password to clipboard", ex);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"accounts_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportImportService.ExportAccounts(_accounts, saveDialog.FileName, _generatedPassword);
                    
                    MessageBox.Show($"Successfully exported {_accounts.Count} accounts!\n\nFile: {saveDialog.FileName}\n\nDon't forget to save the password!", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LoggerService.Info($"[ExportDialog] Successfully exported {_accounts.Count} accounts to {saveDialog.FileName}");
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoggerService.Error("[ExportDialog] Export failed", ex);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoggerService.Debug("[ExportDialog] Export cancelled by user");
            DialogResult = false;
            Close();
        }
    }
}
