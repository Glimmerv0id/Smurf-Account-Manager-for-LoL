using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;

namespace SmurfAccountManager.Views
{
    public partial class ImportDialog : Window
    {
        public List<Account> ImportedAccounts { get; private set; }

        public ImportDialog()
        {
            InitializeComponent();
            ImportedAccounts = new List<Account>();
            LoggerService.Info("[ImportDialog] Import dialog opened");
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select Account Export File"
            };

            if (openDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openDialog.FileName;
                LoggerService.Debug($"[ImportDialog] Selected file: {openDialog.FileName}");
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
            {
                MessageBox.Show("Please select a file to import.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please enter the export password.", "No Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ImportedAccounts = ExportImportService.ImportAccounts(FilePathTextBox.Text, PasswordBox.Password);
                
                MessageBox.Show($"Successfully imported {ImportedAccounts.Count} accounts!", 
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoggerService.Info($"[ImportDialog] Successfully imported {ImportedAccounts.Count} accounts from {FilePathTextBox.Text}");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}\n\nPlease check that:\n- The file is valid\n- The password is correct", 
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoggerService.Error("[ImportDialog] Import failed", ex);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoggerService.Debug("[ImportDialog] Import cancelled by user");
            DialogResult = false;
            Close();
        }
    }
}
