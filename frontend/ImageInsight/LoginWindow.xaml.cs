using ImageInsight.Data;
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
using ImageInsight.Data;
using ImageInsight.Models;
using System;
using System.Linq;
using System.Windows;

namespace ImageInsight
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter username and password.");
                return;
            }

            try
            {
                using var db = new ImageInsightDbContext();

                var user = db.Users.FirstOrDefault(u => u.Username == username);

                //if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                //{
                //    MessageBox.Show("Invalid username or password.");
                //    PasswordBox.Clear();
                //    return;
                //}

                user.LastLogin = DateTime.Now;
                db.SaveChanges();

                var main = new MainWindow(user);
                Application.Current.MainWindow = main;
                main.Show();

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error:\n{ex.Message}");
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (Application.Current.MainWindow == null)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
