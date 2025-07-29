using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectEpochLauncherAvalonia.ViewModels
{
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}