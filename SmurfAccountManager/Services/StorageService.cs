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

        private static readonly string BackupPath = ConfigPath + ".backup";
        private static readonly string TempPath = ConfigPath + ".tmp";

        /// <summary>
        /// Loads configuration from disk, with automatic backup recovery on failure
        /// </summary>
        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    
                    if (config != null)
                        return config;
                }
            }
            catch (JsonException jsonEx)
            {
                // Config file is corrupted - try to load from backup
                System.Diagnostics.Debug.WriteLine($"[StorageService] Config corrupted: {jsonEx.Message}");
                
                if (File.Exists(BackupPath))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[StorageService] Attempting to load from backup...");
                        var json = File.ReadAllText(BackupPath);
                        var config = JsonConvert.DeserializeObject<AppConfig>(json);
                        
                        if (config != null)
                        {
                            // Successfully loaded from backup!
                            System.Diagnostics.Debug.WriteLine("[StorageService] Backup loaded successfully, restoring...");
                            
                            // Move corrupted config out of the way
                            var corruptedPath = ConfigPath + $".corrupted_{DateTime.Now:yyyyMMddHHmmss}";
                            File.Move(ConfigPath, corruptedPath);
                            
                            // Save the backup as the new main config
                            SaveConfig(config);
                            
                            return config;
                        }
                    }
                    catch (Exception backupEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[StorageService] Backup also failed: {backupEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageService] Load failed: {ex.Message}");
            }

            // If all else fails, return new config
            return new AppConfig();
        }

        /// <summary>
        /// Saves configuration to disk with automatic backup and atomic write
        /// </summary>
        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Backup existing config before overwriting
                if (File.Exists(ConfigPath))
                {
                    File.Copy(ConfigPath, BackupPath, overwrite: true);
                }

                // Serialize config
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                
                // Write to temporary file first
                File.WriteAllText(TempPath, json);
                
                // Then atomically replace the main config file
                // This ensures we never end up with a half-written config
                File.Move(TempPath, ConfigPath, overwrite: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageService] Save failed: {ex.Message}");
                
                // Clean up temp file if it exists
                try
                {
                    if (File.Exists(TempPath))
                        File.Delete(TempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
