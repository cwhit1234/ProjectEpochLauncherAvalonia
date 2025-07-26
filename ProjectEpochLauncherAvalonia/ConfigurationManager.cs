using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ProjectEpochLauncherAvalonia
{
    public class ConfigurationManager
    {
        private readonly string _configFilePath;
        private LauncherConfiguration _configuration;

        public ConfigurationManager()
        {
            // Get cross-platform config directory
            var configDirectory = GetConfigDirectory();

            // Ensure directory exists
            Directory.CreateDirectory(configDirectory);

            _configFilePath = Path.Combine(configDirectory, Constants.CONFIG_FILE_NAME);
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

        public bool SetupCompleted
        {
            get => _configuration.SetupCompleted;
            set
            {
                _configuration.SetupCompleted = value;
                SaveConfiguration();
            }
        }

        public DateTime LastUpdateCheck
        {
            get => _configuration.LastUpdateCheck;
            set
            {
                _configuration.LastUpdateCheck = value;
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
                    Constants.APPLICATION_NAME
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: ~/Library/Application Support/ProjectEpochLauncherAvalonia
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Library", "Application Support", Constants.APPLICATION_NAME);
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
                return Path.Combine(configHome, Constants.APPLICATION_NAME);
            }
            else
            {
                // Fallback: Use user profile directory
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    $".{Constants.APPLICATION_NAME}"
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
                    Constants.PROJECT_EPOCH_DISPLAY_NAME
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine("/Applications", Constants.PROJECT_EPOCH_DISPLAY_NAME);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Games", Constants.PROJECT_EPOCH_DISPLAY_NAME_ALTERNATIVE);
            }
            else
            {
                // Fallback
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, Constants.PROJECT_EPOCH_DISPLAY_NAME_ALTERNATIVE);
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

                    if (config != null)
                    {
                        LogDebug($"Configuration loaded successfully. Install path: {config.InstallPath}, Setup completed: {config.SetupCompleted}");
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
                InstallPath = GetDefaultInstallPath(),
                SetupCompleted = false,
                LastUpdateCheck = DateTime.MinValue
            };

            SaveConfiguration();
            return config;
        }

        public void MarkSetupCompleted()
        {
            SetupCompleted = true;
            LogDebug("Setup marked as completed");
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
        public bool SetupCompleted { get; set; } = false;
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
    }
}