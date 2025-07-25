using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ProjectEpochLauncherAvalonia
{
    public class ConfigurationManager
    {
        private static readonly string ConfigFileName = "launcher-config.json";
        private readonly string _configFilePath;
        private LauncherConfiguration _configuration;

        public ConfigurationManager()
        {
            // Get cross-platform config directory
            var configDirectory = GetConfigDirectory();

            // Ensure directory exists
            Directory.CreateDirectory(configDirectory);

            _configFilePath = Path.Combine(configDirectory, ConfigFileName);
            _configuration = LoadConfiguration();
        }

        public string InstallPath
        {
            get => _configuration.InstallPath;
            set
            {
                _configuration.InstallPath = value;
                SaveConfiguration();
            }
        }

        private string GetConfigDirectory()
        {
            // Cross-platform config directory resolution
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: %LocalAppData%\ProjectEpochLauncherAvalonia
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ProjectEpochLauncherAvalonia"
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: ~/Library/Application Support/ProjectEpochLauncherAvalonia
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Library", "Application Support", "ProjectEpochLauncherAvalonia");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: ~/.config/ProjectEpochLauncherAvalonia (follows XDG Base Directory spec)
                var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(configHome))
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    configHome = Path.Combine(home, ".config");
                }
                return Path.Combine(configHome, "ProjectEpochLauncherAvalonia");
            }
            else
            {
                // Fallback: Use user profile directory
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".ProjectEpochLauncherAvalonia"
                );
            }
        }

        private string GetDefaultInstallPath()
        {
            // Cross-platform default install path
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Project Epoch"
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine("/Applications", "Project Epoch");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Games", "ProjectEpoch");
            }
            else
            {
                // Fallback
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "ProjectEpoch");
            }
        }

        private LauncherConfiguration LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    LogDebug($"Loading configuration from: {_configFilePath}");

                    var jsonString = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<LauncherConfiguration>(jsonString);

                    if (config != null && !string.IsNullOrEmpty(config.InstallPath))
                    {
                        LogDebug($"Configuration loaded successfully. Install path: {config.InstallPath}");
                        return config;
                    }
                }

                LogDebug("No configuration found or invalid config. Creating default configuration.");
                return CreateDefaultConfiguration();
            }
            catch (Exception ex)
            {
                LogError($"Error loading configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var jsonString = JsonSerializer.Serialize(_configuration, options);
                File.WriteAllText(_configFilePath, jsonString);

                LogDebug($"Configuration saved to: {_configFilePath}");
            }
            catch (Exception ex)
            {
                LogError($"Error saving configuration: {ex.Message}");
            }
        }

        private LauncherConfiguration CreateDefaultConfiguration()
        {
            var config = new LauncherConfiguration
            {
                InstallPath = GetDefaultInstallPath()
            };

            SaveConfiguration();
            return config;
        }

        #region Logging

        private void LogDebug(string message)
        {
            Debug.WriteLine($"[CONFIG-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private void LogError(string message)
        {
            Debug.WriteLine($"[CONFIG-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        #endregion
    }

    public class LauncherConfiguration
    {
        public string InstallPath { get; set; } = string.Empty;
    }
}