using System.Windows;
using System.Windows.Input;

namespace SmurfAccountManager.Views
{
    public partial class PathRequirementDialog : Window
    {
        public string RiotGamesPath { get; private set; }

        public PathRequirementDialog(string currentPath)
        {
            InitializeComponent();
            RiotGamesPath = currentPath;
            PathTextBox.Text = currentPath;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            RiotGamesPath = PathTextBox.Text;
            DialogResult = true;
            this.Close();
        }
    }
}
