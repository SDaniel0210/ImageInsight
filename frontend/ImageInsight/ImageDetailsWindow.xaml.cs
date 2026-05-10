using System.Windows;

namespace ImageInsight
{
    public partial class ImageDetailsWindow : Window
    {
        private readonly ImageDisplayModel _image;

        public ImageDetailsWindow(ImageDisplayModel image)
        {
            InitializeComponent();

            _image = image;

            LoadDetails();
        }

        private void LoadDetails()
        {
            ImageUrlTextBlock.Text = _image.ImageUrl;
            FileNameTextBlock.Text = _image.FileName;
            SourceUrlTextBlock.Text = _image.SourceUrl;
            SourceTypeTextBlock.Text = _image.SourceType;

            CaptionTextBlock.Text = string.IsNullOrWhiteSpace(_image.FlorenceCaption)
                ? "-"
                : _image.FlorenceCaption;

            RamTagsListBox.ItemsSource = _image.RamTags;
            LlmTagsListBox.ItemsSource = _image.LlmTags;
            FinalTagsListBox.ItemsSource = _image.FinalTags;

            StatusTextBlock.Text = _image.Status;
            IsValidatedTextBlock.Text = _image.IsValidated.ToString();
            ValidationModeTextBlock.Text = _image.ValidationMode;
            ValidationDateTextBlock.Text = _image.ValidationDateDisplay;
            ValidatedByTextBlock.Text = _image.ValidatedByUsername;
            DbCreatedAtTextBlock.Text = _image.DbCreatedAt.ToString("yyyy.MM.dd HH:mm:ss");
            FileCreatedDateTextBlock.Text = _image.FileCreatedDateDisplay;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}