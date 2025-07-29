using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ProjectEpochLauncherAvalonia.ViewModels;

namespace ProjectEpochLauncherAvalonia.Views
{
    public partial class SetupWizardView : Window
    {
        public SetupWizardView()
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
            if (DataContext is SetupWizardViewModel viewModel)
            {
                // Replace the browse command with one that can access the TopLevel
                viewModel.BrowseInstallPathCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                    async () => await ExecuteBrowseInstallPathAsync(viewModel));
            }
        }

        private async Task ExecuteBrowseInstallPathAsync(SetupWizardViewModel viewModel)
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
                viewModel.StatusMessage = $"Error selecting folder: {ex.Message}";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Dispose the ViewModel when window closes
            if (DataContext is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }

            base.OnClosed(e);
        }
    }
}