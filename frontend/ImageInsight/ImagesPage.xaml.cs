using ImageInsight.Data;
using ImageInsight.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageInsight
{
    public partial class ImagesPage : Page
    {
        private readonly User _currentUser;

        public ImagesPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            Loaded += async (_, _) => await LoadImagesAsync();
        }

        private async Task LoadImagesAsync()
        {
            using var db = new ImageInsightDbContext();

            var imagesFromDb = await db.Images
                .OrderByDescending(i => i.ValidationDate ?? i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    i.ImageUrl,
                    i.FileName,
                    i.SourceUrl,
                    i.SourceType,
                    i.FlorenceCaption,
                    i.RamTagsJson,
                    i.LlmTagsJson,
                    i.FinalTagsJson,
                    i.ValidationMode,
                    i.Status,
                    i.IsValidated,
                    i.ValidationDate,
                    i.CreatedAt,
                    i.ValidatedByUserId
                })
                .ToListAsync();

            var userIds = imagesFromDb
                .Where(i => i.ValidatedByUserId.HasValue)
                .Select(i => i.ValidatedByUserId!.Value)
                .Distinct()
                .ToList();

            var users = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username);

            var displayImages = imagesFromDb
                .Select(i => new ImageDisplayModel
                {
                    Id = i.Id,
                    ThumbnailSource = LoadThumbnail(i.ImageUrl),
                    ImageUrl = i.ImageUrl,
                    FileName = i.FileName,
                    SourceUrl = i.SourceUrl,
                    SourceType = i.SourceType,
                    FlorenceCaption = i.FlorenceCaption,
                    RamTags = ParseTags(i.RamTagsJson),
                    LlmTags = ParseTags(i.LlmTagsJson),
                    FinalTags = ParseTags(i.FinalTagsJson),
                    ValidationMode = i.ValidationMode,
                    Status = i.Status,
                    IsValidated = i.IsValidated,
                    ValidationDate = i.ValidationDate,
                    DbCreatedAt = i.CreatedAt,
                    FileCreatedDate = GetWindowsFileCreatedDate(i.ImageUrl),
                    ValidatedByUsername =
                        i.ValidatedByUserId.HasValue && users.ContainsKey(i.ValidatedByUserId.Value)
                            ? users[i.ValidatedByUserId.Value]
                            : "-"
                })
                .ToList();

            ImagesDataGrid.ItemsSource = displayImages;
        }

        private void ImagesDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.PreviewMouseRightButtonDown += (_, _) =>
            {
                e.Row.IsSelected = true;
                ImagesDataGrid.SelectedItem = e.Row.Item;
                e.Row.Focus();
            };

            var menu = new ContextMenu();

            var openImageItem = new MenuItem { Header = "Open image" };
            openImageItem.Click += OpenImage_Click;

            var openLocationItem = new MenuItem { Header = "Open image location" };
            openLocationItem.Click += OpenImageLocation_Click;

            var detailsItem = new MenuItem { Header = "Details" };
            detailsItem.Click += Details_Click;

            menu.Items.Add(openImageItem);
            menu.Items.Add(openLocationItem);
            menu.Items.Add(detailsItem);

            e.Row.ContextMenu = menu;
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesDataGrid.SelectedItem is not ImageDisplayModel selectedImage)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(selectedImage.ImageUrl))
                {
                    MessageBox.Show("Image URL is empty.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedImage.ImageUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open image:\n{ex.Message}");
            }
        }

        private void OpenImageLocation_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesDataGrid.SelectedItem is not ImageDisplayModel selectedImage)
                return;

            try
            {
                if (IsHttpUrl(selectedImage.ImageUrl))
                {
                    MessageBox.Show("This image is an URL. There is no local folder to open.");
                    return;
                }

                if (!File.Exists(selectedImage.ImageUrl))
                {
                    MessageBox.Show("Local image file was not found.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{selectedImage.ImageUrl}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open image location:\n{ex.Message}");
            }
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesDataGrid.SelectedItem is not ImageDisplayModel selectedImage)
                return;

            var detailsWindow = new ImageDetailsWindow(selectedImage);
            detailsWindow.Owner = Window.GetWindow(this);
            detailsWindow.ShowDialog();
        }
        private BitmapImage? LoadThumbnail(string imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath))
                    return null;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 90;

                if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri))
                {
                    bitmap.UriSource = uri;
                }
                else
                {
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                }

                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? GetWindowsFileCreatedDate(string imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return null;

                if (IsHttpUrl(imageUrl))
                    return null;

                if (!File.Exists(imageUrl))
                    return null;

                return File.GetCreationTime(imageUrl);
            }
            catch
            {
                return null;
            }
        }

        private static List<string> ParseTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static bool IsHttpUrl(string path)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class ImageDisplayModel
    {
        public int Id { get; set; }

        public string ImageUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public string SourceUrl { get; set; } = "";
        public string SourceType { get; set; } = "";

        public string? FlorenceCaption { get; set; }

        public List<string> RamTags { get; set; } = new();
        public List<string> LlmTags { get; set; } = new();
        public List<string> FinalTags { get; set; } = new();

        public string FinalTagsDisplay => FinalTags.Count == 0
            ? "-"
            : string.Join(", ", FinalTags);

        public string ValidationMode { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsValidated { get; set; }

        public DateTime? ValidationDate { get; set; }
        public DateTime DbCreatedAt { get; set; }
        public DateTime? FileCreatedDate { get; set; }

        public string ValidatedByUsername { get; set; } = "-";

        public BitmapImage? ThumbnailSource { get; set; }

        public string ValidationDateDisplay => ValidationDate.HasValue
            ? ValidationDate.Value.ToString("yyyy.MM.dd HH:mm:ss")
            : "-";

        public string FileCreatedDateDisplay => FileCreatedDate.HasValue
            ? FileCreatedDate.Value.ToString("yyyy.MM.dd HH:mm:ss")
            : "-";
    }
}