using ImageInsight.Data;
using ImageInsight.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ImageInsight
{
    public partial class UserEditWindow : Window
    {
        private readonly int? _userId;

        public UserEditWindow(int? userId)
        {
            InitializeComponent();
            _userId = userId;

            if (_userId.HasValue)
            {
                LoadUser();
                Title = "Edit user";
            }
            else
            {
                Title = "Add user";
                RoleComboBox.SelectedIndex = 1;
            }
        }

        private void LoadUser()
        {
            using var db = new ImageInsightDbContext();

            var user = db.Users.FirstOrDefault(u => u.Id == _userId.Value);

            if (user == null)
            {
                MessageBox.Show("User not found.");
                DialogResult = false;
                Close();
                return;
            }

            UsernameTextBox.Text = user.Username;

            foreach (ComboBoxItem item in RoleComboBox.Items)
            {
                if (item.Content?.ToString() == user.Role)
                {
                    RoleComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username cannot be empty.");
                return;
            }

            if (!_userId.HasValue && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required when creating a new user.");
                return;
            }

            using var db = new ImageInsightDbContext();

            bool usernameExists = db.Users.Any(u =>
                u.Username == username &&
                (!_userId.HasValue || u.Id != _userId.Value));

            if (usernameExists)
            {
                MessageBox.Show("This username already exists.");
                return;
            }

            User user;

            if (_userId.HasValue)
            {
                user = db.Users.FirstOrDefault(u => u.Id == _userId.Value);

                if (user == null)
                {
                    MessageBox.Show("User not found.");
                    return;
                }
            }
            else
            {
                user = new User
                {
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(user);
            }

            user.Username = username;
            user.Role = role;

            if (!string.IsNullOrWhiteSpace(password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            db.SaveChanges();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}