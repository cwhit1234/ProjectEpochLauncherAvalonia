using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectEpochLauncherAvalonia.Services;
using System;
using System.Windows.Input;

namespace ProjectEpochLauncherAvalonia.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly InstallationValidationService _validationService;

        [ObservableProperty]
        private string _installPath = string.Empty;

        [ObservableProperty]
        private bool _setupCompleted;

        [ObservableProperty]
        private DateTime _lastUpdateCheck;

        [ObservableProperty]
        private string _platformName = string.Empty;

        [ObservableProperty]
        private bool _supportsGameLaunching;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBrowseButtonEnabled = true;

        public ICommand BrowseInstallPathCommand { get; set; }

        public SettingsViewModel(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _validationService = new InstallationValidationService();

            // Initialize commands
            BrowseInstallPathCommand = new RelayCommand(() => { });

            // Load current settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            InstallPath = _configurationManager.InstallPath;
            SetupCompleted = _configurationManager.SetupCompleted;
            PlatformName = PlatformSupport.GetPlatformName();
            SupportsGameLaunching = PlatformSupport.SupportsGameLaunching;

            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            if (!SetupCompleted)
            {
                StatusMessage = "Setup not completed. Please run the setup wizard.";
                return;
            }

            var validationResult = _validationService.ValidateInstallation(InstallPath);
            StatusMessage = validationResult.ErrorMessage;
        }

        // Method to be called from the View when folder is selected
        public void UpdateInstallPath(string newPath)
        {
            if (string.IsNullOrEmpty(newPath))
            {
                return;
            }

            InstallPath = newPath;
            _configurationManager.InstallPath = newPath;
            UpdateStatusMessage();
            StatusMessage = $"Install path updated to: {newPath}";
        }

        // Property to display platform limitations
        public string PlatformLimitations
        {
            get
            {
                if (SupportsGameLaunching)
                {
                    return "Full functionality available on this platform.";
                }
                else
                {
                    return PlatformSupport.GetPlatformLimitationMessage();
                }
            }
        }

        // Additional helper properties for UI binding
        public string SetupStatusText => SetupCompleted ? "Setup completed successfully" : "Setup not completed";
        public string SupportStatusText => SupportsGameLaunching ? "Supported" : "Not Supported";
    }
}