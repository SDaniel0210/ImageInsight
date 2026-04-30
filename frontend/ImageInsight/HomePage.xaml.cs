using ImageInsight.Data;
using ImageInsight.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;

namespace ImageInsight
{
    public partial class HomePage : Page
    {
        private readonly User _currentUser;
        private int _backendPort = 8000;
        private const string BackendHost = "127.0.0.1";

        public HomePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            Loaded += async (_, _) =>
            {
                RestoreLogs();
                await LoadUserInfoAsync();
            };
        }
        private void RestoreLogs()
        {
            LogTextBox.Clear();

            foreach (string log in AppRuntime.Logs)
            {
                LogTextBox.AppendText(log + Environment.NewLine);
            }

            LogTextBox.ScrollToEnd();
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
                if (AppRuntime.IsBackendRunning)
                {
                    AddLog("AI service is already running.");
                    return;
                }

                string? projectRoot = FindProjectRoot();

                if (projectRoot == null)
                {
                    AddLog("AI service start error: backend/main.py was not found.");
                    AddLog("Make sure the backend folder exists in the project root.");
                    return;
                }

                string pythonExe = Path.Combine(projectRoot, ".venv", "Scripts", "python.exe");

                if (!File.Exists(pythonExe))
                {
                    AddLog($"AI service start error: Python venv not found.");
                    AddLog($"Expected path: {pythonExe}");
                    AddLog("Run this in the project root:");
                    AddLog("python -m venv .venv");
                    AddLog(".venv\\Scripts\\pip install -r requirements.txt");
                    return;
                }

                _backendPort = FindAvailablePort(8000, 20);

                AddLog($"Selected backend port: {_backendPort}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m uvicorn backend.main:app --host {BackendHost} --port {_backendPort}",
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                AppRuntime.BackendProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                AppRuntime.BackendProcess.OutputDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Dispatcher.Invoke(() => AddLog(args.Data));
                    }
                };

                AppRuntime.BackendProcess.ErrorDataReceived += (_, args) =>
                {
                    if (string.IsNullOrWhiteSpace(args.Data))
                        return;

                    Dispatcher.Invoke(() =>
                    {
                        if (args.Data.Contains("Traceback") ||
                            args.Data.Contains("Exception") ||
                            args.Data.Contains("Error") ||
                            args.Data.Contains("ERROR") ||
                            args.Data.Contains("ModuleNotFoundError"))
                        {
                            AddLog("ERROR: " + args.Data);
                        }
                        else
                        {
                            AddLog(args.Data);
                        }
                    });
                };

                AppRuntime.BackendProcess.Exited += (_, _) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog("AI service stopped.");
                        AppRuntime.BackendProcess?.Dispose();
                        AppRuntime.BackendProcess = null;
                    });
                };

                AppRuntime.BackendProcess.Start();
                AppRuntime.BackendProcess.BeginOutputReadLine();
                AppRuntime.BackendProcess.BeginErrorReadLine();

                AddLog("AI service starting...");
                AddLog($"Project root: {projectRoot}");
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
                if (AppRuntime.BackendProcess == null || AppRuntime.BackendProcess.HasExited)
                {
                    AddLog("AI service is not running.");
                    return;
                }

                AddLog("Stopping AI service...");

                var processToStop = AppRuntime.BackendProcess;
                AppRuntime.BackendProcess = null;

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
                        // ignored
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
            var window = new UserEditWindow(_currentUser.Id, _currentUser);
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

        private string? FindProjectRoot()
        {
            string dir = AppContext.BaseDirectory;

            for (int i = 0; i < 10; i++)
            {
                string backendMain = Path.Combine(dir, "backend", "main.py");

                if (File.Exists(backendMain))
                {
                    return dir;
                }

                DirectoryInfo? parent = Directory.GetParent(dir);

                if (parent == null)
                    break;

                dir = parent.FullName;
            }

            return null;
        }

        private void AddLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

            AppRuntime.Logs.Add(line);

            LogTextBox.AppendText(line + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        }

        private int FindAvailablePort(int startPort = 8000, int maxAttempts = 20)
        {
            for (int port = startPort; port < startPort + maxAttempts; port++)
            {
                if (IsPortAvailable(port))
                    return port;
            }

            throw new Exception($"No available port found between {startPort} and {startPort + maxAttempts - 1}.");
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Parse(BackendHost), port);
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}