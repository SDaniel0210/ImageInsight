using ImageInsight.Data;
using ImageInsight.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ImageInsight
{
    public partial class HomePage : Page
    {
        private readonly User _currentUser;
        private Process? _backendProcess;

        public HomePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            Loaded += async (_, _) => await LoadUserInfoAsync();
        }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                using var db = new ImageInsightDbContext();

                int contributedImages = await db.Images
                    .CountAsync(i => i.ValidatedByUserId == _currentUser.Id);

                UserInfoTextBlock.Text =
                    $"{_currentUser.Username} | Role: {_currentUser.Role} | Images: {contributedImages}";
            }
            catch (Exception ex)
            {
                AddLog($"User info load error: {ex.Message}");
            }
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backendProcess != null && !_backendProcess.HasExited)
                {
                    AddLog("AI service is already running.");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c .venv\\Scripts\\python.exe -m uvicorn backend.main:app --host 127.0.0.1 --port 8000",
                    WorkingDirectory = @"C:\Users\Samyb\Desktop\Stuff\Saját\Python projektek\ImageInsight",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _backendProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _backendProcess.OutputDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                        Dispatcher.Invoke(() => AddLog(args.Data));
                };

                _backendProcess.ErrorDataReceived += (_, args) =>
                {
                    if (string.IsNullOrWhiteSpace(args.Data))
                        return;

                    Dispatcher.Invoke(() =>
                    {
                        if (args.Data.Contains("Traceback") ||
                            args.Data.Contains("Exception") ||
                            args.Data.Contains("Error") ||
                            args.Data.Contains("ERROR"))
                        {
                            AddLog("ERROR: " + args.Data);
                        }
                        else
                        {
                            AddLog(args.Data);
                        }
                    });
                };

                _backendProcess.Exited += (_, _) =>
                {
                    Dispatcher.Invoke(() => AddLog("AI service stopped."));
                };

                _backendProcess.Start();
                _backendProcess.BeginOutputReadLine();
                _backendProcess.BeginErrorReadLine();

                AddLog("AI service starting...");
            }
            catch (Exception ex)
            {
                AddLog($"AI service start error: {ex.Message}");
            }
        }

        private async void StopService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backendProcess == null || _backendProcess.HasExited)
                {
                    AddLog("AI service is not running.");
                    return;
                }

                AddLog("Stopping AI service...");

                var processToStop = _backendProcess;
                _backendProcess = null;

                await Task.Run(() =>
                {
                    try
                    {
                        if (!processToStop.HasExited)
                        {
                            processToStop.Kill(entireProcessTree: true);
                            processToStop.WaitForExit(5000);
                        }
                    }
                    catch
                    {
                        // ignored here, UI logs below if needed
                    }
                    finally
                    {
                        processToStop.Dispose();
                    }
                });

                AddLog($"AI service stopped by {_currentUser.Username}.");
            }
            catch (Exception ex)
            {
                AddLog($"AI service stop error: {ex.Message}");
            }
        }

        private async void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new UserEditWindow(_currentUser.Id);
            bool? result = window.ShowDialog();

            if (result == true)
            {
                using var db = new ImageInsightDbContext();
                var refreshedUser = await db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

                if (refreshedUser != null)
                {
                    _currentUser.Username = refreshedUser.Username;
                    _currentUser.Role = refreshedUser.Role;
                    _currentUser.LastLogin = refreshedUser.LastLogin;
                }

                await LoadUserInfoAsync();
                AddLog("Profile updated.");
            }
        }

        private void AddLog(string message)
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        }
    }
}