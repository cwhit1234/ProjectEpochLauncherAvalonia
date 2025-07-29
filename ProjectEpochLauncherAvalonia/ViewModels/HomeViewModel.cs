using CommunityToolkit.Mvvm.ComponentModel;
using ProjectEpochLauncherAvalonia.Services;

namespace ProjectEpochLauncherAvalonia.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        private readonly InstallationValidationService _validationService;

        [ObservableProperty]
        private string _welcomeMessage = "Welcome to Project Epoch";

        [ObservableProperty]
        private bool _isGameInstalled;

        [ObservableProperty]
        private string _installPath = string.Empty;

        [ObservableProperty]
        private string _serverStatus = "Checking server status...";

        public string InstallStatusText { get; private set; } = "Not Installed";

        public HomeViewModel(bool isGameInstalled, string installPath)
        {
            _validationService = new InstallationValidationService();

            IsGameInstalled = isGameInstalled;
            InstallPath = installPath;

            UpdateWelcomeMessage();
            UpdateInstallStatus();
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

        public void UpdateInstallationStatus(bool isInstalled, string installPath)
        {
            IsGameInstalled = isInstalled;
            InstallPath = installPath;
            UpdateWelcomeMessage();
            UpdateInstallStatus();
        }

        public void UpdateServerStatus(string status)
        {
            ServerStatus = status;
        }
    }
}