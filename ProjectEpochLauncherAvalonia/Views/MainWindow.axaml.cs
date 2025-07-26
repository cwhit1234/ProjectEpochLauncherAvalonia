using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ProjectEpochLauncherAvalonia.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEpochLauncherAvalonia
{
    public partial class MainWindow : Window
    {
        private TransitioningContentControl? _contentArea;
        private RadioButton? _homeButton;
        private RadioButton? _settingsButton;
        private Button? _donateButton;
        private Button? _discordButton;
        private ConfigurationManager _configManager;
        private UpdateService _updateService;
        private CancellationTokenSource? _updateCheckCancellation;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize configuration manager and update service
            _configManager = new ConfigurationManager();
            _updateService = new UpdateService(_configManager);

            // Wire up controls after initialization
            _contentArea = this.FindControl<TransitioningContentControl>("ContentArea");
            _homeButton = this.FindControl<RadioButton>("HomeButton");
            _settingsButton = this.FindControl<RadioButton>("SettingsButton");
            _donateButton = this.FindControl<Button>("DonateButton");
            _discordButton = this.FindControl<Button>("DiscordButton");

            // Wire up navigation events
            if (_homeButton != null)
                _homeButton.Click += OnNavigationClick;

            if (_settingsButton != null)
                _settingsButton.Click += OnNavigationClick;

            if (_donateButton != null)
                _donateButton.Click += OnDonateButtonClick;

            if (_discordButton != null)
                _discordButton.Click += OnDiscordButtonClick;

            // Set initial content (this will only run for returning users now)
            NavigateToHome();

            LogDebug("MainWindow initialized successfully");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Cancel any ongoing update checks
            _updateCheckCancellation?.Cancel();
            _updateCheckCancellation?.Dispose();
            base.OnClosed(e);
        }

        private void OnNavigationClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender == _homeButton)
                {
                    LogDebug("Navigating to Home");
                    NavigateToHome();
                }
                else if (sender == _settingsButton)
                {
                    LogDebug("Navigating to Settings");
                    NavigateToSettings();
                }
            }
            catch (Exception ex)
            {
                LogError($"Navigation error: {ex.Message}");
            }
        }

        private void OnDonateButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug($"Opening donate URL: {Constants.DONATE_URL}");
                OpenUrl(Constants.DONATE_URL);
            }
            catch (Exception ex)
            {
                LogError($"Error opening donate URL: {ex.Message}");
            }
        }

        private void OnDiscordButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug($"Opening Discord URL: {Constants.DISCORD_URL}");
                OpenUrl(Constants.DISCORD_URL);
            }
            catch (Exception ex)
            {
                LogError($"Error opening Discord URL: {ex.Message}");
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };

                Process.Start(processStartInfo);
                LogDebug($"Successfully opened URL: {url}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open URL '{url}': {ex.Message}");
            }
        }

        private void NavigateToHome()
        {
            LogDebug("NavigateToHome() called");

            if (_contentArea == null)
            {
                LogError("ContentArea is null in NavigateToHome");
                return;
            }

            var homeContent = new Grid();

            // Welcome text in center
            var welcomeText = new TextBlock
            {
                Text = "Welcome to Project Epoch",
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse(Constants.COLOR_PRIMARY_GOLD),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            homeContent.Children.Add(welcomeText);

            // Create button panel for bottom-right positioning
            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 15,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom
            };

            // Check if we have a valid install path
            var installPath = _configManager.InstallPath;
            var hasValidPath = !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath);

            LogDebug($"Install path check - Path: '{installPath}', HasValidPath: {hasValidPath}");

            // Determine what status message to show (if any)
            string statusMessage = "";
            bool isErrorMessage = false;

            // Always show check updates button when we have a valid path OR on non-Windows platforms
            if (hasValidPath || !PlatformSupport.SupportsGameLaunching)
            {
                var checkUpdatesButton = CreateCheckUpdatesButton();
                buttonPanel.Children.Add(checkUpdatesButton);
            }

            // Only show Play button on Windows with valid setup
            if (PlatformSupport.SupportsGameLaunching && hasValidPath)
            {
                // Check what files are missing and create appropriate message
                var missingFilesInfo = GetMissingFilesInfo();
                LogDebug($"Missing files check - HasMissingFiles: {missingFilesInfo.HasMissingFiles}");

                if (!missingFilesInfo.HasMissingFiles)
                {
                    var playButton = CreatePlayButton();
                    buttonPanel.Children.Add(playButton);

                    // No status message needed when game is ready
                    LogDebug("Game is ready - no status message needed");
                }
                else
                {
                    // Set message about missing files with details
                    statusMessage = missingFilesInfo.Message;
                    isErrorMessage = missingFilesInfo.HasBaseClientFiles;
                    LogDebug($"Game files missing - will show status message: {statusMessage}");
                }
            }
            else if (!PlatformSupport.SupportsGameLaunching)
            {
                // Show platform limitation message
                statusMessage = PlatformSupport.GetPlatformLimitationMessage();
                isErrorMessage = false;
                LogDebug($"Platform doesn't support game launching - showing limitation message for {PlatformSupport.GetPlatformName()}");
            }
            else if (!hasValidPath)
            {
                // Set error message about missing install path
                statusMessage = "No install path configured.\nPlease go to Settings to select an installation location.";
                isErrorMessage = true;
                LogDebug("No valid install path - will show error message");
            }

            // Only add button panel if it has children
            if (buttonPanel.Children.Count > 0)
            {
                homeContent.Children.Add(buttonPanel);
            }

            // Add status message if needed
            if (!string.IsNullOrEmpty(statusMessage))
            {
                var statusTextBlock = new TextBlock
                {
                    Name = "StatusMessage",
                    Text = statusMessage,
                    FontSize = 14,
                    Foreground = isErrorMessage ?
                        Avalonia.Media.Brush.Parse(Constants.COLOR_ERROR_RED) :
                        Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(20, 0, 20, 100), // Space above buttons
                    MaxWidth = 600,
                    Opacity = 1.0
                };

                homeContent.Children.Add(statusTextBlock);
                LogDebug($"Added status message to home content: '{statusMessage}'");
            }

            // Set the content once with everything in place
            _contentArea.Content = homeContent;

            LogDebug("NavigateToHome() completed");
        }

        private MissingFilesInfo GetMissingFilesInfo()
        {
            var installPath = _configManager.InstallPath;
            var missingFiles = new List<string>();
            var missingBaseClientFiles = new List<string>();
            var missingProjectEpochFiles = new List<string>();

            if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
            {
                return new MissingFilesInfo
                {
                    HasMissingFiles = true,
                    HasBaseClientFiles = false,
                    Message = "Install directory does not exist.\nPlease check your settings and ensure the installation path is valid."
                };
            }

            // Check for base WoW client files
            var wowExePath = Path.Combine(installPath, Constants.WOW_EXECUTABLE);
            if (!File.Exists(wowExePath))
            {
                missingBaseClientFiles.Add(Constants.WOW_EXECUTABLE);
            }

            // Check for Project Epoch files
            var projectEpochExePath = Path.Combine(installPath, Constants.PROJECT_EPOCH_EXECUTABLE);
            if (!File.Exists(projectEpochExePath))
            {
                missingProjectEpochFiles.Add(Constants.PROJECT_EPOCH_EXECUTABLE);
            }

            // Build the message based on what's missing
            if (missingBaseClientFiles.Count == 0 && missingProjectEpochFiles.Count == 0)
            {
                return new MissingFilesInfo
                {
                    HasMissingFiles = false,
                    HasBaseClientFiles = false,
                    Message = ""
                };
            }

            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.AppendLine("Missing game files:");
            messageBuilder.AppendLine();

            // Show missing base client files first
            if (missingBaseClientFiles.Count > 0)
            {
                messageBuilder.AppendLine("Base WoW Client files:");
                foreach (var file in missingBaseClientFiles)
                {
                    messageBuilder.AppendLine($"• {file}");
                }
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("You need to obtain the World of Warcraft 3.3.5a client");
                messageBuilder.AppendLine("and place it in your installation directory.");

                if (missingProjectEpochFiles.Count > 0)
                {
                    messageBuilder.AppendLine();
                }
            }

            // Show missing Project Epoch files
            if (missingProjectEpochFiles.Count > 0)
            {
                if (missingBaseClientFiles.Count > 0)
                {
                    messageBuilder.AppendLine("Project Epoch files:");
                }

                foreach (var file in missingProjectEpochFiles)
                {
                    messageBuilder.AppendLine($"• {file}");
                }
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"Click '{Constants.CHECK_FOR_UPDATES_BUTTON_TEXT}' to download Project Epoch files.");
            }

            return new MissingFilesInfo
            {
                HasMissingFiles = true,
                HasBaseClientFiles = missingBaseClientFiles.Count > 0,
                Message = messageBuilder.ToString().TrimEnd()
            };
        }

        private class MissingFilesInfo
        {
            public bool HasMissingFiles { get; set; }
            public bool HasBaseClientFiles { get; set; }
            public string Message { get; set; } = "";
        }

        private Button CreateCheckUpdatesButton()
        {
            var checkUpdatesButton = new Button
            {
                Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT,
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.Normal,
                Foreground = Avalonia.Media.Brushes.White,
                Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN),
                Padding = new Avalonia.Thickness(40, 15),
                CornerRadius = new Avalonia.CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            checkUpdatesButton.PointerEntered += (s, e) => checkUpdatesButton.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN_HOVER);
            checkUpdatesButton.PointerExited += (s, e) => checkUpdatesButton.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
            checkUpdatesButton.Click += OnCheckUpdatesButtonClick;

            return checkUpdatesButton;
        }

        private Button CreatePlayButton()
        {
            var playButton = new Button
            {
                Content = Constants.PLAY_BUTTON_TEXT,
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.White,
                Background = Avalonia.Media.Brush.Parse(Constants.COLOR_PRIMARY_BLUE),
                Padding = new Avalonia.Thickness(40, 15),
                CornerRadius = new Avalonia.CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            playButton.PointerEntered += (s, e) => playButton.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_PRIMARY_BLUE_HOVER);
            playButton.PointerExited += (s, e) => playButton.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_PRIMARY_BLUE);
            playButton.Click += OnPlayButtonClick;

            return playButton;
        }

        private async void OnCheckUpdatesButtonClick(object? sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            try
            {
                LogDebug("Check for updates button clicked");

                // Cancel any existing update check
                _updateCheckCancellation?.Cancel();
                _updateCheckCancellation = new CancellationTokenSource();

                // Update button state
                button.Content = Constants.CHECKING_BUTTON_TEXT;
                button.IsEnabled = false;

                var result = await _updateService.CheckForUpdatesAsync(_updateCheckCancellation.Token);

                if (result.Success)
                {
                    if (result.UpdatesAvailable)
                    {
                        var sizeText = FormatFileSize(result.TotalSize);
                        LogDebug($"Updates found: {result.FilesToUpdate.Count} files, {sizeText}");

                        // Update button to show download option
                        button.Content = $"Download Updates ({sizeText})";
                        button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_WARNING_ORANGE);

                        // Change button click handler to download
                        button.Click -= OnCheckUpdatesButtonClick;
                        button.Click += async (s, e) => await OnDownloadUpdatesButtonClick(s, e, result.FilesToUpdate);
                    }
                    else
                    {
                        LogDebug("No updates available");
                        button.Content = Constants.UP_TO_DATE_BUTTON_TEXT;
                        button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);

                        // Reset button after 3 seconds
                        _ = Task.Delay(3000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (button.Content?.ToString() == Constants.UP_TO_DATE_BUTTON_TEXT)
                            {
                                button.Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT;
                                button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
                            }
                        }));
                    }
                }
                else
                {
                    LogError($"Update check failed: {result.ErrorMessage}");
                    button.Content = Constants.CHECK_FAILED_BUTTON_TEXT;
                    button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_ERROR_RED);

                    // Reset button after 3 seconds
                    _ = Task.Delay(3000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (button.Content?.ToString() == Constants.CHECK_FAILED_BUTTON_TEXT)
                        {
                            button.Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT;
                            button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
                        }
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                LogDebug("Update check was cancelled");
                button.Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT;
                button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
            }
            catch (Exception ex)
            {
                LogError($"Update check error: {ex.Message}");
                button.Content = Constants.CHECK_FAILED_BUTTON_TEXT;
                button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_ERROR_RED);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async Task OnDownloadUpdatesButtonClick(object? sender, RoutedEventArgs e, List<GameFile> filesToDownload)
        {
            var button = sender as Button;
            if (button == null || filesToDownload == null) return;

            try
            {
                LogDebug($"Starting download of {filesToDownload.Count} files");

                // Create cancellation token for this download
                _updateCheckCancellation?.Cancel();
                _updateCheckCancellation = new CancellationTokenSource();

                // Update button state
                button.Content = Constants.DOWNLOADING_BUTTON_TEXT;
                button.IsEnabled = false;
                button.Background = Avalonia.Media.Brush.Parse("#2196F3");

                // Create and show progress dialog
                var progressDialog = new DownloadProgressDialog(_updateCheckCancellation);

                // Show dialog without blocking
                progressDialog.Show(this);

                // Set up progress reporting
                var progress = new Progress<DownloadProgress>(p =>
                {
                    progressDialog.UpdateProgress(p);
                });

                // Start the download
                var downloadResult = await _updateService.DownloadFilesAsync(
                    filesToDownload,
                    progress,
                    _updateCheckCancellation.Token);

                if (downloadResult.Success)
                {
                    LogDebug($"Download completed successfully: {downloadResult.FilesDownloaded} files, {downloadResult.TotalBytesDownloaded} bytes");

                    // Update progress dialog
                    progressDialog.SetCompleted(true, $"Download complete!\n\n{downloadResult.FilesDownloaded} files downloaded\n{FormatFileSize(downloadResult.TotalBytesDownloaded)} total");

                    // Update button state
                    button.Content = Constants.DOWNLOAD_COMPLETE_BUTTON_TEXT;
                    button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);

                    // Refresh the home view to show play button if files are now present
                    NavigateToHome();
                }
                else
                {
                    LogError($"{Constants.DOWNLOAD_FAILED_BUTTON_TEXT}: {downloadResult.ErrorMessage}");

                    // Update progress dialog
                    progressDialog.SetCompleted(false, $"{Constants.DOWNLOAD_FAILED_BUTTON_TEXT}!\n\n{downloadResult.ErrorMessage}");

                    // Update button state
                    button.Content = Constants.DOWNLOAD_FAILED_BUTTON_TEXT;
                    button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_ERROR_RED);

                    // Reset button after 5 seconds
                    _ = Task.Delay(5000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (button.Content?.ToString() == Constants.DOWNLOAD_FAILED_BUTTON_TEXT)
                        {
                            button.Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT;
                            button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
                            button.Click -= OnCheckUpdatesButtonClick;
                            button.Click += OnCheckUpdatesButtonClick;
                        }
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                LogDebug("Download was cancelled");

                // Update button state
                button.Content = Constants.DOWNLOAD_CANCELLED_BUTTON_TEXT;
                button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_WARNING_ORANGE);

                // Reset button after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (button.Content?.ToString() == Constants.DOWNLOAD_CANCELLED_BUTTON_TEXT)
                    {
                        button.Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT;
                        button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
                        button.Click -= OnCheckUpdatesButtonClick;
                        button.Click += OnCheckUpdatesButtonClick;
                    }
                }));
            }
            catch (Exception ex)
            {
                LogError($"Download error: {ex.Message}");

                // Update button state
                button.Content = Constants.DOWNLOAD_ERROR_BUTTON_TEXT;
                button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_ERROR_RED);

                // Reset button after 5 seconds
                _ = Task.Delay(5000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (button.Content?.ToString() == Constants.DOWNLOAD_ERROR_BUTTON_TEXT)
                    {
                        button.Content = Constants.CHECK_FOR_UPDATES_BUTTON_TEXT;
                        button.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN);
                        button.Click -= OnCheckUpdatesButtonClick;
                        button.Click += OnCheckUpdatesButtonClick;
                    }
                }));
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            double number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        private void OnPlayButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("Play button clicked");
                LaunchGame(sender as Button);
            }
            catch (Exception ex)
            {
                LogError($"Play button error: {ex.Message}");
            }
        }


        private void LaunchGame(Button? playButton)
        {
            try
            {
                // First check if platform supports game launching
                if (!PlatformSupport.SupportsGameLaunching)
                {
                    LogError($"Game launching not supported on {PlatformSupport.GetPlatformName()}");
                    ShowGameLaunchError($"Game launching is not supported on {PlatformSupport.GetPlatformName()}. The game requires Windows to run.");
                    ResetPlayButton(playButton);
                    return;
                }

                var installPath = _configManager.InstallPath;
                if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
                {
                    LogError($"Invalid install path: {installPath}");
                    ShowGameLaunchError("Install path is not configured or does not exist. Please check your settings.");
                    return;
                }

                var gameExePath = Path.Combine(installPath, Constants.PROJECT_EPOCH_EXECUTABLE);
                if (!File.Exists(gameExePath))
                {
                    LogError($"Game executable not found: {gameExePath}");
                    ShowGameLaunchError("Project-Epoch.exe not found. Please check for updates to download the game files.");
                    return;
                }

                LogDebug($"Launching game from: {gameExePath}");

                // Update button state to show launching
                if (playButton != null)
                {
                    playButton.Content = Constants.LAUNCHING_BUTTON_TEXT;
                    playButton.IsEnabled = false;
                }

                // Launch the game
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = gameExePath,
                    WorkingDirectory = installPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                var gameProcess = Process.Start(processStartInfo);

                if (gameProcess != null)
                {
                    LogDebug($"Game launched successfully. Process ID: {gameProcess.Id}");

                    // Show success message that stays visible
                    ShowHomeStatusMessage("Game launched successfully! Enjoy playing Project Epoch.", isError: false);

                    // Minimize the launcher after a short delay
                    _ = Task.Delay(2000).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                this.WindowState = WindowState.Minimized;
                            }
                            catch (Exception ex)
                            {
                                LogError($"Error minimizing launcher: {ex.Message}");
                            }
                        });
                    });

                    // Monitor the game process in the background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await gameProcess.WaitForExitAsync();
                            LogDebug($"Game process exited with code: {gameProcess.ExitCode}");

                            // Restore launcher when game closes
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                try
                                {
                                    this.WindowState = WindowState.Normal;
                                    this.Activate();

                                    // Reset play button
                                    if (playButton != null)
                                    {
                                        playButton.Content = Constants.PLAY_BUTTON_TEXT;
                                        playButton.IsEnabled = true;
                                    }

                                    // Clear the launch success message when returning
                                    ClearHomeStatusMessage();
                                }
                                catch (Exception ex)
                                {
                                    LogError($"Error restoring launcher after game exit: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error monitoring game process: {ex.Message}");
                        }
                        finally
                        {
                            gameProcess?.Dispose();
                        }
                    });
                }
                else
                {
                    LogError("Failed to start game process");
                    ShowGameLaunchError("Failed to launch the game. Please try again.");

                    // Reset button
                    if (playButton != null)
                    {
                        playButton.Content = Constants.PLAY_BUTTON_TEXT;
                        playButton.IsEnabled = true;
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"Access denied launching game: {ex.Message}");
                ShowGameLaunchError("Access denied. Please run the launcher as administrator or check file permissions.");
                ResetPlayButton(playButton);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LogError($"Win32 error launching game: {ex.Message}");
                ShowGameLaunchError($"Failed to launch game: {ex.Message}");
                ResetPlayButton(playButton);
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error launching game: {ex.Message}");
                ShowGameLaunchError($"An unexpected error occurred: {ex.Message}");
                ResetPlayButton(playButton);
            }
        }

        private void ResetPlayButton(Button? playButton)
        {
            try
            {
                if (playButton != null)
                {
                    playButton.Content = Constants.PLAY_BUTTON_TEXT;
                    playButton.IsEnabled = true;
                }
                else
                {
                    // Fallback: Find and reset the play button in the UI
                    var homeContent = _contentArea?.Content as Grid;
                    if (homeContent != null)
                    {
                        foreach (var child in homeContent.Children)
                        {
                            if (child is StackPanel buttonPanel)
                            {
                                foreach (var button in buttonPanel.Children)
                                {
                                    if (button is Button btn && btn.Content?.ToString() == Constants.LAUNCHING_BUTTON_TEXT)
                                    {
                                        btn.Content = Constants.PLAY_BUTTON_TEXT;
                                        btn.IsEnabled = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error resetting play button: {ex.Message}");
            }
        }

        private void ShowGameLaunchError(string message)
        {
            try
            {
                LogError($"Game launch error: {message}");

                // Show error message in the home window (persistent)
                ShowHomeStatusMessage(message, isError: true);
            }
            catch (Exception ex)
            {
                LogError($"Error showing launch error: {ex.Message}");
            }
        }

        private void ShowHomeStatusMessage(string message, bool isError = false)
        {
            try
            {
                LogDebug($"ShowHomeStatusMessage called with: '{message}' (Error: {isError})");

                var homeContent = _contentArea?.Content as Grid;
                if (homeContent == null)
                {
                    LogError("HomeContent is null, cannot show status message");
                    return;
                }

                // Remove any existing status message
                var existingStatus = homeContent.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Name == "StatusMessage");
                if (existingStatus != null)
                {
                    LogDebug($"Removing existing status message: '{existingStatus.Text}'");
                    homeContent.Children.Remove(existingStatus);
                }

                // Don't add empty messages
                if (string.IsNullOrWhiteSpace(message))
                {
                    LogDebug("Skipping empty message");
                    return;
                }

                // Create new status message
                var statusMessage = new TextBlock
                {
                    Name = "StatusMessage",
                    Text = message,
                    FontSize = 14,
                    Foreground = isError ?
                        Avalonia.Media.Brush.Parse(Constants.COLOR_ERROR_RED) :
                        Avalonia.Media.Brush.Parse(Constants.COLOR_SUCCESS_GREEN),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(20, 0, 20, 100), // Space above buttons
                    MaxWidth = 600,
                    Opacity = 1.0 // Show immediately without animation
                };

                // Add the message directly to the grid
                homeContent.Children.Add(statusMessage);

                LogDebug($"Successfully added status message to UI: '{message}'");
            }
            catch (Exception ex)
            {
                LogError($"Error showing status message: {ex.Message}");
            }
        }

        private void ClearHomeStatusMessage()
        {
            try
            {
                var homeContent = _contentArea?.Content as Grid;
                if (homeContent == null) return;

                var statusMessage = homeContent.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Name == "StatusMessage");

                if (statusMessage != null)
                {
                    homeContent.Children.Remove(statusMessage);
                    LogDebug("Cleared status message");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error clearing status message: {ex.Message}");
            }
        }

        private void NavigateToSettings()
        {
            if (_contentArea == null) return;

            var settingsGrid = new Grid();
            settingsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            settingsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            // Main settings content
            var settingsContent = new StackPanel
            {
                Spacing = 20,
                Margin = new Avalonia.Thickness(0, 20, 0, 0)
            };

            // Title
            var titleText = new TextBlock
            {
                Text = Constants.SETTINGS_NAVIGATION,
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse(Constants.COLOR_PRIMARY_GOLD),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0, 0, 0, 30)
            };

            // Install Path Section
            var installPathPanel = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };

            var installPathLabel = new TextBlock
            {
                Text = "Install Path",
                FontSize = 14,
                Foreground = Avalonia.Media.Brush.Parse("#B0B0C0"),
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };

            var installPathTextBox = new TextBox
            {
                Text = GetInstallPath(),
                Width = 500,
                IsReadOnly = true,
                Background = Avalonia.Media.Brush.Parse("#33FFFFFF"),
                BorderBrush = Avalonia.Media.Brush.Parse("#555555"),
                Foreground = Avalonia.Media.Brush.Parse("#E0E0E0"),
                Padding = new Avalonia.Thickness(10, 8),
                CornerRadius = new Avalonia.CornerRadius(4)
            };

            var changePathButton = new Button
            {
                Content = "Change Install Path",
                FontSize = 14,
                Foreground = Avalonia.Media.Brushes.White,
                Background = Avalonia.Media.Brush.Parse("#555555"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding = new Avalonia.Thickness(20, 8),
                CornerRadius = new Avalonia.CornerRadius(4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Margin = new Avalonia.Thickness(0, 5, 0, 0),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Add hover effect to button
            changePathButton.PointerEntered += (s, e) =>
            {
                changePathButton.Background = Avalonia.Media.Brush.Parse(Constants.COLOR_SECONDARY_GRAY);
            };

            changePathButton.PointerExited += (s, e) =>
            {
                changePathButton.Background = Avalonia.Media.Brush.Parse("#555555");
            };

            changePathButton.Click += async (s, e) => await OnChangeInstallPathClick(installPathTextBox);

            // Assemble the install path section
            installPathPanel.Children.Add(installPathLabel);
            installPathPanel.Children.Add(installPathTextBox);
            installPathPanel.Children.Add(changePathButton);

            // Add everything to settings content
            settingsContent.Children.Add(titleText);
            settingsContent.Children.Add(installPathPanel);

            Grid.SetRow(settingsContent, 0);
            settingsGrid.Children.Add(settingsContent);

            _contentArea.Content = settingsGrid;
        }

        private string GetInstallPath()
        {
            return _configManager.InstallPath;
        }

        private async Task OnChangeInstallPathClick(TextBox installPathTextBox)
        {
            try
            {
                LogDebug("Opening folder dialog for install path selection");

                // Get the top level window (required for storage provider)
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    LogError("Could not get top level window");
                    return;
                }

                // Create folder picker options
                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Install Location",
                    AllowMultiple = false,
                    SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(GetInstallPath())
                };

                // Open the folder picker
                var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

                if (result != null && result.Count > 0)
                {
                    var selectedFolder = result[0];
                    var path = selectedFolder.Path.LocalPath;

                    LogDebug($"New install path selected: {path}");
                    installPathTextBox.Text = path;

                    // Save the new path to configuration file
                    SaveInstallPath(path);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error selecting install path: {ex.Message}");
            }
        }

        private void SaveInstallPath(string path)
        {
            _configManager.InstallPath = path;
            LogDebug($"Install path saved: {path}");

            // Refresh home view if currently showing home
            if (_homeButton?.IsChecked == true)
            {
                NavigateToHome();
            }
        }

        #region Logging

        private void LogDebug(string message)
        {
            Debug.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private void LogError(string message)
        {
            Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        #endregion
    }
}