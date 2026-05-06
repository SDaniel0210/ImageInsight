using ImageInsight.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ImageInsight
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private AppSettings _settings;
        private string _SavedTheme;
        private bool _isSaved = false;

        public SettingsPage()
        {
            InitializeComponent();

            _settings = AppSettingsService.Load();

            LoadSettingsToUi();
        }

        private void LoadSettingsToUi()
        {
            _SavedTheme = _settings.Theme;
            ThemeComboBox.SelectedValue = _settings.Theme;

            SaveUsernameCheckBox.IsChecked = _settings.SaveUsername;
            AutoValidationModeCheckBox.IsChecked = _settings.AutoValidationMode;
            AutoStartAiServiceCheckBox.IsChecked = _settings.AutoStartAiService;
            SaveAnalyzedImagesAutomaticallyCheckBox.IsChecked = _settings.SaveAnalyzedImagesAutomatically;

            DefaultBackendPortTextBox.Text = _settings.DefaultBackendPort.ToString();

            ApplyAutoValidationRules();
        }

        private void AutoValidationModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ApplyAutoValidationRules();
        }

        private void ApplyAutoValidationRules()
        {
            bool autoValidation = AutoValidationModeCheckBox.IsChecked == true;

            if (autoValidation)
            {
                SaveAnalyzedImagesAutomaticallyCheckBox.IsChecked = true;
                SaveAnalyzedImagesAutomaticallyCheckBox.IsEnabled = false;
            }
            else
            {
                SaveAnalyzedImagesAutomaticallyCheckBox.IsEnabled = true;
            }
        }

        private void ThemeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var item = ThemeComboBox.SelectedItem as ComboBoxItem;
            if (item == null || item.Tag == null) return;

            string selectedTheme = item.Tag.ToString();

            (Application.Current as App)?.ApplyTheme(selectedTheme);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DefaultBackendPortTextBox.Text.Trim(), out int port))
            {
                MessageBox.Show("Default backend port must be a valid number.");
                return;
            }

            if (port < 1 || port > 65535)
            {
                MessageBox.Show("Default backend port must be between 1 and 65535.");
                return;
            }

            bool autoValidation = AutoValidationModeCheckBox.IsChecked == true;

            _settings.SaveUsername = SaveUsernameCheckBox.IsChecked == true;
            _settings.AutoValidationMode = autoValidation;
            _settings.AutoStartAiService = AutoStartAiServiceCheckBox.IsChecked == true;
            _settings.DefaultBackendPort = port;

            if (autoValidation)
            {
                _settings.SaveAnalyzedImagesAutomatically = true;
            }
            else
            {
                _settings.SaveAnalyzedImagesAutomatically =
                    SaveAnalyzedImagesAutomaticallyCheckBox.IsChecked == true;
            }

            var item = ThemeComboBox.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag != null)
            {
                _settings.Theme = item.Tag.ToString();
            }

            AppSettingsService.Save(_settings);
            _isSaved = true;

            MessageBox.Show("Settings saved.");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_isSaved)
            {
                (Application.Current as App)?.ApplyTheme(_SavedTheme);
            }
        }
    }
}