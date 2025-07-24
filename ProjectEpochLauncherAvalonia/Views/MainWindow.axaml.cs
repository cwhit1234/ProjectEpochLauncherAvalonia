using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace ProjectEpochLauncherAvalonia
{
    public partial class MainWindow : Window
    {
        private TransitioningContentControl? _contentArea;
        private RadioButton? _homeButton;
        private RadioButton? _settingsButton;

        public MainWindow()
        {
            InitializeComponent();

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
            var welcomeText = new TextBlock
            {
                Text = "Welcome to Project Epoch",
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#F0E68C"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            homeContent.Children.Add(welcomeText);

            _contentArea.Content = homeContent;
        }

        private void NavigateToSettings()
        {
            if (_contentArea == null) return;

            var settingsContent = new StackPanel
            {
                Spacing = 20,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = "Settings",
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#F0E68C"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var placeholderText = new TextBlock
            {
                Text = "Settings options will be displayed here",
                FontSize = 16,
                Foreground = Avalonia.Media.Brush.Parse("#B0B0C0"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            settingsContent.Children.Add(titleText);
            settingsContent.Children.Add(placeholderText);

            _contentArea.Content = settingsContent;
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