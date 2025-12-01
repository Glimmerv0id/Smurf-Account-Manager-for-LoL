using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmurfAccountManager.Models;
using SmurfAccountManager.Services;

namespace SmurfAccountManager.Views
{
    public partial class EditAccountsWindow : Window
    {
        private AppConfig _config;
        private Point _dragStartPoint;
        private Account? _draggedAccount;
        private bool _isDragging;

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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_config.Accounts.Count == 0)
            {
                MessageBox.Show("No accounts to export.", "Export Accounts", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportDialog = new ExportDialog(_config.Accounts);
            exportDialog.Owner = this;
            exportDialog.ShowDialog();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var importDialog = new ImportDialog();
            importDialog.Owner = this;
            
            if (importDialog.ShowDialog() == true && importDialog.ImportedAccounts.Count > 0)
            {
                // Add imported accounts to existing accounts
                int nextDisplayOrder = _config.Accounts.Count > 0 ? _config.Accounts.Max(a => a.DisplayOrder) + 1 : 0;
                
                foreach (var account in importDialog.ImportedAccounts)
                {
                    account.DisplayOrder = nextDisplayOrder++;
                    _config.Accounts.Add(account);
                }
                
                StorageService.SaveConfig(_config);
                LoadAccounts();
                
                MessageBox.Show($"Added {importDialog.ImportedAccounts.Count} accounts successfully!", 
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Account account)
            {
                var contextMenu = new ContextMenu();
                contextMenu.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                contextMenu.Foreground = System.Windows.Media.Brushes.White;

                // Yellow Star option with colored icon
                var starItem = new MenuItem();
                var starPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var starIcon = new System.Windows.Shapes.Polygon
                {
                    Points = new System.Windows.Media.PointCollection { new Point(10, 0), new Point(12, 7), new Point(20, 7), new Point(14, 12), new Point(16, 20), new Point(10, 15), new Point(4, 20), new Point(6, 12), new Point(0, 7), new Point(8, 7) },
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)),
                    Width = 20,
                    Height = 20,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                starPanel.Children.Add(starIcon);
                starPanel.Children.Add(new TextBlock { Text = "Important", Foreground = System.Windows.Media.Brushes.White });
                starItem.Header = starPanel;
                starItem.Click += (s, args) =>
                {
                    account.Tag = AccountTag.YellowStar;
                    StorageService.SaveConfig(_config);
                    LoadAccounts();
                };
                contextMenu.Items.Add(starItem);

                // Red Circle option with colored icon
                var redItem = new MenuItem();
                var redPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var redCircle = new System.Windows.Shapes.Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 58)),
                    Margin = new Thickness(2, 0, 10, 0)
                };
                redPanel.Children.Add(redCircle);
                redPanel.Children.Add(new TextBlock { Text = "Red Circle", Foreground = System.Windows.Media.Brushes.White });
                redItem.Header = redPanel;
                redItem.Click += (s, args) =>
                {
                    account.Tag = AccountTag.RedCircle;
                    StorageService.SaveConfig(_config);
                    LoadAccounts();
                };
                contextMenu.Items.Add(redItem);

                // Green Circle option with colored icon
                var greenItem = new MenuItem();
                var greenPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var greenCircle = new System.Windows.Shapes.Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 199, 89)),
                    Margin = new Thickness(2, 0, 10, 0)
                };
                greenPanel.Children.Add(greenCircle);
                greenPanel.Children.Add(new TextBlock { Text = "Green Circle", Foreground = System.Windows.Media.Brushes.White });
                greenItem.Header = greenPanel;
                greenItem.Click += (s, args) =>
                {
                    account.Tag = AccountTag.GreenCircle;
                    StorageService.SaveConfig(_config);
                    LoadAccounts();
                };
                contextMenu.Items.Add(greenItem);

                // Remove Tag option
                var removeItem = new MenuItem();
                var removePanel = new StackPanel { Orientation = Orientation.Horizontal };
                var removeIcon = new TextBlock { Text = "✖", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)), Margin = new Thickness(0, 0, 10, 0), Width = 20, TextAlignment = TextAlignment.Center };
                removePanel.Children.Add(removeIcon);
                removePanel.Children.Add(new TextBlock { Text = "Remove Tag", Foreground = System.Windows.Media.Brushes.White });
                removeItem.Header = removePanel;
                removeItem.Click += (s, args) =>
                {
                    account.Tag = AccountTag.None;
                    StorageService.SaveConfig(_config);
                    LoadAccounts();
                };
                contextMenu.Items.Add(removeItem);

                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Account account)
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

        // Drag and Drop Implementation
        private void AccountItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            if (sender is Border border && border.DataContext is Account account)
            {
                _draggedAccount = account;
            }
        }

        private void AccountItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedAccount != null)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is Border border)
                    {
                        _isDragging = true;
                        DragDrop.DoDragDrop(border, _draggedAccount, DragDropEffects.Move);
                        _isDragging = false;
                    }
                }
            }
        }

        private void AccountItem_DragOver(object sender, DragEventArgs e)
        {
            if (sender is Border border && _isDragging)
            {
                // Highlight drop target with bright border and background
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 150, 255));
                border.BorderThickness = new Thickness(2);
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 85, 110));
                
                // Add drop shadow effect
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(100, 150, 255),
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
            }
        }

        private void AccountItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                // Reset visual effects
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
                border.BorderThickness = new Thickness(1);
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                border.Effect = null;

                if (border.DataContext is Account targetAccount && _draggedAccount != null && _draggedAccount != targetAccount)
                {
                    int oldIndex = _config.Accounts.IndexOf(_draggedAccount);
                    int newIndex = _config.Accounts.IndexOf(targetAccount);

                    if (oldIndex >= 0 && newIndex >= 0)
                    {
                        // Remove from old position
                        _config.Accounts.RemoveAt(oldIndex);
                        
                        // Insert at new position
                        if (newIndex > oldIndex)
                            newIndex--;
                        
                        _config.Accounts.Insert(newIndex, _draggedAccount);

                        // Update display orders
                        for (int i = 0; i < _config.Accounts.Count; i++)
                        {
                            _config.Accounts[i].DisplayOrder = i;
                        }

                        StorageService.SaveConfig(_config);
                        LoadAccounts();
                    }
                }

                _draggedAccount = null;
            }
        }
    }
}
