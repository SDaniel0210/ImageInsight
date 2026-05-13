using ImageInsight.Data;
using ImageInsight.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImageInsight
{
    public partial class UsersPage : Page
    {
        private readonly User _currentUser;
        private bool IsAdmin => _currentUser.Role == "Admin";

        public UsersPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (!IsAdmin)
            {
                UsersDataGrid.IsReadOnly = true;
                UsersDataGrid.ContextMenu = null;
            }

            Loaded += async (_, _) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            using var db = new ImageInsightDbContext();

            var users = await db.Users
                .Select(u => new UserDisplayModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    ValidatedImagesCount = u.Images.Count,
                    FeedbackCount = u.TagFeedbacks.Count
                })
                .ToListAsync();

            UsersDataGrid.ItemsSource = users;
        }

        private void UsersDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (!IsAdmin)
            {
                e.Row.ContextMenu = null;
                return;
            }

            e.Row.PreviewMouseRightButtonDown += (s, args) =>
            {
                var row = s as DataGridRow;
                if (row != null)
                {
                    row.IsSelected = true;
                    row.Focus();
                }
            };

            var menu = new ContextMenu();

            menu.SetResourceReference(Control.BackgroundProperty, "ElevatedSurfaceBrush");
            menu.SetResourceReference(Control.ForegroundProperty, "PrimaryTextBrush");
            menu.SetResourceReference(Control.BorderBrushProperty, "ControlBorderBrush");

            menu.Items.Add(CreateStyledMenuItem("Add user", AddUser_Click));
            menu.Items.Add(CreateStyledMenuItem("Edit user", EditUser_Click));
            menu.Items.Add(CreateStyledMenuItem("Delete user", DeleteUser_Click));

            e.Row.ContextMenu = menu;
        }

        private MenuItem CreateStyledMenuItem(string header, RoutedEventHandler onClick)
        {
            var item = new MenuItem { Header = header };
            item.Click += onClick;
            item.Padding = new Thickness(10, 5, 20, 5);

            item.SetResourceReference(Control.ForegroundProperty, "PrimaryTextBrush");

            return item;
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Access denied.");
                return;
            }
            var window = new UserEditWindow(_currentUser.Id, _currentUser);
            bool? result = window.ShowDialog();

            if (result == true)
                await LoadUsersAsync();
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not UserDisplayModel selectedUser)
                return;
            if (!IsAdmin)
            {
                MessageBox.Show("Access denied.");
                return;
            }

            var window = new UserEditWindow(_currentUser.Id, _currentUser);
            bool? result = window.ShowDialog();

            if (result == true)
                await LoadUsersAsync();
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not UserDisplayModel selectedUser)
                return;
            if (!IsAdmin)
            {
                MessageBox.Show("Access denied.");
                return;
            }
            if (selectedUser.Id == _currentUser.Id)
            {
                MessageBox.Show("You cannot delete your own user.");
                return;
            }

            var confirm = MessageBox.Show(
            $"Are you sure you want to delete user '{selectedUser.Username}'?",
            "Confirm delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            using var db = new ImageInsightDbContext();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == selectedUser.Id);
            if (user == null)
                return;

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            await LoadUsersAsync();
        }
    }

    public class UserDisplayModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public int ValidatedImagesCount { get; set; }
        public int FeedbackCount { get; set; }
    }
}