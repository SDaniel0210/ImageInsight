using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageInsight.Services
{
    public class AppSettings
    {
        public bool SaveUsername { get; set; } = false;
        public string? SavedUsername { get; set; } = "";

        public bool AutoValidationMode { get; set; } = false;
        public bool AutoStartAiService { get; set; } = false;
        public bool SaveAnalyzedImagesAutomatically { get; set; } = true;

        public int DefaultBackendPort { get; set; } = 8000;

        public string Theme { get; set; } = "DefaultTheme";
    }

    public static class AppSettingsService
    {
        private static readonly string AppFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ImageInsight"
            );

        private static readonly string SettingsPath =
            Path.Combine(AppFolder, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsPath);

                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);

                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(AppFolder);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(settings, options);

            File.WriteAllText(SettingsPath, json);
        }

        public static string GetSettingsPath()
        {
            return SettingsPath;
        }
    }
}
