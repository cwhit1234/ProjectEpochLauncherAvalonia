using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ProjectEpochLauncherAvalonia.Services;
using System;
using System.Threading;

namespace ProjectEpochLauncherAvalonia
{
    public partial class DownloadProgressDialog : Window
    {
        private TextBlock? _statusText;
        private TextBlock? _fileProgressLabel;
        private ProgressBar? _fileProgressBar;
        private TextBlock? _overallProgressLabel;
        private ProgressBar? _overallProgressBar;
        private Button? _cancelButton;

        private CancellationTokenSource? _cancellationTokenSource;

        public DownloadProgressDialog()
        {
            InitializeComponent();
            WireUpControls();
        }

        public DownloadProgressDialog(CancellationTokenSource cancellationTokenSource) : this()
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void WireUpControls()
        {
            _statusText = this.FindControl<TextBlock>("StatusText");
            _fileProgressLabel = this.FindControl<TextBlock>("FileProgressLabel");
            _fileProgressBar = this.FindControl<ProgressBar>("FileProgressBar");
            _overallProgressLabel = this.FindControl<TextBlock>("OverallProgressLabel");
            _overallProgressBar = this.FindControl<ProgressBar>("OverallProgressBar");
            _cancelButton = this.FindControl<Button>("CancelButton");

            if (_cancelButton != null)
            {
                _cancelButton.Click += OnCancelClick;
            }
        }

        public void UpdateProgress(DownloadProgress progress)
        {
            if (_statusText != null)
                _statusText.Text = progress.Status;

            if (_fileProgressLabel != null)
                _fileProgressLabel.Text = $"File: {progress.FileName} ({progress.FileIndex}/{progress.TotalFiles})";

            if (_fileProgressBar != null)
                _fileProgressBar.Value = progress.FileProgress;

            if (_overallProgressLabel != null)
            {
                var overallPercent = progress.OverallProgress;
                var downloadedMB = progress.BytesDownloaded / (1024.0 * 1024.0);
                var totalMB = progress.TotalBytes / (1024.0 * 1024.0);
                _overallProgressLabel.Text = $"Overall: {overallPercent:F1}% ({downloadedMB:F1} MB / {totalMB:F1} MB)";
            }

            if (_overallProgressBar != null)
                _overallProgressBar.Value = progress.OverallProgress;
        }

        public void SetCompleted(bool success, string message)
        {
            if (_statusText != null)
                _statusText.Text = message;

            if (_cancelButton != null)
            {
                _cancelButton.Content = "Close";
                _cancelButton.Background = success ?
                    Avalonia.Media.Brush.Parse("#4CAF50") :
                    Avalonia.Media.Brush.Parse("#F44336");
                _cancelButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                _cancelButton.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
            }

            if (success)
            {
                if (_fileProgressBar != null)
                    _fileProgressBar.Value = 100;

                if (_overallProgressBar != null)
                    _overallProgressBar.Value = 100;
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            if (_cancelButton?.Content?.ToString() == "Close")
            {
                Close();
                return;
            }

            // Cancel the download
            _cancellationTokenSource?.Cancel();

            if (_cancelButton != null)
            {
                _cancelButton.Content = "Cancelling...";
                _cancelButton.IsEnabled = false;
            }

            if (_statusText != null)
            {
                _statusText.Text = "Cancelling download...";
            }
        }
    }
}