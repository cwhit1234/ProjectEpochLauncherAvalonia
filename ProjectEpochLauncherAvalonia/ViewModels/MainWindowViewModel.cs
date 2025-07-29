using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectEpochLauncherAvalonia.Services;

namespace ProjectEpochLauncherAvalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly UpdateService _updateService;
        private readonly InstallationValidationService _validationService;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string _playButtonText = Constants.PLAY_BUTTON_TEXT;

        [ObservableProperty]
        private bool _isPlayButtonEnabled = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusMessageVisible = true;

        [ObservableProperty]
        private bool _isHomeSelected = true;

        [ObservableProperty]
        private bool _isSettingsSelected = false;

        [ObservableProperty]
        private object? _currentContent;

        // Store commands as their concrete types to avoid casting
        private readonly AsyncRelayCommand _playCommand;
        private readonly AsyncRelayCommand _checkUpdatesCommand;
        private readonly RelayCommand _navigateHomeCommand;
        private readonly RelayCommand _navigateSettingsCommand;
        private readonly AsyncRelayCommand _openDiscordCommand;
        private readonly AsyncRelayCommand _openDonateCommand;

        // Expose as ICommand for binding
        public ICommand PlayCommand => _playCommand;
        public ICommand CheckUpdatesCommand => _checkUpdatesCommand;
        public ICommand NavigateHomeCommand => _navigateHomeCommand;
        public ICommand NavigateSettingsCommand => _navigateSettingsCommand;
        public ICommand OpenDiscordCommand => _openDiscordCommand;
        public ICommand OpenDonateCommand => _openDonateCommand;

        public MainWindowViewModel(ConfigurationManager? configurationManager = null)
        {
            _configurationManager = configurationManager ?? new ConfigurationManager();
            _updateService = new UpdateService(_configurationManager);
            _validationService = new InstallationValidationService();

            // Initialize commands
            _playCommand = new AsyncRelayCommand(ExecutePlayCommandAsync, CanExecutePlayCommand);
            _checkUpdatesCommand = new AsyncRelayCommand(ExecuteCheckUpdatesAsync, CanExecuteCheckUpdates);
            _navigateHomeCommand = new RelayCommand(ExecuteNavigateHome);
            _navigateSettingsCommand = new RelayCommand(ExecuteNavigateSettings);
            _openDiscordCommand = new AsyncRelayCommand(ExecuteOpenDiscordAsync);
            _openDonateCommand = new AsyncRelayCommand(ExecuteOpenDonateAsync);

            // Initialize with home content
            ExecuteNavigateHome();
        }

        private async Task ExecutePlayCommandAsync()
        {
            try
            {
                if (!PlatformSupport.SupportsGameLaunching)
                {
                    StatusMessage = PlatformSupport.GetPlatformLimitationMessage();
                    return;
                }

                PlayButtonText = Constants.LAUNCHING_BUTTON_TEXT;
                IsPlayButtonEnabled = false;
                _playCommand.NotifyCanExecuteChanged();

                var installPath = _configurationManager.InstallPath;

                // Validate installation before launching
                var validationResult = _validationService.ValidateInstallation(installPath);
                if (!validationResult.IsValid)
                {
                    StatusMessage = validationResult.ErrorMessage;
                    return;
                }

                // Additional check for Project Epoch executable
                var executablePath = Path.Combine(installPath, Constants.PROJECT_EPOCH_EXECUTABLE);
                if (!File.Exists(executablePath))
                {
                    StatusMessage = "Project Epoch executable not found. Please check for updates.";
                    return;
                }

                LogDebug($"Launching game from: {executablePath}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = installPath,
                    UseShellExecute = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    StatusMessage = "Game launched successfully!";
                    LogDebug("Game launched successfully");
                }
                else
                {
                    StatusMessage = "Failed to launch the game.";
                    LogError("Failed to start game process");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error launching game: {ex.Message}";
                LogError($"Error launching game: {ex.Message}");
            }
            finally
            {
                PlayButtonText = Constants.PLAY_BUTTON_TEXT;
                IsPlayButtonEnabled = true;
                _playCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecutePlayCommand()
        {
            // Use the same validation logic as UpdateStatusBasedOnInstallation
            if (string.IsNullOrEmpty(_configurationManager.InstallPath))
                return false;

            var validationResult = _validationService.ValidateInstallation(_configurationManager.InstallPath);
            return IsPlayButtonEnabled && validationResult.IsValid;
        }

        private async Task ExecuteCheckUpdatesAsync()
        {
            try
            {
                PlayButtonText = Constants.CHECKING_BUTTON_TEXT;
                IsPlayButtonEnabled = false;
                _playCommand.NotifyCanExecuteChanged();
                StatusMessage = "Checking for updates...";

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                var updateResult = await _updateService.CheckForUpdatesAsync(_cancellationTokenSource.Token);

                if (!updateResult.Success)
                {
                    PlayButtonText = Constants.CHECK_FAILED_BUTTON_TEXT;
                    StatusMessage = $"Update check failed: {updateResult.ErrorMessage}";
                    return;
                }

                if (!updateResult.UpdatesAvailable)
                {
                    PlayButtonText = Constants.UP_TO_DATE_BUTTON_TEXT;
                    StatusMessage = "Game is up to date!";

                    // Reset to play button after delay
                    await Task.Delay(2000);
                    PlayButtonText = Constants.PLAY_BUTTON_TEXT;
                    return;
                }

                // Updates available - start download
                await StartDownloadAsync(updateResult.FilesToUpdate);
            }
            catch (OperationCanceledException)
            {
                PlayButtonText = Constants.DOWNLOAD_CANCELLED_BUTTON_TEXT;
                StatusMessage = "Update check was cancelled.";
            }
            catch (Exception ex)
            {
                PlayButtonText = Constants.CHECK_FAILED_BUTTON_TEXT;
                StatusMessage = $"Error checking for updates: {ex.Message}";
                LogError($"Error checking for updates: {ex.Message}");
            }
            finally
            {
                IsPlayButtonEnabled = true;
                _playCommand.NotifyCanExecuteChanged();

                // Reset button text after delay if it's showing an error state
                if (PlayButtonText.Contains("Failed") || PlayButtonText.Contains("Cancelled"))
                {
                    await Task.Delay(3000);
                    PlayButtonText = Constants.PLAY_BUTTON_TEXT;
                }
            }
        }

        private bool CanExecuteCheckUpdates()
        {
            return !string.IsNullOrEmpty(_configurationManager.InstallPath) &&
                   Directory.Exists(_configurationManager.InstallPath);
        }

        private async Task StartDownloadAsync(System.Collections.Generic.List<Services.GameFile> filesToUpdate)
        {
            try
            {
                PlayButtonText = Constants.DOWNLOADING_BUTTON_TEXT;
                StatusMessage = "Downloading updates...";

                var progress = new Progress<Services.DownloadProgress>(p =>
                {
                    StatusMessage = $"Downloading: {p.FileName} ({p.FileIndex}/{p.TotalFiles}) - {p.OverallProgress:F1}%";
                });

                var downloadResult = await _updateService.DownloadFilesAsync(
                    filesToUpdate,
                    progress,
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

                if (downloadResult.Success)
                {
                    PlayButtonText = Constants.DOWNLOAD_COMPLETE_BUTTON_TEXT;
                    StatusMessage = "Download completed successfully!";

                    // Refresh command states since installation status may have changed
                    RefreshCommandStates();

                    await Task.Delay(2000);
                    PlayButtonText = Constants.PLAY_BUTTON_TEXT;
                    StatusMessage = "Ready to play!";
                }
                else
                {
                    PlayButtonText = Constants.DOWNLOAD_FAILED_BUTTON_TEXT;
                    StatusMessage = $"Download failed: {downloadResult.ErrorMessage}";
                }
            }
            catch (OperationCanceledException)
            {
                PlayButtonText = Constants.DOWNLOAD_CANCELLED_BUTTON_TEXT;
                StatusMessage = "Download was cancelled.";
            }
            catch (Exception ex)
            {
                PlayButtonText = Constants.DOWNLOAD_FAILED_BUTTON_TEXT;
                StatusMessage = $"Download error: {ex.Message}";
                LogError($"Download error: {ex.Message}");
            }
        }

        private void ExecuteNavigateHome()
        {
            IsHomeSelected = true;
            IsSettingsSelected = false;
            IsStatusMessageVisible = true; // Show status message on Home

            // Refresh home content and status to reflect current installation status
            CurrentContent = CreateHomeContent();
            UpdateStatusBasedOnInstallation();
        }

        private void ExecuteNavigateSettings()
        {
            IsHomeSelected = false;
            IsSettingsSelected = true;
            IsStatusMessageVisible = false; // Hide status message on Settings

            // Refresh settings content
            CurrentContent = CreateSettingsContent();
        }

        private object CreateHomeContent()
        {
            var validationResult = _validationService.ValidateInstallation(_configurationManager.InstallPath);
            var isInstalled = validationResult.IsValid && validationResult.InstallationType == InstallationType.Complete;

            return new HomeViewModel(
                isInstalled,
                _configurationManager.InstallPath);
        }

        private object CreateSettingsContent()
        {
            var settingsViewModel = new SettingsViewModel(_configurationManager);
            return settingsViewModel;
        }

        private async Task ExecuteOpenDiscordAsync()
        {
            try
            {
                await OpenUrlAsync(Constants.DISCORD_URL);
                StatusMessage = "Opening Discord community...";
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to open Discord link.";
                LogError($"Error opening Discord URL: {ex.Message}");
            }
        }

        private async Task ExecuteOpenDonateAsync()
        {
            try
            {
                await OpenUrlAsync(Constants.DONATE_URL);
                StatusMessage = "Opening donation page...";
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to open donation link.";
                LogError($"Error opening donate URL: {ex.Message}");
            }
        }

        private static async Task OpenUrlAsync(string url)
        {
            await Task.Run(() =>
            {
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
                catch (Exception ex)
                {
                    // Fallback for different platforms
                    if (PlatformSupport.IsWindows)
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (PlatformSupport.IsMacOS)
                    {
                        Process.Start("open", url);
                    }
                    else if (PlatformSupport.IsLinux)
                    {
                        Process.Start("xdg-open", url);
                    }
                    else
                    {
                        throw;
                    }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static void LogDebug(string message)
        {
            Debug.WriteLine($"[MAIN-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void LogError(string message)
        {
            Debug.WriteLine($"[MAIN-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private void RefreshCommandStates()
        {
            // Refresh command can execute states
            _playCommand.NotifyCanExecuteChanged();
            _checkUpdatesCommand.NotifyCanExecuteChanged();

            // Update status message based on current installation state (only if on Home)
            if (IsHomeSelected)
            {
                UpdateStatusBasedOnInstallation();
            }

            // Refresh home content to show updated installation status
            if (IsHomeSelected)
            {
                CurrentContent = CreateHomeContent();
            }
        }

        private void UpdateStatusBasedOnInstallation()
        {
            if (string.IsNullOrEmpty(_configurationManager.InstallPath))
            {
                StatusMessage = "Please configure an installation path in Settings.";
                IsPlayButtonEnabled = false;
                _playCommand.NotifyCanExecuteChanged();
                return;
            }

            var validationResult = _validationService.ValidateInstallation(_configurationManager.InstallPath);

            if (validationResult.IsValid)
            {
                IsPlayButtonEnabled = true;
                StatusMessage = "Ready to play!";
            }
            else
            {
                IsPlayButtonEnabled = false;
                StatusMessage = validationResult.ErrorMessage;
            }

            _playCommand.NotifyCanExecuteChanged();
        }
    }
}