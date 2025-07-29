using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ProjectEpochLauncherAvalonia.Services
{
    public class InstallationValidationService
    {
        public InstallationValidationResult ValidateInstallation(string? installPath)
        {
            var result = new InstallationValidationResult();

            try
            {
                if (string.IsNullOrEmpty(installPath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "No installation path configured.";
                    result.ValidationIssues.Add("Installation path is not set");
                    return result;
                }

                if (!Directory.Exists(installPath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Installation directory does not exist.";
                    result.ValidationIssues.Add($"Directory not found: {installPath}");
                    return result;
                }

                LogDebug($"Validating installation at: {installPath}");

                // Check for WoW 3.3.5a client files
                var wowValidation = ValidateWowClientFiles(installPath);
                result.HasWowClient = wowValidation.IsValid;
                result.MissingWowFiles.AddRange(wowValidation.MissingFiles);

                // Check for Project Epoch files
                var epochValidation = ValidateProjectEpochFiles(installPath);
                result.HasProjectEpochFiles = epochValidation.IsValid;
                result.MissingProjectEpochFiles.AddRange(epochValidation.MissingFiles);

                // Determine overall validity and create appropriate message
                if (result.HasWowClient && result.HasProjectEpochFiles)
                {
                    result.IsValid = true;
                    result.ErrorMessage = "Installation is valid and ready to play!";
                    result.InstallationType = InstallationType.Complete;
                    LogDebug("Installation validation passed - complete installation");
                }
                else if (result.HasWowClient && !result.HasProjectEpochFiles)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "WoW 3.3.5a client found, but Project Epoch files are missing. Please run setup to download Project Epoch files.";
                    result.InstallationType = InstallationType.WowClientOnly;
                    result.ValidationIssues.Add("Project Epoch files missing");
                    LogDebug("Installation validation failed - WoW client present but Project Epoch files missing");
                }
                else if (!result.HasWowClient && result.HasProjectEpochFiles)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Project Epoch files found, but WoW 3.3.5a client is missing. Please install the WoW 3.3.5a client first.";
                    result.InstallationType = InstallationType.ProjectEpochOnly;
                    result.ValidationIssues.Add("WoW 3.3.5a client missing");
                    LogDebug("Installation validation failed - Project Epoch files present but WoW client missing");
                }
                else
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Neither WoW 3.3.5a client nor Project Epoch files found. Please install WoW 3.3.5a client and run Project Epoch setup.";
                    result.InstallationType = InstallationType.Empty;
                    result.ValidationIssues.Add("Both WoW client and Project Epoch files missing");
                    LogDebug("Installation validation failed - no valid files found");
                }

                // Add detailed missing files to validation issues
                if (result.MissingWowFiles.Count > 0)
                {
                    result.ValidationIssues.Add($"Missing WoW client files: {string.Join(", ", result.MissingWowFiles.Take(5))}{(result.MissingWowFiles.Count > 5 ? "..." : "")}");
                }

                if (result.MissingProjectEpochFiles.Count > 0)
                {
                    result.ValidationIssues.Add($"Missing Project Epoch files: {string.Join(", ", result.MissingProjectEpochFiles)}");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error during installation validation: {ex.Message}");
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                result.ValidationIssues.Add($"Validation exception: {ex.Message}");
                return result;
            }
        }

        private FileValidationResult ValidateWowClientFiles(string installPath)
        {
            var result = new FileValidationResult();
            var missingFiles = new List<string>();

            foreach (var requiredFile in Constants.REQUIRED_WOW_CLIENT_FILES)
            {
                var filePath = Path.Combine(installPath, requiredFile);
                if (!File.Exists(filePath))
                {
                    missingFiles.Add(requiredFile);
                }
            }

            result.IsValid = missingFiles.Count == 0;
            result.MissingFiles = missingFiles;

            LogDebug($"WoW client validation: {(result.IsValid ? "PASSED" : "FAILED")} - Missing {missingFiles.Count} files");

            return result;
        }

        private FileValidationResult ValidateProjectEpochFiles(string installPath)
        {
            var result = new FileValidationResult();
            var missingFiles = new List<string>();

            foreach (var requiredFile in Constants.REQUIRED_PROJECT_EPOCH_FILES)
            {
                var filePath = Path.Combine(installPath, requiredFile);
                if (!File.Exists(filePath))
                {
                    missingFiles.Add(requiredFile);
                }
            }

            result.IsValid = missingFiles.Count == 0;
            result.MissingFiles = missingFiles;

            LogDebug($"Project Epoch validation: {(result.IsValid ? "PASSED" : "FAILED")} - Missing {missingFiles.Count} files");

            return result;
        }

        public bool IsPlayable(string? installPath)
        {
            var validation = ValidateInstallation(installPath);
            return validation.IsValid && validation.InstallationType == InstallationType.Complete;
        }

        public string GetInstallationStatusMessage(string? installPath)
        {
            var validation = ValidateInstallation(installPath);
            return validation.ErrorMessage;
        }

        public List<string> GetDetailedValidationIssues(string? installPath)
        {
            var validation = ValidateInstallation(installPath);
            return validation.ValidationIssues;
        }

        private static void LogDebug(string message)
        {
            Debug.WriteLine($"[VALIDATION-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void LogError(string message)
        {
            Debug.WriteLine($"[VALIDATION-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }

    public class InstallationValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasWowClient { get; set; }
        public bool HasProjectEpochFiles { get; set; }
        public InstallationType InstallationType { get; set; }
        public List<string> MissingWowFiles { get; set; } = new();
        public List<string> MissingProjectEpochFiles { get; set; } = new();
        public List<string> ValidationIssues { get; set; } = new();
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> MissingFiles { get; set; } = new();
    }

    public enum InstallationType
    {
        Empty,
        WowClientOnly,
        ProjectEpochOnly,
        Complete
    }
}