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
                    setupWizard.Closed += async (s, e) =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[APP] Setup wizard closed event triggered");

                            // Add a small delay to ensure all operations have completed
                            await System.Threading.Tasks.Task.Delay(100);

                            // Re-check setup completion and first launch status
                            var isSetupComplete = configManager.SetupCompleted;
                            var isStillFirstLaunch = IsFirstLaunch(configManager);

                            System.Diagnostics.Debug.WriteLine($"[APP] Setup wizard closed - SetupCompleted: {isSetupComplete}, IsFirstLaunch: {isStillFirstLaunch}");

                            if (isSetupComplete && !isStillFirstLaunch)
                            {
                                // Setup completed successfully - show main window
                                System.Diagnostics.Debug.WriteLine("[APP] Opening main window after successful setup");

                                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    var mainWindow = new MainWindow
                                    {
                                        DataContext = new MainWindowViewModel(),
                                    };

                                    desktop.MainWindow = mainWindow;
                                    mainWindow.Show();

                                    System.Diagnostics.Debug.WriteLine("[APP] Main window created and shown");
                                });
                            }
                            else
                            {
                                // Setup was cancelled or not completed properly - exit application
                                System.Diagnostics.Debug.WriteLine("[APP] Shutting down application - setup not completed");

                                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    desktop.Shutdown();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[APP] Error in setup wizard closed handler: {ex.Message}");

                            // Fallback: try to show main window anyway
                            try
                            {
                                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    var mainWindow = new MainWindow
                                    {
                                        DataContext = new MainWindowViewModel(),
                                    };

                                    desktop.MainWindow = mainWindow;
                                    mainWindow.Show();
                                });
                            }
                            catch (Exception fallbackEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[APP] Fallback failed: {fallbackEx.Message}");
                                desktop.Shutdown();
                            }
                        }
                    };
                }
                else
                {
                    // Show main window directly for returning users
                    System.Diagnostics.Debug.WriteLine("[APP] Showing main window directly - not first launch");
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
            // Primary check: if setup has been completed, don't consider it first launch
            if (configManager.SetupCompleted)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] IsFirstLaunch = false - Setup already completed");
                return false;
            }

            var installPath = configManager.InstallPath;

            // Consider it first launch if:
            // 1. Setup not completed, AND
            // 2. (No install path is configured, OR install path doesn't exist, OR Project Epoch files don't exist)
            if (string.IsNullOrEmpty(installPath) ||
                !Directory.Exists(installPath) ||
                !AreProjectEpochFilesPresent(installPath))
            {
                System.Diagnostics.Debug.WriteLine($"[APP] IsFirstLaunch = true - SetupCompleted: {configManager.SetupCompleted}, InstallPath: '{installPath}', PathExists: {Directory.Exists(installPath ?? "")}, ProjectEpochFilesPresent: {AreProjectEpochFilesPresent(installPath)}");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[APP] IsFirstLaunch = false - all checks passed");
            return false;
        }

        private bool AreProjectEpochFilesPresent(string installPath)
        {
            try
            {
                if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
                {
                    return false;
                }

                // Only check for Project Epoch specific files that are downloaded by the setup wizard
                var projectEpochExePath = Path.Combine(installPath, Constants.PROJECT_EPOCH_EXECUTABLE);
                bool projectEpochExists = File.Exists(projectEpochExePath);

                System.Diagnostics.Debug.WriteLine($"[APP] Project Epoch files check - Project-Epoch.exe: {projectEpochExists}");

                return projectEpochExists;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] Error checking Project Epoch files: {ex.Message}");
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