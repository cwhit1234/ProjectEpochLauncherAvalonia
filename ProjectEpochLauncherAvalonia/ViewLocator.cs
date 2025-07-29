using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ProjectEpochLauncherAvalonia.ViewModels;
using ProjectEpochLauncherAvalonia.Views;

namespace ProjectEpochLauncherAvalonia
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            // Handle specific ViewModel to View mappings
            return param switch
            {
                HomeViewModel => new HomeView { DataContext = param },
                SettingsViewModel => new SettingsView { DataContext = param },
                _ => BuildByConvention(param)
            };
        }

        private Control? BuildByConvention(object param)
        {
            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null)
            {
                var control = (Control)Activator.CreateInstance(type)!;
                control.DataContext = param;
                return control;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}