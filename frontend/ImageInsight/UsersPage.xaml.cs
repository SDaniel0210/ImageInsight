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

            e.Row.PreviewMouseRightButtonDown += (_, _) =>
            {
                e.Row.IsSelected = true;
                UsersDataGrid.SelectedItem = e.Row.Item;
                e.Row.Focus();
            };

            var menu = new ContextMenu();

            var addItem = new MenuItem { Header = "Add user" };
            addItem.Click += AddUser_Click;

            var editItem = new MenuItem { Header = "Edit user" };
            editItem.Click += EditUser_Click;

            var deleteItem = new MenuItem { Header = "Delete user" };
            deleteItem.Click += DeleteUser_Click;

            menu.Items.Add(addItem);
            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);

            e.Row.ContextMenu = menu;
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var window = new UserEditWindow(null);
            bool? result = window.ShowDialog();

            if (result == true)
                await LoadUsersAsync();
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not UserDisplayModel selectedUser)
                return;

            var window = new UserEditWindow(selectedUser.Id);
            bool? result = window.ShowDialog();

            if (result == true)
                await LoadUsersAsync();
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not UserDisplayModel selectedUser)
                return;

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