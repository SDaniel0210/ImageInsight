using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageInsight
{
    /// <summary>
    /// Interaction logic for DatabasePage.xaml
    /// </summary>

    // temporary
    public class ImageRecord
    {
        public string FileName { get; set; }
        public string Url { get; set; }
        public string Tags { get; set; }
    }


    public partial class DatabasePage : Page
    {
        public ObservableCollection<ImageRecord> Records { get; set; }

        public DatabasePage()
        {
            InitializeComponent();

            Records = new ObservableCollection<ImageRecord>
            {
                new ImageRecord { FileName = "photo.png", Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTc9APxkj0xClmrU3PpMZglHQkx446nQPG6lA&s", Tags = "photo, road, bridge" },
                new ImageRecord { FileName = "flower.jpg", Url = "https://cdn.pixabay.com/photo/2015/04/19/08/32/flower-729510_1280.jpg", Tags = "flower, white, yellow" }
            };

            ImagesDataGrid.ItemsSource = Records;
        }

        private void PopUpImage(object sender, MouseButtonEventArgs e)
        {
            if (ImagesDataGrid.SelectedItem is ImageRecord record)
            {
                try
                {
                    PopupImage.Source = new BitmapImage(new Uri(record.Url));
                    ImagePopup.Visibility = Visibility.Visible;
                }
                catch
                {
                    MessageBox.Show("Image cannot be loaded.");
                }
            }
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            ImagePopup.Visibility = Visibility.Collapsed;
            PopupImage.Source = null;
        }
    }
}
