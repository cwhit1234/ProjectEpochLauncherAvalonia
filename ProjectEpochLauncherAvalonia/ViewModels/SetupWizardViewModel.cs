using System;
using System.Collections.Generic;
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
    public partial class SetupWizardViewModel : ViewModelBase
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly UpdateService _updateService;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private SetupStep _currentStep = SetupStep.Welcome;

        [ObservableProperty]
        private string _selectedInstallPath = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _downloadStatus = string.Empty;

        [ObservableProperty]
        private double _fileProgress = 0;

        [ObservableProperty]
        private double _overallProgress = 0;

        [ObservableProperty]
        private string _fileProgressText = "File: Preparing...";

        [ObservableProperty]
        private string _overallProgressText = "Overall: 0%";

        [ObservableProperty]
        private bool _canGoBack = false;

        [ObservableProperty]
        private bool _canGoNext = true;

        [ObservableProperty]
        private string _nextButtonText = Constants.GET_STARTED_BUTTON_TEXT;

        [ObservableProperty]
        private bool _isDownloading = false;

        [ObservableProperty]
        private bool _isCompleted = false;

        public ICommand BackCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand BrowseInstallPathCommand { get; set; }
        public ICommand CancelCommand { get; }

        public event EventHandler? SetupCompleted;
        public event EventHandler? SetupCancelled;

        public SetupWizardViewModel(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _updateService = new UpdateService(_configurationManager);

            // Initialize with default install path
            SelectedInstallPath = _configurationManager.InstallPath;
            if (string.IsNullOrEmpty(SelectedInstallPath))
            {
                SelectedInstallPath = _configurationManager.GetDefaultInstallPath();
            }

            // Initialize commands
            BackCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
            NextCommand = new AsyncRelayCommand(ExecuteNextAsync, CanExecuteNext);
            BrowseInstallPathCommand = new RelayCommand(() => { }); // Will be replaced by View
            CancelCommand = new RelayCommand(ExecuteCancel);

            UpdateNavigationState();
        }

        private bool CanExecuteBack()
        {
            return CanGoBack && !IsDownloading;
        }

        private void ExecuteBack()
        {
            if (CurrentStep > SetupStep.Welcome)
            {
                CurrentStep--;
                UpdateNavigationState();
                LogDebug($"Navigated back to step: {CurrentStep}");
            }
        }

        private bool CanExecuteNext()
        {
            return CanGoNext && !IsDownloading && !IsCompleted;
        }

        private async Task ExecuteNextAsync()
        {
            try
            {
                switch (CurrentStep)
                {
                    case SetupStep.Welcome:
                        CurrentStep = SetupStep.InstallPath;
                        break;

                    case SetupStep.InstallPath:
                        if (await ValidateInstallPathAsync())
                        {
                            CurrentStep = SetupStep.Download;
                            await StartDownloadAsync();
                        }
                        break;

                    case SetupStep.Download:
                        // This step should not be navigable during download
                        break;

                    case SetupStep.Complete:
                        await CompleteSetupAsync();
                        break;
                }

                UpdateNavigationState();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                LogError($"Error in ExecuteNextAsync: {ex.Message}");
            }
        }

        private async Task<bool> ValidateInstallPathAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedInstallPath))
                {
                    StatusMessage = "Please select an installation path.";
                    return false;
                }

                // Check if directory exists or can be created
                if (!Directory.Exists(SelectedInstallPath))
                {
                    try
                    {
                        Directory.CreateDirectory(SelectedInstallPath);
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Cannot create directory: {ex.Message}";
                        return false;
                    }
                }

                // Check write permissions
                var testFile = Path.Combine(SelectedInstallPath, "test_write_permission.tmp");
                try
                {
                    await File.WriteAllTextAsync(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"No write permission to selected directory: {ex.Message}";
                    return false;
                }

                // Save the install path
                _configurationManager.InstallPath = SelectedInstallPath;
                StatusMessage = "Install path validated successfully.";
                LogDebug($"Install path validated: {SelectedInstallPath}");
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Path validation error: {ex.Message}";
                LogError($"Path validation error: {ex.Message}");
                return false;
            }
        }

        private async Task StartDownloadAsync()
        {
            try
            {
                IsDownloading = true;
                StatusMessage = "Checking for game files...";
                DownloadStatus = "Preparing download...";

                _cancellationTokenSource = new CancellationTokenSource();

                // Check for updates
                var updateResult = await _updateService.CheckForUpdatesAsync(_cancellationTokenSource.Token);

                if (!updateResult.Success)
                {
                    StatusMessage = $"Error checking for updates: {updateResult.ErrorMessage}";
                    IsDownloading = false;
                    return;
                }

                if (!updateResult.UpdatesAvailable)
                {
                    StatusMessage = "Game files are already up to date!";
                    DownloadStatus = "No download required.";
                    await Task.Delay(1000);
                    await CompleteDownload();
                    return;
                }

                // Start download
                StatusMessage = "Downloading game files...";
                LogDebug($"Starting download of {updateResult.FilesToUpdate.Count} files");

                var progress = new Progress<DownloadProgress>(UpdateDownloadProgress);

                var downloadResult = await _updateService.DownloadFilesAsync(
                    updateResult.FilesToUpdate,
                    progress,
                    _cancellationTokenSource.Token);

                if (downloadResult.Success)
                {
                    StatusMessage = "Download completed successfully!";
                    DownloadStatus = "All files downloaded.";
                    await CompleteDownload();
                }
                else
                {
                    StatusMessage = $"{Constants.DOWNLOAD_FAILED_BUTTON_TEXT}: {downloadResult.ErrorMessage}";
                    IsDownloading = false;
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Download was cancelled.";
                DownloadStatus = Constants.DOWNLOAD_CANCELLED_BUTTON_TEXT;
                IsDownloading = false;
                LogDebug("Download cancelled by user");
            }
            catch (Exception ex)
            {
                StatusMessage = $"{Constants.DOWNLOAD_ERROR_BUTTON_TEXT}: {ex.Message}";
                DownloadStatus = Constants.DOWNLOAD_FAILED_BUTTON_TEXT;
                IsDownloading = false;
                LogError($"{Constants.DOWNLOAD_ERROR_BUTTON_TEXT}: {ex.Message}");
            }
        }

        private void UpdateDownloadProgress(DownloadProgress progress)
        {
            FileProgress = progress.FileProgress;
            OverallProgress = progress.OverallProgress;
            FileProgressText = $"File: {progress.FileName} ({progress.FileIndex}/{progress.TotalFiles})";

            var downloadedMB = progress.BytesDownloaded / (1024.0 * 1024.0);
            var totalMB = progress.TotalBytes / (1024.0 * 1024.0);
            OverallProgressText = $"Overall: {progress.OverallProgress:F1}% ({downloadedMB:F1} MB / {totalMB:F1} MB)";

            DownloadStatus = progress.Status;
        }

        private async Task CompleteDownload()
        {
            await Task.Delay(1000); // Brief pause to show completion

            CurrentStep = SetupStep.Complete;
            IsDownloading = false;
            UpdateNavigationState();

            LogDebug("Download completed, moved to completion step");
        }

        private async Task CompleteSetupAsync()
        {
            try
            {
                // Mark setup as completed
                _configurationManager.MarkSetupCompleted();

                IsCompleted = true;
                StatusMessage = "Setup completed successfully!";

                LogDebug("Setup marked as completed");

                // Small delay before notifying completion
                await Task.Delay(500);

                // Notify that setup is complete
                SetupCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error completing setup: {ex.Message}";
                LogError($"Error completing setup: {ex.Message}");
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                // Cancel any ongoing operations
                _cancellationTokenSource?.Cancel();

                LogDebug("Setup cancelled by user");

                // Notify cancellation
                SetupCancelled?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogError($"Error during cancel: {ex.Message}");
                // Still notify cancellation even if there's an error
                SetupCancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdateInstallPath(string newPath)
        {
            if (!string.IsNullOrEmpty(newPath))
            {
                SelectedInstallPath = newPath;
                StatusMessage = $"Install path updated to: {newPath}";
                UpdateNavigationState();
            }
        }

        private void UpdateNavigationState()
        {
            CanGoBack = CurrentStep > SetupStep.Welcome && !IsDownloading;

            switch (CurrentStep)
            {
                case SetupStep.Welcome:
                    NextButtonText = Constants.GET_STARTED_BUTTON_TEXT;
                    CanGoNext = true;
                    break;

                case SetupStep.InstallPath:
                    NextButtonText = Constants.INSTALL_BUTTON_TEXT;
                    CanGoNext = !string.IsNullOrWhiteSpace(SelectedInstallPath);
                    break;

                case SetupStep.Download:
                    NextButtonText = "Please Wait...";
                    CanGoNext = false;
                    break;

                case SetupStep.Complete:
                    NextButtonText = Constants.FINISH_BUTTON_TEXT;
                    CanGoNext = true;
                    break;
            }

            // Update command can execute states
            ((RelayCommand)BackCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)NextCommand).NotifyCanExecuteChanged();
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
            Debug.WriteLine($"[SETUP-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void LogError(string message)
        {
            Debug.WriteLine($"[SETUP-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }

    public enum SetupStep
    {
        Welcome = 0,
        InstallPath = 1,
        Download = 2,
        Complete = 3
    }
}