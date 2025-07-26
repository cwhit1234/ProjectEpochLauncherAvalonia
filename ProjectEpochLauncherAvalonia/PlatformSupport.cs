using System.Runtime.InteropServices;

namespace ProjectEpochLauncherAvalonia
{
    public static class PlatformSupport
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Determines if the current platform supports launching the game.
        /// Currently, only Windows is supported for game launching.
        /// </summary>
        public static bool SupportsGameLaunching => IsWindows;

        /// <summary>
        /// Gets a user-friendly message explaining platform limitations.
        /// </summary>
        public static string GetPlatformLimitationMessage()
        {
            if (IsMacOS)
            {
                return "Game launching is not supported on macOS. You can use this launcher to download and update game files.";
            }
            else if (IsLinux)
            {
                return "Game launching is not supported on Linux. You can use this launcher to download and update game files.";
            }

            return string.Empty; // Windows - no limitation message needed
        }

        /// <summary>
        /// Gets the platform name for display purposes.
        /// </summary>
        public static string GetPlatformName()
        {
            if (IsWindows) return "Windows";
            if (IsMacOS) return "macOS";
            if (IsLinux) return "Linux";
            return "Unknown";
        }
    }
}