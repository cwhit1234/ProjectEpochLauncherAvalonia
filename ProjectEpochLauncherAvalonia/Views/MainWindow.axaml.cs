using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ProjectEpochLauncherAvalonia.ViewModels;
using System;

namespace ProjectEpochLauncherAvalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext will be set by the ApplicationStartupService
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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