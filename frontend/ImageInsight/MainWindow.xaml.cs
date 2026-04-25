using ImageInsight.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageInsight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string CurrentImagePath { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            using var db = new ImageInsightDbContext();

            var userCount = db.Users.Count();

            MessageBox.Show($"DB connection OK. Users: {userCount}");
            MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
        }

        // Window functions
        private void CloseApp(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ToggleMaximizeRestore(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // Image functions

        private void ShowImageButtons (bool show)
        {
            if (show)
            {
                DeleteImageButton.Visibility = Visibility.Visible;
                AnalyzeButton.Visibility = Visibility.Visible;
                UploadButton.Visibility = Visibility.Visible;
            }
            else
            {
                DeleteImageButton.Visibility = Visibility.Collapsed;
                AnalyzeButton.Visibility = Visibility.Collapsed;
                UploadButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UploadImage(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (dialog.ShowDialog() == true)
            {
                ShowImage(dialog.FileName);
            }
        }

        private void DeleteImage(object sender, RoutedEventArgs e)
        {
            DisplayedImage.Source = null;
            DisplayedImage.Visibility = Visibility.Collapsed;
            PlaceholderPanel.Visibility = Visibility.Visible;
            ShowImageButtons(false);
        }

        private void ShowImage(string path)
        {
            CurrentImagePath = path;

            DisplayedImage.Source = new BitmapImage(new Uri(CurrentImagePath));
            DisplayedImage.Visibility = Visibility.Visible;
            PlaceholderPanel.Visibility = Visibility.Collapsed;
            ShowImageButtons(true);
        }


        private void ImageDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void ImageDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    ShowImage(files[0]);
                }
            }
        }

        // Analyze

        private async void AnalyzeImage(object sender, RoutedEventArgs e)
        {
            if (ResultPopup.IsVisible) ResultPopup.Visibility = Visibility.Collapsed;
            LoadingOverlay.Visibility = Visibility.Visible;

            await Task.Delay(5000); // backend

            LoadingOverlay.Visibility = Visibility.Collapsed;

            ShowResultPopup("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", "cat, cute, animal");
        }

        private void ValidateResult(object sender, RoutedEventArgs e)
        {

        }

        //private void RevalidateResult(object sender, RoutedEventArgs e)
        //{

        //}

        private void ShowResultPopup(string description, string tags)
        {
            AnalyzedImage.Source = new BitmapImage(new Uri(CurrentImagePath));
            ResultDescription.Text = description;
            ResultTags.Text = "Tags: " + tags;

            ResultPopup.Visibility = Visibility.Visible;
        }

        private void RejectResult(object sender, RoutedEventArgs e)
        {
            ResultPopup.Visibility = Visibility.Collapsed;
        }


        // Navigation functions
        private void GoToImagesPage(object sender, RoutedEventArgs e)
        {
            ImageGrid.Visibility = Visibility.Collapsed;
            MainFrame.Navigate(new DatabasePage());
            if (DisplayedImage.Source != null) ShowImageButtons(false);
        }

        private void GoToHomePage(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = null;
            ImageGrid.Visibility = Visibility.Visible;
            if (DisplayedImage.Source != null) ShowImageButtons(true);
        }
    }
}