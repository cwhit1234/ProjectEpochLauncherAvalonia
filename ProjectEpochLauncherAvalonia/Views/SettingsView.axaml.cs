using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ProjectEpochLauncherAvalonia.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectEpochLauncherAvalonia.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Wire up the browse functionality when DataContext is set
            if (DataContext is SettingsViewModel viewModel)
            {
                viewModel.BrowseInstallPathCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(
                    () => ExecuteBrowseInstallPathAsync(viewModel));
            }
        }

        private async Task ExecuteBrowseInstallPathAsync(SettingsViewModel viewModel)
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
                    viewModel.UpdateInstallPath(selectedPath);
                }
            }
            catch (Exception ex)
            {
                // Handle error through the ViewModel
                viewModel.StatusMessage = $"Error selecting folder: {ex.Message}";
            }
        }
    }
}