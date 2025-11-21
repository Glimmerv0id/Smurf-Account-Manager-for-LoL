using System;
using System.IO;
using Newtonsoft.Json;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Services
{
    public class StorageService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmurfAccountManager",
            "config.json"
        );

        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch
            {
                // If loading fails, return new config
            }

            return new AppConfig();
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
