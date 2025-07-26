using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ProjectEpochLauncherAvalonia.ViewModels;
using System.IO;
using System;

namespace ProjectEpochLauncherAvalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                // Check if this is first launch
                var configManager = new ConfigurationManager();
                if (IsFirstLaunch(configManager))
                {
                    // Show setup wizard first
                    var setupWizard = new SetupWizard(configManager);
                    desktop.MainWindow = setupWizard;

                    // When setup wizard closes, check if setup was completed
                    setupWizard.Closed += (s, e) =>
                    {
                        // Re-check setup completion and first launch status
                        var isSetupComplete = configManager.SetupCompleted;
                        var isStillFirstLaunch = IsFirstLaunch(configManager);

                        System.Diagnostics.Debug.WriteLine($"[APP] Setup wizard closed - SetupCompleted: {isSetupComplete}, IsFirstLaunch: {isStillFirstLaunch}");

                        if (isSetupComplete && !isStillFirstLaunch)
                        {
                            // Setup completed successfully - show main window
                            System.Diagnostics.Debug.WriteLine("[APP] Opening main window after successful setup");
                            var mainWindow = new MainWindow
                            {
                                DataContext = new MainWindowViewModel(),
                            };
                            desktop.MainWindow = mainWindow;
                            mainWindow.Show();
                        }
                        else
                        {
                            // Setup was cancelled or not completed properly - exit application
                            System.Diagnostics.Debug.WriteLine("[APP] Shutting down application - setup not completed");
                            desktop.Shutdown();
                        }
                    };
                }
                else
                {
                    // Show main window directly for returning users
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private bool IsFirstLaunch(ConfigurationManager configManager)
        {
            var installPath = configManager.InstallPath;

            // Consider it first launch if:
            // 1. No install path is configured, OR
            // 2. Install path doesn't exist, OR 
            // 3. Required game files don't exist
            if (string.IsNullOrEmpty(installPath) ||
                !Directory.Exists(installPath) ||
                !AreRequiredFilesPresent(installPath))
            {
                return true;
            }

            return false;
        }

        private bool AreRequiredFilesPresent(string installPath)
        {
            try
            {
                if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
                {
                    return false;
                }

                var wowExePath = Path.Combine(installPath, Constants.WOW_EXECUTABLE);
                var projectEpochExePath = Path.Combine(installPath, Constants.PROJECT_EPOCH_EXECUTABLE);

                return File.Exists(wowExePath) && File.Exists(projectEpochExePath);
            }
            catch
            {
                return false;
            }
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}