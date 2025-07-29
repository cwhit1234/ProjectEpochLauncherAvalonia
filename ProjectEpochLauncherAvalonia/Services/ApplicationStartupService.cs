using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ProjectEpochLauncherAvalonia.ViewModels;
using ProjectEpochLauncherAvalonia.Views;

namespace ProjectEpochLauncherAvalonia.Services
{
    public class ApplicationStartupService
    {
        private readonly IClassicDesktopStyleApplicationLifetime _desktop;
        private readonly ConfigurationManager _configurationManager;
        private readonly InstallationValidationService _validationService;

        public ApplicationStartupService(IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop ?? throw new ArgumentNullException(nameof(desktop));
            _configurationManager = new ConfigurationManager();
            _validationService = new InstallationValidationService();
        }

        public void StartApplication()
        {
            try
            {
                LogDebug("Starting application...");

                if (IsFirstLaunch())
                {
                    LogDebug("First launch detected - showing setup wizard");
                    ShowSetupWizard();
                }
                else
                {
                    LogDebug("Returning user - showing main window");
                    ShowMainWindow();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during application startup: {ex.Message}");

                // Fallback - try to show main window
                try
                {
                    ShowMainWindow();
                }
                catch (Exception fallbackEx)
                {
                    LogError($"Fallback startup failed: {fallbackEx.Message}");
                    _desktop.Shutdown();
                }
            }
        }

        private bool IsFirstLaunch()
        {
            // Primary check: if setup has been completed, don't consider it first launch
            if (_configurationManager.SetupCompleted)
            {
                LogDebug("Setup already completed - not first launch");
                return false;
            }

            var installPath = _configurationManager.InstallPath;

            // Use validation service to check if we have a playable installation
            var validationResult = _validationService.ValidateInstallation(installPath);
            var isFirstLaunch = !validationResult.IsValid || validationResult.InstallationType != InstallationType.Complete;

            LogDebug($"First launch check - SetupCompleted: {_configurationManager.SetupCompleted}, " +
                    $"InstallPath: '{installPath}', ValidationResult: {validationResult.InstallationType}, " +
                    $"IsValid: {validationResult.IsValid}, Result: {isFirstLaunch}");

            return isFirstLaunch;
        }

        private bool AreProjectEpochFilesPresent(string? installPath)
        {
            // This method is kept for backward compatibility but now uses the validation service
            var validationResult = _validationService.ValidateInstallation(installPath);
            return validationResult.HasProjectEpochFiles;
        }

        private void ShowSetupWizard()
        {
            try
            {
                var setupWizardViewModel = new SetupWizardViewModel(_configurationManager);
                var setupWizard = new SetupWizardView
                {
                    DataContext = setupWizardViewModel
                };

                // Handle setup completion
                setupWizardViewModel.SetupCompleted += OnSetupCompleted;
                setupWizardViewModel.SetupCancelled += OnSetupCancelled;

                _desktop.MainWindow = setupWizard;
                LogDebug("Setup wizard displayed");
            }
            catch (Exception ex)
            {
                LogError($"Error creating setup wizard: {ex.Message}");
                throw;
            }
        }

        private void OnSetupCompleted(object? sender, EventArgs e)
        {
            LogDebug("Setup completed - transitioning to main window");

            try
            {
                // Temporarily prevent shutdown when closing setup wizard
                _desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Close setup wizard and show main window
                _desktop.MainWindow?.Close();
                ShowMainWindow();

                // Reset shutdown mode
                _desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            catch (Exception ex)
            {
                LogError($"Error transitioning from setup to main window: {ex.Message}");
                _desktop.Shutdown();
            }
        }

        private void OnSetupCancelled(object? sender, EventArgs e)
        {
            LogDebug("Setup cancelled - shutting down application");
            _desktop.Shutdown();
        }

        private void ShowMainWindow()
        {
            try
            {
                var mainWindowViewModel = new MainWindowViewModel(_configurationManager);
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };

                _desktop.MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.Activate();

                LogDebug("Main window displayed");
            }
            catch (Exception ex)
            {
                LogError($"Error creating main window: {ex.Message}");
                throw;
            }
        }

        private static void LogDebug(string message)
        {
            Debug.WriteLine($"[STARTUP-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void LogError(string message)
        {
            Debug.WriteLine($"[STARTUP-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }
}