using ImageInsight.Data;
using ImageInsight.Models;
using ImageInsight.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageInsight
{
    public partial class ValidationPage : Page
    {
        private readonly User _currentUser;

        private readonly List<string> _imagePaths = new();
        private int _currentIndex = -1;

        private AnalyzeResponse? _lastAnalyzeResult;
        private bool _isValidationRunning = false;

        private readonly string[] _supportedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".webp"
        };

        public ValidationPage(User currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;

            Loaded += (_, _) =>
            {
                ApplySettings();
                RestoreRuntimeState();
            };
        }

        private void ApplySettings()
        {
            var settings = AppSettingsService.Load();

            ValidationModeTextBlock.Text = settings.AutoValidationMode ? "Auto" : "Manual";

            bool isAuto = settings.AutoValidationMode;

            AcceptButton.IsEnabled = !isAuto;
            DeclineButton.IsEnabled = !isAuto;
            PreviousButton.IsEnabled = !isAuto;
            NextButton.IsEnabled = !isAuto;
        }

        private void LoadImages_Click(object sender, RoutedEventArgs e)
        {
            string source = SourcePathTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(source))
            {
                ShowFatalError("Please enter a source path or URL.");
                return;
            }

            try
            {
                _imagePaths.Clear();
                _currentIndex = -1;
                _lastAnalyzeResult = null;

                if (Directory.Exists(source))
                {
                    var files = Directory.GetFiles(source)
                        .Where(IsSupportedImage)
                        .ToList();

                    _imagePaths.AddRange(files);
                }
                else if (File.Exists(source))
                {
                    if (!IsSupportedImage(source))
                    {
                        ShowFatalError("The selected file is not a supported image format.");
                        return;
                    }

                    _imagePaths.Add(source);
                }
                else if (IsHttpUrl(source))
                {
                    var uri = new Uri(source);

                    if (!IsSupportedImage(uri.AbsolutePath))
                    {
                        ShowFatalError("Currently only direct image URLs are supported here. Folder/website URL scanning can be added next.");
                        return;
                    }

                    _imagePaths.Add(source);
                }
                else
                {
                    ShowFatalError("Source path or URL was not found.");
                    return;
                }

                if (_imagePaths.Count == 0)
                {
                    ShowFatalError("No supported images were found.");
                    return;
                }

                _currentIndex = 0;
                LoadCurrentImage();

                LastMessageTextBlock.Text = "Images loaded.";
                StatusTextBlock.Text = "Status: Images loaded";
                SaveRuntimeState();
            }
            catch (Exception ex)
            {
                ShowFatalError($"Load images error: {ex.Message}");
            }
        }

        private async void StartValidation_Click(object sender, RoutedEventArgs e)
        {
            if (_imagePaths.Count == 0)
            {
                ShowFatalError("No images loaded.");
                return;
            }

            if (_isValidationRunning)
            {
                LastMessageTextBlock.Text = "Validation is already running.";
                return;
            }

            var settings = AppSettingsService.Load();

            _isValidationRunning = true;
            StatusTextBlock.Text = "Status: Running";

            try
            {
                if (settings.AutoValidationMode)
                {
                    await RunAutoValidationAsync();
                }
                else
                {
                    await AnalyzeCurrentImageAsync();
                }
            }
            catch (Exception ex)
            {
                ShowFatalError($"Validation error: {ex.Message}");
            }
            finally
            {
                _isValidationRunning = false;

                if (StatusTextBlock.Text != "Status: Finished" &&
                    StatusTextBlock.Text != "Status: Error" &&
                    StatusTextBlock.Text != "Status: Stopped")
                {
                    StatusTextBlock.Text = "Status: Idle";
                }

                SaveRuntimeState();
            }
        }

        private void StopValidation_Click(object sender, RoutedEventArgs e)
        {
            _isValidationRunning = false;
            StatusTextBlock.Text = "Status: Stopped";
            LastMessageTextBlock.Text = "Validation stopped.";
            SaveRuntimeState();
        }

        private async Task RunAutoValidationAsync()
        {
            while (_isValidationRunning)
            {
                if (_imagePaths.Count == 0)
                {
                    LastMessageTextBlock.Text = "No images loaded.";
                    StatusTextBlock.Text = "Status: Idle";
                    _isValidationRunning = false;
                    SaveRuntimeState();
                    return;
                }

                if (_currentIndex < 0)
                {
                    _currentIndex = 0;
                }

                if (_currentIndex >= _imagePaths.Count)
                {
                    _currentIndex = _imagePaths.Count - 1;
                    LastMessageTextBlock.Text = "Auto validation finished.";
                    StatusTextBlock.Text = "Status: Finished";
                    _isValidationRunning = false;
                    RefreshUi();
                    SaveRuntimeState();
                    return;
                }

                LoadCurrentImage();

                await AnalyzeCurrentImageAsync();

                if (_lastAnalyzeResult != null)
                {
                    await SaveCurrentImageAsync(isAccepted: true);
                    IsValidatedTextBlock.Text = "True";
                }

                bool hasNextImage = _currentIndex < _imagePaths.Count - 1;

                if (!hasNextImage)
                {
                    LastMessageTextBlock.Text = "Auto validation finished.";
                    StatusTextBlock.Text = "Status: Finished";
                    _isValidationRunning = false;
                    RefreshUi();
                    SaveRuntimeState();
                    return;
                }

                LastMessageTextBlock.Text = "Auto saved. Waiting 5 seconds...";
                SaveRuntimeState();

                await Task.Delay(5000);

                if (!_isValidationRunning)
                {
                    LastMessageTextBlock.Text = "Validation stopped.";
                    StatusTextBlock.Text = "Status: Stopped";
                    SaveRuntimeState();
                    return;
                }

                _currentIndex++;
                SaveRuntimeState();
            }
        }

        private async Task AnalyzeCurrentImageAsync()
        {
            if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
                return;

            string imagePath = _imagePaths[_currentIndex];

            StatusTextBlock.Text = "Status: Analyzing";
            LastMessageTextBlock.Text = "Analyzing image...";
            TagsListBox.ItemsSource = null;
            _lastAnalyzeResult = null;

            string localImagePath = imagePath;

            if (IsHttpUrl(imagePath))
            {
                var uri = new Uri(imagePath);
                localImagePath = await DownloadImageToTempAsync(uri);
            }

            if (!AppRuntime.IsBackendRunning || AppRuntime.CurrentBackendPort == null)
            {
                throw new Exception("AI service is not running. Start the AI service first from the Home page.");
            }

            int port = AppRuntime.CurrentBackendPort.Value;

            string url = $"http://127.0.0.1:{port}/analyze";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            var request = new AnalyzeRequest
            {
                image_path = localImagePath
            };

            string json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Backend returned error: {response.StatusCode}\n{responseBody}");
            }

            var result = JsonSerializer.Deserialize<AnalyzeResponse>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
            {
                throw new Exception("Backend response could not be parsed.");
            }

            if (!string.IsNullOrWhiteSpace(result.error))
            {
                throw new Exception(result.error);
            }

            _lastAnalyzeResult = result;

            TagsListBox.ItemsSource = result.final_tags ?? new List<string>();

            LastMessageTextBlock.Text = "Analysis finished.";
            StatusTextBlock.Text = "Status: Analyzed";
            SaveRuntimeState();
        }

        private async void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (_lastAnalyzeResult == null)
            {
                MessageBox.Show("Analyze the image first.");
                return;
            }

            await SaveCurrentImageAsync(isAccepted: true);

            IsValidatedTextBlock.Text = "True";
            LastMessageTextBlock.Text = "Image accepted and saved.";

            SaveRuntimeState();
            MoveNext();
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            IsValidatedTextBlock.Text = "False";
            LastMessageTextBlock.Text = "Image declined.";

            SaveRuntimeState();
            MoveNext();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (_imagePaths.Count == 0)
            {
                _currentIndex = -1;
                LastMessageTextBlock.Text = "No images loaded.";
                RefreshUi();
                SaveRuntimeState();
                return;
            }

            if (_currentIndex <= 0)
            {
                _currentIndex = _imagePaths.Count - 1; // elsőről utolsóra
                LastMessageTextBlock.Text = "Reached start of list. Jumped to last image.";
            }
            else
            {
                _currentIndex--;
            }

            LoadCurrentImage();
            SaveRuntimeState();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            MoveNext();
        }

        private void MoveNext()
        {
            if (_imagePaths.Count == 0)
            {
                _currentIndex = -1;
                LastMessageTextBlock.Text = "No images loaded.";
                RefreshUi();
                SaveRuntimeState();
                return;
            }

            if (_currentIndex < 0)
            {
                _currentIndex = 0;
            }
            else if (_currentIndex >= _imagePaths.Count - 1)
            {
                _currentIndex = 0;
                LastMessageTextBlock.Text = "Reached end of list. Jumped to first image.";
            }
            else
            {
                _currentIndex++;
            }

            LoadCurrentImage();
            SaveRuntimeState();
        }

        private async Task SaveCurrentImageAsync(bool isAccepted)
        {
            if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
                return;

            if (_lastAnalyzeResult == null)
                return;

            string imagePath = _imagePaths[_currentIndex];
            string fileName = GetFileNameFromPathOrUrl(imagePath);

            using var db = new ImageInsightDbContext();

            var image = await db.Images
                .FirstOrDefaultAsync(i => i.FileName == fileName);

            if (image == null)
            {
                image = new ImageInsight.Models.Image
                {
                    CreatedAt = DateTime.Now
                };

                db.Images.Add(image);
            }

            image.ImageUrl = imagePath;
            image.FileName = fileName;
            image.SourceUrl = SourcePathTextBox.Text.Trim();
            image.SourceType = IsHttpUrl(imagePath) ? "url" : "local";
            image.FlorenceCaption = _lastAnalyzeResult.caption;
            image.RamTagsJson = JsonSerializer.Serialize(_lastAnalyzeResult.ram_tags ?? new List<string>());
            image.LlmTagsJson = JsonSerializer.Serialize(_lastAnalyzeResult.llm_tags ?? new List<string>());
            image.FinalTagsJson = JsonSerializer.Serialize(_lastAnalyzeResult.final_tags ?? new List<string>());
            image.ValidationMode = ValidationModeTextBlock.Text.ToLower();
            image.Status = isAccepted ? "accepted" : "declined";
            image.IsValidated = isAccepted;
            image.ValidationDate = DateTime.Now;
            image.ValidatedByUserId = _currentUser.Id;

            await db.SaveChangesAsync();
        }

        private void LoadCurrentImage(bool clearAnalysis = true)
        {
            if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
                return;

            string path = _imagePaths[_currentIndex];

            try
            {
                PreviewImage.Source = null;
                DropHintTextBlock.Visibility = Visibility.Collapsed;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                if (IsHttpUrl(path))
                {
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                }
                else
                {
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                }

                bitmap.EndInit();
                bitmap.Freeze();

                PreviewImage.Source = bitmap;

                if (clearAnalysis)
                {
                    _lastAnalyzeResult = null;
                    TagsListBox.ItemsSource = null;
                }

                RefreshUi();
                LoadExistingValidationFromDatabase();
                SaveRuntimeState();
            }
            catch (Exception ex)
            {
                ShowFatalError($"Image preview error: {ex.Message}");
            }
        }

        private void RefreshUi()
        {
            LocatedImagesTextBlock.Text = _imagePaths.Count.ToString();

            if (_currentIndex >= 0 && _imagePaths.Count > 0)
            {
                CurrentImageTextBlock.Text = $"{_currentIndex + 1} / {_imagePaths.Count}";
                CurrentFileNameTextBlock.Text = GetFileNameFromPathOrUrl(_imagePaths[_currentIndex]);

                LoadValidationStatusFromDatabase();
            }
            else
            {
                CurrentImageTextBlock.Text = "0 / 0";
                CurrentFileNameTextBlock.Text = "-";
            }
        }

        private void ImageDropArea_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void ImageDropArea_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 1)
            {
                MessageBox.Show("Bulk drag & drop is not supported here. Use the Source path / URL field instead.");
                return;
            }

            string file = files[0];

            if (!File.Exists(file))
            {
                MessageBox.Show("Only single image files can be dropped here.");
                return;
            }

            if (!IsSupportedImage(file))
            {
                MessageBox.Show("Unsupported image format.");
                return;
            }

            _imagePaths.Clear();
            _imagePaths.Add(file);
            _currentIndex = 0;
            SourcePathTextBox.Text = file;

            LoadCurrentImage();

            LastMessageTextBlock.Text = "Single image loaded.";
            StatusTextBlock.Text = "Status: Image loaded";
            SaveRuntimeState();
        }

        private bool IsSupportedImage(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return _supportedExtensions.Contains(ext);
        }

        private string GetFileNameFromPathOrUrl(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
                return Path.GetFileName(uri.LocalPath);

            return Path.GetFileName(path);
        }

        private async Task<string> DownloadImageToTempAsync(Uri uri)
        {
            using var client = new HttpClient();

            byte[] bytes = await client.GetByteArrayAsync(uri);

            string extension = Path.GetExtension(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                $"imageinsight_{Guid.NewGuid()}{extension}");

            await File.WriteAllBytesAsync(tempFile, bytes);

            return tempFile;
        }

        private void ShowFatalError(string message)
        {
            _isValidationRunning = false;

            StatusTextBlock.Text = "Status: Error";
            LastMessageTextBlock.Text = message;
            SaveRuntimeState();
            MessageBox.Show(
                message,
                "Validation error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

        }
        private void LoadValidationStatusFromDatabase()
        {
            if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
            {
                IsValidatedTextBlock.Text = "False";
                return;
            }

            string imagePath = _imagePaths[_currentIndex];
            string fileName = GetFileNameFromPathOrUrl(imagePath);

            using var db = new ImageInsightDbContext();

            var existingImage = db.Images
                .Where(i => i.ImageUrl == imagePath || i.FileName == fileName)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();

            if (existingImage == null)
            {
                IsValidatedTextBlock.Text = "False";
                return;
            }

            IsValidatedTextBlock.Text = existingImage.IsValidated ? "True" : "False";
        }

        private void LoadExistingValidationFromDatabase()
        {
            if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
                return;

            string imagePath = _imagePaths[_currentIndex];
            string fileName = GetFileNameFromPathOrUrl(imagePath);

            using var db = new ImageInsightDbContext();

            var existingImage = db.Images
                .AsNoTracking()
                .Where(i => i.FileName == fileName)
                .OrderByDescending(i => i.ValidationDate)
                .FirstOrDefault();

            if (existingImage == null)
            {
                IsValidatedTextBlock.Text = "False";
                TagsListBox.ItemsSource = null;

                if (_lastAnalyzeResult == null)
                    LastMessageTextBlock.Text = "No previous validation found.";

                return;
            }

            IsValidatedTextBlock.Text = existingImage.IsValidated ? "True" : "False";

            List<string> finalTags = new();
            List<string> ramTags = new();
            List<string> llmTags = new();

            if (!string.IsNullOrWhiteSpace(existingImage.FinalTagsJson))
            {
                finalTags = JsonSerializer.Deserialize<List<string>>(existingImage.FinalTagsJson) ?? new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(existingImage.RamTagsJson))
            {
                ramTags = JsonSerializer.Deserialize<List<string>>(existingImage.RamTagsJson) ?? new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(existingImage.LlmTagsJson))
            {
                llmTags = JsonSerializer.Deserialize<List<string>>(existingImage.LlmTagsJson) ?? new List<string>();
            }

            if (existingImage.IsValidated)
            {
                TagsListBox.ItemsSource = finalTags;

                _lastAnalyzeResult = new AnalyzeResponse
                {
                    caption = existingImage.FlorenceCaption,
                    ram_tags = ramTags,
                    llm_tags = llmTags,
                    final_tags = finalTags
                };

                LastMessageTextBlock.Text = "Existing validation loaded from database.";
            }
            else
            {
                TagsListBox.ItemsSource = null;
                _lastAnalyzeResult = null;
                LastMessageTextBlock.Text = "Image exists in database but is not validated.";
            }
        }

        private bool IsHttpUrl(string path)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private void SaveRuntimeState()
        {
            AppRuntime.ValidationState.SourcePath = SourcePathTextBox.Text.Trim();
            AppRuntime.ValidationState.ImagePaths = new List<string>(_imagePaths);
            AppRuntime.ValidationState.CurrentIndex = _currentIndex;
            AppRuntime.ValidationState.LastAnalyzeResult = _lastAnalyzeResult;
            AppRuntime.ValidationState.LastMessage = LastMessageTextBlock.Text;
            AppRuntime.ValidationState.StatusText = StatusTextBlock.Text;
            AppRuntime.ValidationState.IsValidatedText = IsValidatedTextBlock.Text;
        }
        private void RestoreRuntimeState()
        {
            var state = AppRuntime.ValidationState;

            SourcePathTextBox.Text = state.SourcePath ?? "";

            _imagePaths.Clear();
            _imagePaths.AddRange(state.ImagePaths);

            _currentIndex = state.CurrentIndex;
            _lastAnalyzeResult = state.LastAnalyzeResult;

            LastMessageTextBlock.Text = string.IsNullOrWhiteSpace(state.LastMessage)
                ? "Ready."
                : state.LastMessage;

            StatusTextBlock.Text = string.IsNullOrWhiteSpace(state.StatusText)
                ? "Status: Idle"
                : state.StatusText;

            IsValidatedTextBlock.Text = string.IsNullOrWhiteSpace(state.IsValidatedText)
                ? "False"
                : state.IsValidatedText;

            if (_imagePaths.Count > 0 && _currentIndex >= 0 && _currentIndex < _imagePaths.Count)
            {
                LoadCurrentImage(clearAnalysis: false);

                if (_lastAnalyzeResult != null)
                {
                    TagsListBox.ItemsSource = _lastAnalyzeResult.final_tags ?? new List<string>();
                }
            }
            else
            {
                PreviewImage.Source = null;
                DropHintTextBlock.Visibility = Visibility.Visible;
                RefreshUi();
            }
        }
    }

    public class AnalyzeRequest
    {
        public string image_path { get; set; } = "";
    }

    public class AnalyzeResponse
    {
        public string? caption { get; set; }
        public List<string>? ram_tags { get; set; }
        public List<string>? llm_tags { get; set; }
        public List<string>? final_tags { get; set; }
        public string? error { get; set; }
    }
}