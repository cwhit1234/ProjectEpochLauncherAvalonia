using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProjectEpochLauncherAvalonia
{
    public partial class MainWindow : Window
    {
        private TransitioningContentControl? _contentArea;
        private RadioButton? _homeButton;
        private RadioButton? _settingsButton;
        private ConfigurationManager _configManager;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize configuration manager
            _configManager = new ConfigurationManager();

            // Wire up navigation after initialization
            _contentArea = this.FindControl<TransitioningContentControl>("ContentArea");
            _homeButton = this.FindControl<RadioButton>("HomeButton");
            _settingsButton = this.FindControl<RadioButton>("SettingsButton");

            if (_homeButton != null)
                _homeButton.Click += OnNavigationClick;

            if (_settingsButton != null)
                _settingsButton.Click += OnNavigationClick;

            // Set initial content
            NavigateToHome();

            LogDebug("MainWindow initialized successfully");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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

        private void NavigateToHome()
        {
            if (_contentArea == null) return;

            var homeContent = new Grid();

            // Welcome text in center
            var welcomeText = new TextBlock
            {
                Text = "Welcome to Project Epoch",
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#F0E68C"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Play button in bottom right
            var playButton = new Button
            {
                Content = "PLAY",
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.White,
                Background = Avalonia.Media.Brush.Parse("#1E88E5"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Padding = new Avalonia.Thickness(40, 15),
                CornerRadius = new Avalonia.CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            // Add hover effect styling
            playButton.PointerEntered += (s, e) =>
            {
                playButton.Background = Avalonia.Media.Brush.Parse("#1976D2");
                LogDebug("Play button hover state");
            };

            playButton.PointerExited += (s, e) =>
            {
                playButton.Background = Avalonia.Media.Brush.Parse("#1E88E5");
            };

            playButton.Click += OnPlayButtonClick;

            homeContent.Children.Add(welcomeText);
            homeContent.Children.Add(playButton);

            _contentArea.Content = homeContent;
        }

        private void OnPlayButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("Play button clicked");
                // TODO: Implement game launch logic here
                // For now, just log the action
                Debug.WriteLine("Launching Project Epoch...");
            }
            catch (Exception ex)
            {
                LogError($"Play button error: {ex.Message}");
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
                Text = "Settings",
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#F0E68C"),
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
                Margin = new Avalonia.Thickness(0, 5, 0, 0)
            };

            // Add hover effect to button
            changePathButton.PointerEntered += (s, e) =>
            {
                changePathButton.Background = Avalonia.Media.Brush.Parse("#666666");
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