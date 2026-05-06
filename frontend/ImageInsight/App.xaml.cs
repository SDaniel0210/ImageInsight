using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using ImageInsight.Services;

namespace ImageInsight
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = AppSettingsService.Load();
            ApplyTheme(settings.Theme);

            var login = new LoginWindow();
            login.Show();
        }

        public void ApplyTheme(string themeName)
        {
            var dicts = Resources.MergedDictionaries;

            var oldTheme = dicts.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("Theme"));

            if (oldTheme != null)
                dicts.Remove(oldTheme);

            dicts.Add(new ResourceDictionary
            {
                Source = new Uri($"/Styles/{themeName}.xaml", UriKind.Relative)
            });
        }
    }
}

