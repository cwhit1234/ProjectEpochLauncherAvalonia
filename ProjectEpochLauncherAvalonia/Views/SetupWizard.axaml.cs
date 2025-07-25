using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ProjectEpochLauncherAvalonia.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEpochLauncherAvalonia
{
    public partial class SetupWizard : Window
    {
        private TransitioningContentControl? _wizardContent;
        private Button? _backButton;
        private Button? _nextButton;

        private ConfigurationManager _configManager;
        private UpdateService _updateService;
        private CancellationTokenSource? _cancellationTokenSource;

        private int _currentStep = 0;
        private string _selectedInstallPath = string.Empty;
        private bool _forceClose = false; // Flag to bypass confirmation dialog

        public SetupWizard(ConfigurationManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _updateService = new UpdateService(_configManager);

            InitializeComponent();
            WireUpControls();
            ShowCurrentStep();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void WireUpControls()
        {
            _wizardContent = this.FindControl<TransitioningContentControl>("WizardContent");
            _backButton = this.FindControl<Button>("BackButton");
            _nextButton = this.FindControl<Button>("NextButton");

            if (_backButton != null)
                _backButton.Click += OnBackClick;

            if (_nextButton != null)
                _nextButton.Click += OnNextClick;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Cancel any ongoing operations immediately
            try
            {
                _cancellationTokenSource?.Cancel();
                LogDebug("Cancelled ongoing operations due to window close");
            }
            catch (Exception ex)
            {
                LogError($"Error cancelling operations: {ex.Message}");
            }

            // Dispose resources
            try
            {
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                LogError($"Error disposing cancellation token: {ex.Message}");
            }

            // Log whether setup was completed or cancelled
            if (_configManager.SetupCompleted)
            {
                LogDebug("Setup wizard closed - setup completed successfully");
            }
            else
            {
                LogDebug("Setup wizard closed - setup was cancelled or incomplete");
            }

            base.OnClosed(e);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // If we're forcing close or setup is completed, allow it
            if (_forceClose || _configManager.SetupCompleted || _currentStep >= 3)
            {
                // Make sure to cancel any ongoing operations
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (Exception ex)
                {
                    LogError($"Error cancelling operations during close: {ex.Message}");
                }

                base.OnClosing(e);
                return;
            }

            // If we're currently downloading (step 2), warn about active download
            if (_currentStep == 2 && _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Cancel the close and show download-specific confirmation
                e.Cancel = true;
                ShowDownloadExitConfirmation();
                return;
            }

            // For other steps, show normal exit confirmation
            if (!_configManager.SetupCompleted && _currentStep < 3)
            {
                // Cancel the close and show confirmation
                e.Cancel = true;
                ShowExitConfirmation();
            }

            base.OnClosing(e);
        }

        private async void ShowDownloadExitConfirmation()
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Download in Progress",
                    Width = 450,
                    Height = 220,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var content = new StackPanel
                {
                    Spacing = 20,
                    Margin = new Avalonia.Thickness(30)
                };

                var messageText = new TextBlock
                {
                    Text = "A download is currently in progress.\n\nCancelling now will stop the download and you'll need to start over. Are you sure you want to exit?",
                    FontSize = 14,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 15,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                var continueButton = new Button
                {
                    Content = "Continue Download",
                    Width = 140,
                    Height = 35,
                    Background = Avalonia.Media.Brush.Parse("#4CAF50"),
                    Foreground = Avalonia.Media.Brushes.White,
                    CornerRadius = new Avalonia.CornerRadius(6),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var cancelButton = new Button
                {
                    Content = "Cancel Download",
                    Width = 140,
                    Height = 35,
                    Background = Avalonia.Media.Brush.Parse("#F44336"),
                    Foreground = Avalonia.Media.Brushes.White,
                    CornerRadius = new Avalonia.CornerRadius(6),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                continueButton.Click += (s, e) => dialog.Close(false);
                cancelButton.Click += (s, e) => dialog.Close(true);

                buttonPanel.Children.Add(continueButton);
                buttonPanel.Children.Add(cancelButton);

                content.Children.Add(messageText);
                content.Children.Add(buttonPanel);

                dialog.Content = content;

                var result = await dialog.ShowDialog<bool>(this);

                if (result) // User chose to cancel download and exit
                {
                    LogDebug("User confirmed cancellation of active download");

                    // Cancel the download first
                    _cancellationTokenSource?.Cancel();

                    // Set flag to force close and then close
                    _forceClose = true;
                    this.Close();
                }
                else
                {
                    LogDebug("User chose to continue download");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error showing download exit confirmation: {ex.Message}");
                // If dialog fails, cancel download and allow the close
                _cancellationTokenSource?.Cancel();
                _forceClose = true;
                this.Close();
            }
        }

        private async void ShowExitConfirmation()
        {
            try
            {
                // Create a simple message box dialog
                var dialog = new Window
                {
                    Title = "Exit Setup",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var content = new StackPanel
                {
                    Spacing = 20,
                    Margin = new Avalonia.Thickness(30)
                };

                var messageText = new TextBlock
                {
                    Text = "Are you sure you want to exit the setup?\n\nProject Epoch has not been installed yet. You can run the setup again later if needed.",
                    FontSize = 14,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 15,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                var cancelButton = new Button
                {
                    Content = "Continue Setup",
                    Width = 120,
                    Height = 35,
                    Background = Avalonia.Media.Brush.Parse("#4CAF50"),
                    Foreground = Avalonia.Media.Brushes.White,
                    CornerRadius = new Avalonia.CornerRadius(6),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var exitButton = new Button
                {
                    Content = "Exit",
                    Width = 120,
                    Height = 35,
                    Background = Avalonia.Media.Brush.Parse("#F44336"),
                    Foreground = Avalonia.Media.Brushes.White,
                    CornerRadius = new Avalonia.CornerRadius(6),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                cancelButton.Click += (s, e) => dialog.Close(false);
                exitButton.Click += (s, e) => dialog.Close(true);

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(exitButton);

                content.Children.Add(messageText);
                content.Children.Add(buttonPanel);

                dialog.Content = content;

                var result = await dialog.ShowDialog<bool>(this);

                if (result) // User chose to exit
                {
                    LogDebug("User confirmed exit from setup wizard");

                    // Set flag to force close and then close
                    _forceClose = true;
                    this.Close();
                }
                else
                {
                    LogDebug("User chose to continue setup");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error showing exit confirmation: {ex.Message}");
                // If dialog fails, allow the close
                _forceClose = true;
                this.Close();
            }
        }

        private void ShowCurrentStep()
        {
            if (_wizardContent == null) return;

            switch (_currentStep)
            {
                case 0:
                    ShowWelcomeStep();
                    break;
                case 1:
                    ShowInstallPathStep();
                    break;
                case 2:
                    ShowDownloadStep();
                    break;
                case 3:
                    ShowCompleteStep();
                    break;
            }

            UpdateNavigationButtons();
        }

        private void ShowWelcomeStep()
        {
            var content = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var welcomeText = new TextBlock
            {
                Text = "Project Epoch is a World of Warcraft private server that brings back the classic experience with modern improvements.",
                FontSize = 16,
                Foreground = Avalonia.Media.Brush.Parse("#E0E0E0"),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            content.Children.Add(welcomeText);

            _wizardContent!.Content = content;
        }

        private void ShowInstallPathStep()
        {
            var content = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            var titleText = new TextBlock
            {
                Text = "Choose Installation Location",
                FontSize = 20,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#F0E68C"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };

            var explanationText = new TextBlock
            {
                Text = "Select where you'd like to install Project Epoch.",
                FontSize = 14,
                Foreground = Avalonia.Media.Brush.Parse("#B0B0C0"),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            // Install path selection
            var pathPanel = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            var pathLabel = new TextBlock
            {
                Text = "Installation Path:",
                FontSize = 14,
                Foreground = Avalonia.Media.Brush.Parse("#E0E0E0"),
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };

            var pathContainer = new Grid();
            pathContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            pathContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var pathTextBox = new TextBox
            {
                Text = _selectedInstallPath.Length > 0 ? _selectedInstallPath : _configManager.InstallPath,
                IsReadOnly = true,
                Background = Avalonia.Media.Brush.Parse("#33FFFFFF"),
                BorderBrush = Avalonia.Media.Brush.Parse("#555555"),
                Foreground = Avalonia.Media.Brush.Parse("#E0E0E0"),
                Padding = new Avalonia.Thickness(10, 8),
                CornerRadius = new Avalonia.CornerRadius(4),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var browseButton = new Button
            {
                Content = "Browse...",
                Width = 100,
                Height = 35,
                Margin = new Avalonia.Thickness(10, 0, 0, 0),
                Background = Avalonia.Media.Brush.Parse("#4CAF50"),
                Foreground = Avalonia.Media.Brushes.White,
                CornerRadius = new Avalonia.CornerRadius(4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            browseButton.PointerEntered += (s, e) => browseButton.Background = Avalonia.Media.Brush.Parse("#45A049");
            browseButton.PointerExited += (s, e) => browseButton.Background = Avalonia.Media.Brush.Parse("#4CAF50");
            browseButton.Click += async (s, e) => await OnBrowseInstallPath(pathTextBox);

            Grid.SetColumn(pathTextBox, 0);
            Grid.SetColumn(browseButton, 1);
            pathContainer.Children.Add(pathTextBox);
            pathContainer.Children.Add(browseButton);

            pathPanel.Children.Add(pathLabel);
            pathPanel.Children.Add(pathContainer);

            content.Children.Add(titleText);
            content.Children.Add(explanationText);
            content.Children.Add(pathPanel);

            _wizardContent!.Content = content;

            // Update selected path if textbox has content
            if (!string.IsNullOrEmpty(pathTextBox.Text))
            {
                _selectedInstallPath = pathTextBox.Text;
            }
        }

        private async Task OnBrowseInstallPath(TextBox pathTextBox)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Installation Location",
                    AllowMultiple = false
                };

                var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

                if (result != null && result.Count > 0)
                {
                    var selectedPath = result[0].Path.LocalPath;
                    pathTextBox.Text = selectedPath;
                    _selectedInstallPath = selectedPath;

                    LogDebug($"Install path selected: {selectedPath}");
                    UpdateNavigationButtons();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error selecting install path: {ex.Message}");
            }
        }

        private void ShowDownloadStep()
        {
            var content = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = "Downloading Game Files",
                FontSize = 20,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#F0E68C"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            var statusText = new TextBlock
            {
                Text = "Preparing download...",
                FontSize = 14,
                Foreground = Avalonia.Media.Brush.Parse("#B0B0C0"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            // Progress bars container
            var progressContainer = new StackPanel
            {
                Spacing = 15,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Width = 400
            };

            // File progress
            var fileProgressLabel = new TextBlock
            {
                Text = "File: Preparing...",
                FontSize = 12,
                Foreground = Avalonia.Media.Brush.Parse("#B0B0C0")
            };

            var fileProgressBar = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = Avalonia.Media.Brush.Parse("#33FFFFFF"),
                Foreground = Avalonia.Media.Brush.Parse("#4CAF50")
            };

            // Overall progress
            var overallProgressLabel = new TextBlock
            {
                Text = "Overall: 0%",
                FontSize = 12,
                Foreground = Avalonia.Media.Brush.Parse("#B0B0C0")
            };

            var overallProgressBar = new ProgressBar
            {
                Height = 12,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = Avalonia.Media.Brush.Parse("#33FFFFFF"),
                Foreground = Avalonia.Media.Brush.Parse("#1E88E5")
            };

            progressContainer.Children.Add(fileProgressLabel);
            progressContainer.Children.Add(fileProgressBar);
            progressContainer.Children.Add(overallProgressLabel);
            progressContainer.Children.Add(overallProgressBar);

            content.Children.Add(titleText);
            content.Children.Add(statusText);
            content.Children.Add(progressContainer);

            _wizardContent!.Content = content;

            // Start download process
            _ = Task.Run(async () => await StartDownloadProcess(statusText, fileProgressLabel, fileProgressBar, overallProgressLabel, overallProgressBar));
        }

        private async Task StartDownloadProcess(
            TextBlock statusText,
            TextBlock fileProgressLabel,
            ProgressBar fileProgressBar,
            TextBlock overallProgressLabel,
            ProgressBar overallProgressBar)
        {
            try
            {
                // Save the install path
                _configManager.InstallPath = _selectedInstallPath;
                LogDebug($"Install path saved: {_selectedInstallPath}");

                // Update status
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusText.Text = "Checking for updates...";
                });

                _cancellationTokenSource = new CancellationTokenSource();

                // Check for updates
                var updateResult = await _updateService.CheckForUpdatesAsync(_cancellationTokenSource.Token);

                if (!updateResult.Success)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        statusText.Text = $"Error: {updateResult.ErrorMessage}";
                    });
                    return;
                }

                if (!updateResult.UpdatesAvailable)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        statusText.Text = "Game is already up to date!";
                        overallProgressBar.Value = 100;
                        _currentStep = 3;
                        ShowCurrentStep();

                        // Mark setup as completed
                        _configManager.MarkSetupCompleted();
                    });
                    return;
                }

                // Start download
                var progress = new Progress<DownloadProgress>(p =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        statusText.Text = p.Status;
                        fileProgressLabel.Text = $"File: {p.FileName} ({p.FileIndex}/{p.TotalFiles})";
                        fileProgressBar.Value = p.FileProgress;

                        var downloadedMB = p.BytesDownloaded / (1024.0 * 1024.0);
                        var totalMB = p.TotalBytes / (1024.0 * 1024.0);
                        overallProgressLabel.Text = $"Overall: {p.OverallProgress:F1}% ({downloadedMB:F1} MB / {totalMB:F1} MB)";
                        overallProgressBar.Value = p.OverallProgress;
                    });
                });

                var downloadResult = await _updateService.DownloadFilesAsync(
                    updateResult.FilesToUpdate,
                    progress,
                    _cancellationTokenSource.Token);

                if (downloadResult.Success)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Mark setup as completed
                        _configManager.MarkSetupCompleted();

                        _currentStep = 3;
                        ShowCurrentStep();
                    });
                }
                else
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        statusText.Text = $"Download failed: {downloadResult.ErrorMessage}";
                    });
                }
            }
            catch (Exception ex)
            {
                LogError($"Download process error: {ex.Message}");
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusText.Text = $"Error: {ex.Message}";
                });
            }
        }

        private void ShowCompleteStep()
        {
            var content = new StackPanel
            {
                Spacing = 25,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = "Setup Complete!",
                FontSize = 24,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#4CAF50"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var messageText = new TextBlock
            {
                Text = "Project Epoch has been successfully installed and is ready to play!\n\nYou can now close this wizard and start your adventure.",
                FontSize = 16,
                Foreground = Avalonia.Media.Brush.Parse("#E0E0E0"),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            content.Children.Add(titleText);
            content.Children.Add(messageText);

            _wizardContent!.Content = content;
        }

        private void UpdateNavigationButtons()
        {
            if (_backButton == null || _nextButton == null) return;

            _backButton.IsVisible = _currentStep > 0;

            switch (_currentStep)
            {
                case 0: // Welcome
                    _nextButton.Content = "Get Started";
                    _nextButton.IsEnabled = true;
                    break;
                case 1: // Install Path
                    _nextButton.Content = "Install";
                    _nextButton.IsEnabled = !string.IsNullOrEmpty(_selectedInstallPath);
                    break;
                case 2: // Download
                    _nextButton.IsVisible = false;
                    break;
                case 3: // Complete
                    _nextButton.Content = "Finish";
                    _nextButton.IsEnabled = true;
                    break;
            }
        }

        private void OnBackClick(object? sender, RoutedEventArgs e)
        {
            if (_currentStep > 0)
            {
                _currentStep--;
                ShowCurrentStep();
            }
        }

        private void OnNextClick(object? sender, RoutedEventArgs e)
        {
            if (_currentStep == 3) // Complete step
            {
                LogDebug("Setup wizard finished - closing window");
                _forceClose = true; // Allow close without confirmation
                Close();
                return;
            }

            if (_currentStep < 3)
            {
                _currentStep++;
                ShowCurrentStep();
            }
        }

        #region Logging

        private void LogDebug(string message)
        {
            Debug.WriteLine($"[SETUP-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private void LogError(string message)
        {
            Debug.WriteLine($"[SETUP-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        #endregion
    }
}