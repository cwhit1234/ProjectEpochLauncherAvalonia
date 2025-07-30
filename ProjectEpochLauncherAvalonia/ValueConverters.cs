using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ProjectEpochLauncherAvalonia.ViewModels;
using System;
using System.Globalization;

namespace ProjectEpochLauncherAvalonia.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return new SolidColorBrush(boolValue ? Colors.Green : Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ServerStatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string statusText)
            {
                if (statusText.Contains("Online"))
                {
                    return Colors.Green;
                }
                else if (statusText.Contains("Offline"))
                {
                    return Colors.Red;
                }
                else if (statusText.Contains("Checking") || statusText.Contains("..."))
                {
                    return Colors.Orange;
                }
                else if (statusText.Contains("Error"))
                {
                    return Colors.Red;
                }
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToInstallStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Installed and Ready" : "Not Installed";
            }
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSetupStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Setup completed successfully" : "Setup not completed";
            }
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSupportStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Supported" : "Not Supported";
            }
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class SetupStepToContentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SetupStep step)
            {
                return step switch
                {
                    SetupStep.Welcome => CreateWelcomeContent(),
                    SetupStep.InstallPath => CreateInstallPathContent(),
                    SetupStep.Download => CreateDownloadContent(),
                    SetupStep.Complete => CreateCompleteContent(),
                    _ => new TextBlock { Text = "Unknown step" }
                };
            }
            return new TextBlock { Text = "Invalid step" };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Control CreateWelcomeContent()
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var welcomeText = new TextBlock
            {
                Text = "Project Epoch is a World of Warcraft private server that brings back the classic experience with modern improvements.",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            panel.Children.Add(welcomeText);
            return panel;
        }

        private static Control CreateInstallPathContent()
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            var titleText = new TextBlock
            {
                Text = "Choose Installation Location",
                FontSize = 20,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#F0E68C")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };

            var explanationText = new TextBlock
            {
                Text = "Select where you'd like to install Project Epoch.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#B0B0C0")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            // Path selection panel
            var pathPanel = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            var pathLabel = new TextBlock
            {
                Text = "Installation Path:",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };

            var pathContainer = new Grid();
            pathContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            pathContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var pathTextBox = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                Padding = new Avalonia.Thickness(10, 8),
                CornerRadius = new Avalonia.CornerRadius(4),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Bind to SelectedInstallPath
            pathTextBox.Bind(TextBox.TextProperty, new Avalonia.Data.Binding("SelectedInstallPath"));

            var browseButton = new Button
            {
                Content = Constants.BROWSE_BUTTON_TEXT,
                Width = 100,
                Height = 35,
                Margin = new Avalonia.Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#4CAF50")),
                Foreground = new SolidColorBrush(Colors.White),
                CornerRadius = new Avalonia.CornerRadius(4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Bind to BrowseInstallPathCommand
            browseButton.Bind(Button.CommandProperty, new Avalonia.Data.Binding("BrowseInstallPathCommand"));

            Grid.SetColumn(pathTextBox, 0);
            Grid.SetColumn(browseButton, 1);
            pathContainer.Children.Add(pathTextBox);
            pathContainer.Children.Add(browseButton);

            pathPanel.Children.Add(pathLabel);
            pathPanel.Children.Add(pathContainer);

            panel.Children.Add(titleText);
            panel.Children.Add(explanationText);
            panel.Children.Add(pathPanel);

            return panel;
        }

        private static Control CreateDownloadContent()
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = "Downloading Game Files",
                FontSize = 20,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#F0E68C")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            var statusText = new TextBlock
            {
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#B0B0C0")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };
            statusText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("DownloadStatus"));

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
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#B0B0C0"))
            };
            fileProgressLabel.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("FileProgressText"));

            var fileProgressBar = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Background = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                Foreground = new SolidColorBrush(Color.Parse("#4CAF50"))
            };
            fileProgressBar.Bind(ProgressBar.ValueProperty, new Avalonia.Data.Binding("FileProgress"));

            // Overall progress
            var overallProgressLabel = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#B0B0C0"))
            };
            overallProgressLabel.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("OverallProgressText"));

            var overallProgressBar = new ProgressBar
            {
                Height = 12,
                Minimum = 0,
                Maximum = 100,
                Background = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                Foreground = new SolidColorBrush(Color.Parse("#1E88E5"))
            };
            overallProgressBar.Bind(ProgressBar.ValueProperty, new Avalonia.Data.Binding("OverallProgress"));

            progressContainer.Children.Add(fileProgressLabel);
            progressContainer.Children.Add(fileProgressBar);
            progressContainer.Children.Add(overallProgressLabel);
            progressContainer.Children.Add(overallProgressBar);

            panel.Children.Add(titleText);
            panel.Children.Add(statusText);
            panel.Children.Add(progressContainer);

            return panel;
        }

        private static Control CreateCompleteContent()
        {
            var panel = new StackPanel
            {
                Spacing = 25,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = "Setup Complete!",
                FontSize = 24,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#4CAF50")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var messageText = new TextBlock
            {
                Text = "Project Epoch has been successfully installed and is ready to play!\n\nYou can now close this wizard and start your adventure.",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            panel.Children.Add(titleText);
            panel.Children.Add(messageText);

            return panel;
        }
    }
}