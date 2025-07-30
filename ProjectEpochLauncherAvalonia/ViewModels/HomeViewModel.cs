using CommunityToolkit.Mvvm.ComponentModel;
using ProjectEpochLauncherAvalonia.Services;
using System;
using System.Diagnostics;

namespace ProjectEpochLauncherAvalonia.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        private readonly InstallationValidationService _validationService;
        private readonly ServerStatusService _serverStatusService;

        [ObservableProperty]
        private string _welcomeMessage = "Welcome to Project Epoch";

        [ObservableProperty]
        private bool _isGameInstalled;

        [ObservableProperty]
        private string _installPath = string.Empty;

        [ObservableProperty]
        private string _loginServerStatus = Constants.CHECKING_BUTTON_TEXT;

        [ObservableProperty]
        private string _worldServerStatus = Constants.CHECKING_BUTTON_TEXT;

        public string InstallStatusText { get; private set; } = "Not Installed";

        public HomeViewModel(bool isGameInstalled, string installPath, ServerStatusService serverStatusService)
        {
            _validationService = new InstallationValidationService();
            _serverStatusService = serverStatusService ?? throw new ArgumentNullException(nameof(serverStatusService));

            IsGameInstalled = isGameInstalled;
            InstallPath = installPath;

            UpdateWelcomeMessage();
            UpdateInstallStatus();

            // Subscribe to server status updates
            _serverStatusService.StatusUpdated += OnServerStatusUpdated;

            // Initialize with current status if already available
            if (_serverStatusService.IsInitialized)
            {
                UpdateServerStatusDisplay();
            }
        }

        private void UpdateWelcomeMessage()
        {
            if (IsGameInstalled)
            {
                WelcomeMessage = "Welcome back to Project Epoch!";
            }
            else
            {
                WelcomeMessage = "Welcome to Project Epoch";
            }
        }

        private void UpdateInstallStatus()
        {
            var validationResult = _validationService.ValidateInstallation(InstallPath);

            InstallStatusText = validationResult.InstallationType switch
            {
                InstallationType.Complete => "Installed and Ready",
                InstallationType.WowClientOnly => "WoW Client Found - Project Epoch Missing",
                InstallationType.ProjectEpochOnly => "Project Epoch Found - WoW Client Missing",
                InstallationType.Empty => "Not Installed",
                _ => "Unknown Status"
            };

            OnPropertyChanged(nameof(InstallStatusText));
        }

        private void OnServerStatusUpdated(object? sender, ServerStatusEventArgs e)
        {
            UpdateServerStatusDisplay();
        }

        private void UpdateServerStatusDisplay()
        {
            LoginServerStatus = _serverStatusService.FormatServerStatus("Login Server", _serverStatusService.LoginServerStatus);
            WorldServerStatus = _serverStatusService.FormatServerStatus("World Server", _serverStatusService.WorldServerStatus);
        }

        public void UpdateInstallationStatus(bool isInstalled, string installPath)
        {
            IsGameInstalled = isInstalled;
            InstallPath = installPath;
            UpdateWelcomeMessage();
            UpdateInstallStatus();
        }

        public void UpdateServerStatus(string status)
        {
            // This method is kept for backwards compatibility but server status is now auto-updated
            LogDebug($"Legacy server status update called with: {status}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events
                if (_serverStatusService != null)
                {
                    _serverStatusService.StatusUpdated -= OnServerStatusUpdated;
                }
            }
            base.Dispose(disposing);
        }

        private static void LogDebug(string message)
        {
            Debug.WriteLine($"[HOME-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void LogError(string message)
        {
            Debug.WriteLine($"[HOME-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }
}