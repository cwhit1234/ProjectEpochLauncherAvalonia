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
                        System.Diagnostics.Debug.WriteLine("[APP] Setup wizard closed event triggered");

                        // IMPORTANT: Don't let the app shutdown when setup wizard closes
                        desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                        System.Diagnostics.Debug.WriteLine("[APP] Set shutdown mode to OnExplicitShutdown");

                        // Use a timer instead of Task.Run to avoid disposal issues
                        var timer = new System.Timers.Timer(500); // 500ms delay
                        timer.Elapsed += (timerSender, timerArgs) =>
                        {
                            timer.Stop();
                            timer.Dispose();

                            try
                            {
                                System.Diagnostics.Debug.WriteLine("[APP] Timer elapsed - checking setup status");

                                // Re-check setup completion status
                                var isSetupComplete = configManager.SetupCompleted;
                                var isStillFirstLaunch = IsFirstLaunch(configManager);

                                System.Diagnostics.Debug.WriteLine($"[APP] Setup check - SetupCompleted: {isSetupComplete}, IsFirstLaunch: {isStillFirstLaunch}");

                                if (isSetupComplete && !isStillFirstLaunch)
                                {
                                    System.Diagnostics.Debug.WriteLine("[APP] Setup conditions met - attempting to create main window");

                                    // Setup completed - create main window on UI thread
                                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                    {
                                        System.Diagnostics.Debug.WriteLine("[APP] UI thread callback started - Creating main window after successful setup");

                                        try
                                        {
                                            System.Diagnostics.Debug.WriteLine("[APP] About to create MainWindow instance");
                                            var mainWindow = new MainWindow
                                            {
                                                DataContext = new MainWindowViewModel(),
                                            };

                                            System.Diagnostics.Debug.WriteLine("[APP] Main window instance created successfully");

                                            desktop.MainWindow = mainWindow;
                                            System.Diagnostics.Debug.WriteLine("[APP] Main window set as desktop.MainWindow");

                                            // Reset shutdown mode to normal now that we have a main window
                                            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                                            System.Diagnostics.Debug.WriteLine("[APP] Reset shutdown mode to OnMainWindowClose");

                                            mainWindow.Show();
                                            System.Diagnostics.Debug.WriteLine("[APP] Main window Show() called");

                                            mainWindow.Activate();
                                            System.Diagnostics.Debug.WriteLine("[APP] Main window Activate() called");

                                            System.Diagnostics.Debug.WriteLine("[APP] Main window creation completed successfully");
                                        }
                                        catch (Exception mainWindowEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[APP] Error creating main window: {mainWindowEx.Message}");
                                            System.Diagnostics.Debug.WriteLine($"[APP] Main window error stack trace: {mainWindowEx.StackTrace}");

                                            // If main window creation fails, shutdown explicitly
                                            desktop.Shutdown();
                                        }
                                    });
                                }
                                else
                                {
                                    // Setup was cancelled or not completed - exit
                                    System.Diagnostics.Debug.WriteLine($"[APP] Setup conditions NOT met - shutting down. SetupCompleted: {isSetupComplete}, IsFirstLaunch: {isStillFirstLaunch}");
                                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                    {
                                        desktop.Shutdown();
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[APP] Error in timer callback: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"[APP] Error stack trace: {ex.StackTrace}");

                                // Try to create main window anyway as fallback
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    try
                                    {
                                        System.Diagnostics.Debug.WriteLine("[APP] Attempting fallback main window creation");
                                        var mainWindow = new MainWindow
                                        {
                                            DataContext = new MainWindowViewModel(),
                                        };

                                        desktop.MainWindow = mainWindow;
                                        desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                                        mainWindow.Show();
                                        mainWindow.Activate();

                                        System.Diagnostics.Debug.WriteLine("[APP] Fallback main window created");
                                    }
                                    catch (Exception fallbackEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[APP] Fallback failed: {fallbackEx.Message}");
                                        desktop.Shutdown();
                                    }
                                });
                            }
                        };

                        timer.Start();
                        System.Diagnostics.Debug.WriteLine("[APP] Timer started for setup completion check");
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