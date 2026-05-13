using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageInsight
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string title, bool isQuestion = false)
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;
            this.Title = title;
            MessageTextBlock.Text = message;

            if (!isQuestion)
            {
                NoButton.Visibility = Visibility.Collapsed;
                YesButton.Content = "OK";
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public static bool Show(string message, string title = "Information", bool isQuestion = false)
        {
            var msg = new CustomMessageBox(message, title, isQuestion);
            return msg.ShowDialog() ?? false;
        }
    }
}
