using System.IO;

namespace ProjectEpochLauncherAvalonia
{
    public static class Constants
    {
        // Button Text Constants
        public const string PLAY_BUTTON_TEXT = "PLAY";
        public const string UP_TO_DATE_BUTTON_TEXT = "Up to Date";
        public const string DOWNLOAD_ERROR_BUTTON_TEXT = "Download Error";
        public const string DOWNLOAD_CANCELLED_BUTTON_TEXT = "Download Cancelled";
        public const string DOWNLOAD_FAILED_BUTTON_TEXT = "Download Failed";
        public const string LAUNCHING_BUTTON_TEXT = "Launching...";
        public const string CHECKING_BUTTON_TEXT = "Checking...";
        public const string DOWNLOADING_BUTTON_TEXT = "Downloading...";
        public const string CHECK_FAILED_BUTTON_TEXT = "Check Failed";
        public const string DOWNLOAD_COMPLETE_BUTTON_TEXT = "Download Complete";

        // Navigation Text
        public const string GET_STARTED_BUTTON_TEXT = "Get Started";
        public const string INSTALL_BUTTON_TEXT = "Install";
        public const string FINISH_BUTTON_TEXT = "Finish";
        public const string BROWSE_BUTTON_TEXT = "Browse...";

        // File Names
        public const string PROJECT_EPOCH_EXECUTABLE = "Project-Epoch.exe";
        public const string CONFIG_FILE_NAME = "launcher-config.json";

        // Application Names
        public const string APPLICATION_NAME = "ProjectEpochLauncherAvalonia";
        public const string PROJECT_EPOCH_DISPLAY_NAME = "Project Epoch";
        public const string PROJECT_EPOCH_DISPLAY_NAME_ALTERNATIVE = "ProjectEpoch";

        //Text Block Constants
        public const string STATUS_MESSAGE_TEXT_BLOCK = "StatusMessage";

        // Required WoW 3.3.5a Client Files
        public static readonly string[] REQUIRED_WOW_CLIENT_FILES = {
            "WoW.exe",
            "launcher.exe",
            "dbghelp.dll",
            "unicows.dll",
            Path.Combine("Data", "common.MPQ"),
            Path.Combine("Data", "common-2.MPQ"),
            Path.Combine("Data", "expansion.MPQ"),
            Path.Combine("Data", "lichking.MPQ"),
            Path.Combine("Data", "patch.MPQ"),
            Path.Combine("Data", "patch-2.MPQ"),
            Path.Combine("Data", "patch-3.MPQ"),
            Path.Combine("Data", "enUS", "locale-enUS.MPQ"),
            Path.Combine("Data", "enUS", "patch-enUS.MPQ"),
            Path.Combine("Data", "enUS", "speech-enUS.MPQ"),
        };

        // Required Project Epoch Files
        public static readonly string[] REQUIRED_PROJECT_EPOCH_FILES = {
            "Project-Epoch.exe",
            "ClientExtensions.dll",
            "credits.txt",
            Path.Combine("Data", "patch-A.MPQ"),
            Path.Combine("Data", "patch-B.MPQ"),
            Path.Combine("Data", "patch-Y.MPQ"),
            Path.Combine("Data", "patch-Z.MPQ"),
            Path.Combine("Data", "enUS", "realmlist.wtf")
        };
    }
}
