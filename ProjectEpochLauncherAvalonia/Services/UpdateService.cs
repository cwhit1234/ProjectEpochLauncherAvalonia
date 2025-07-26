using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEpochLauncherAvalonia.Services
{
    public class UpdateService
    {
        private static readonly Lazy<HttpClient> _lazyHttpClient = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10); // Set timeout during creation
            client.DefaultRequestHeaders.Add("User-Agent", $"{Constants.APPLICATION_NAME}/1.0");
            return client;
        });

        private static HttpClient HttpClient => _lazyHttpClient.Value;
        private readonly ConfigurationManager _configManager;

        public UpdateService(ConfigurationManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                LogDebug("Starting update check...");

                var manifest = await FetchManifestAsync(cancellationToken);
                if (manifest == null)
                {
                    return new UpdateCheckResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to fetch manifest from server"
                    };
                }

                LogDebug($"Fetched manifest version: {manifest.Version}, UID: {manifest.Uid}");

                var installPath = _configManager.InstallPath;
                if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
                {
                    LogDebug("Install directory does not exist, treating as fresh install");
                    return new UpdateCheckResult
                    {
                        Success = true,
                        UpdatesAvailable = true,
                        FilesToUpdate = manifest.Files,
                        TotalSize = CalculateTotalSize(manifest.Files),
                        Version = manifest.Version
                    };
                }

                var filesToUpdate = new List<GameFile>();
                long totalUpdateSize = 0;

                foreach (var file in manifest.Files)
                {
                    var localFilePath = Path.Combine(installPath, file.Path);

                    if (!File.Exists(localFilePath))
                    {
                        LogDebug($"File missing: {file.Path}");
                        filesToUpdate.Add(file);
                        totalUpdateSize += file.Size;
                        continue;
                    }

                    if (!await ValidateFileHashAsync(localFilePath, file.Hash, cancellationToken))
                    {
                        LogDebug($"File hash mismatch: {file.Path}");
                        filesToUpdate.Add(file);
                        totalUpdateSize += file.Size;
                    }
                    else
                    {
                        LogDebug($"File up to date: {file.Path}");
                    }
                }

                return new UpdateCheckResult
                {
                    Success = true,
                    UpdatesAvailable = filesToUpdate.Count > 0,
                    FilesToUpdate = filesToUpdate,
                    TotalSize = totalUpdateSize,
                    Version = manifest.Version
                };
            }
            catch (OperationCanceledException)
            {
                LogDebug("Update check was cancelled");
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = "Update check was cancelled"
                };
            }
            catch (Exception ex)
            {
                LogError($"Error during update check: {ex.Message}");
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = $"Update check failed: {ex.Message}"
                };
            }
        }

        private async Task<GameManifest?> FetchManifestAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogDebug($"Fetching manifest from: {Constants.MANIFEST_URL}");

                // Create a timeout token for just the manifest request (30 seconds)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await HttpClient.GetAsync(Constants.MANIFEST_URL, combinedCts.Token);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(combinedCts.Token);
                LogDebug($"Received manifest JSON ({jsonContent.Length} characters)");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<GameManifest>(jsonContent, options);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogDebug("Manifest fetch was cancelled by user");
                }
                else
                {
                    LogError("Manifest fetch timed out after 30 seconds");
                }
                return null;
            }
            catch (HttpRequestException ex)
            {
                LogError($"HTTP error fetching manifest: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                LogError($"JSON parsing error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error fetching manifest: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> ValidateFileHashAsync(string filePath, string expectedHash, CancellationToken cancellationToken)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);

                var hash = await Task.Run(() => md5.ComputeHash(stream), cancellationToken);
                var hashString = Convert.ToHexString(hash).ToLowerInvariant();

                return hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogError($"Error validating file hash for {filePath}: {ex.Message}");
                return false;
            }
        }

        private static long CalculateTotalSize(List<GameFile> files)
        {
            long total = 0;
            foreach (var file in files)
            {
                total += file.Size;
            }
            return total;
        }

        public async Task<DownloadResult> DownloadFilesAsync(
            List<GameFile> filesToDownload,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                LogDebug($"Starting download of {filesToDownload.Count} files");

                var installPath = _configManager.InstallPath;
                if (string.IsNullOrEmpty(installPath))
                {
                    return new DownloadResult
                    {
                        Success = false,
                        ErrorMessage = "Install path is not configured"
                    };
                }

                // Ensure install directory exists
                Directory.CreateDirectory(installPath);

                long totalBytes = CalculateTotalSize(filesToDownload);
                long downloadedBytes = 0;
                int filesDownloaded = 0;

                for (int i = 0; i < filesToDownload.Count; i++)
                {
                    var file = filesToDownload[i];
                    var localFilePath = Path.Combine(installPath, file.Path);

                    // Report progress
                    progress?.Report(new DownloadProgress
                    {
                        FileName = Path.GetFileName(file.Path),
                        FileIndex = i + 1,
                        TotalFiles = filesToDownload.Count,
                        BytesDownloaded = downloadedBytes,
                        TotalBytes = totalBytes,
                        OverallProgress = (double)downloadedBytes / totalBytes * 100,
                        FileProgress = 0,
                        Status = $"Downloading {Path.GetFileName(file.Path)}..."
                    });

                    // Download the file
                    var downloadSuccess = await DownloadSingleFileAsync(
                        file,
                        localFilePath,
                        new Progress<double>(fileProgress =>
                        {
                            progress?.Report(new DownloadProgress
                            {
                                FileName = Path.GetFileName(file.Path),
                                FileIndex = i + 1,
                                TotalFiles = filesToDownload.Count,
                                BytesDownloaded = downloadedBytes + (long)(file.Size * fileProgress / 100),
                                TotalBytes = totalBytes,
                                OverallProgress = (double)(downloadedBytes + (long)(file.Size * fileProgress / 100)) / totalBytes * 100,
                                FileProgress = fileProgress,
                                Status = $"Downloading {Path.GetFileName(file.Path)}... {fileProgress:F1}%"
                            });
                        }),
                        cancellationToken);

                    if (!downloadSuccess)
                    {
                        return new DownloadResult
                        {
                            Success = false,
                            ErrorMessage = $"Failed to download {file.Path}",
                            FilesDownloaded = filesDownloaded,
                            TotalBytesDownloaded = downloadedBytes
                        };
                    }

                    downloadedBytes += file.Size;
                    filesDownloaded++;

                    LogDebug($"Successfully downloaded: {file.Path} ({file.Size} bytes)");
                }

                // Final progress report
                progress?.Report(new DownloadProgress
                {
                    FileName = "Complete",
                    FileIndex = filesToDownload.Count,
                    TotalFiles = filesToDownload.Count,
                    BytesDownloaded = downloadedBytes,
                    TotalBytes = totalBytes,
                    OverallProgress = 100,
                    FileProgress = 100,
                    Status = $"{Constants.DOWNLOAD_COMPLETE_BUTTON_TEXT}!"
                });

                LogDebug($"Download completed successfully. {filesDownloaded} files, {downloadedBytes} bytes");

                return new DownloadResult
                {
                    Success = true,
                    FilesDownloaded = filesDownloaded,
                    TotalBytesDownloaded = downloadedBytes
                };
            }
            catch (OperationCanceledException)
            {
                LogDebug("Download was cancelled");
                return new DownloadResult
                {
                    Success = false,
                    ErrorMessage = "Download was cancelled"
                };
            }
            catch (Exception ex)
            {
                LogError($"Error during download: {ex.Message}");
                return new DownloadResult
                {
                    Success = false,
                    ErrorMessage = $"Download failed: {ex.Message}"
                };
            }
        }

        private async Task<bool> DownloadSingleFileAsync(
            GameFile file,
            string localPath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Try different CDN URLs in order of preference
                var urlsToTry = new[]
                {
                    file.Urls.GetValueOrDefault("cloudflare"),
                    file.Urls.GetValueOrDefault("digitalocean"),
                    file.Urls.GetValueOrDefault("none")
                }.Where(url => !string.IsNullOrEmpty(url)).ToArray();

                foreach (var url in urlsToTry)
                {
                    try
                    {
                        LogDebug($"Attempting download from: {url}");

                        // Create timeout for individual file download (5 minutes per file)
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, combinedCts.Token);
                        response.EnsureSuccessStatusCode();

                        var contentLength = response.Content.Headers.ContentLength ?? file.Size;

                        // Create temporary file
                        var tempPath = localPath + ".tmp";

                        using var contentStream = await response.Content.ReadAsStreamAsync(combinedCts.Token);
                        using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, combinedCts.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, combinedCts.Token);
                            totalBytesRead += bytesRead;

                            if (contentLength > 0)
                            {
                                var progressPercentage = (double)totalBytesRead / contentLength * 100;
                                progress?.Report(progressPercentage);
                            }
                        }

                        fileStream.Close();

                        // Verify the downloaded file hash
                        if (await ValidateFileHashAsync(tempPath, file.Hash, cancellationToken))
                        {
                            // Move temp file to final location
                            if (File.Exists(localPath))
                            {
                                File.Delete(localPath);
                            }
                            File.Move(tempPath, localPath);

                            LogDebug($"File downloaded and verified successfully: {file.Path}");
                            return true;
                        }
                        else
                        {
                            LogError($"Hash verification failed for downloaded file: {file.Path}");
                            if (File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                            continue; // Try next URL
                        }
                    }
                    catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            LogDebug($"Download cancelled by user: {file.Path}");
                        }
                        else
                        {
                            LogError($"Download timed out: {file.Path} from {url}");
                        }
                        throw; // Re-throw cancellation
                    }
                    catch (HttpRequestException ex)
                    {
                        LogError($"HTTP error downloading from {url}: {ex.Message}");
                        continue; // Try next URL
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error downloading from {url}: {ex.Message}");
                        continue; // Try next URL
                    }
                }

                LogError($"Failed to download file from all available URLs: {file.Path}");
                return false;
            }
            catch (OperationCanceledException)
            {
                LogDebug($"Download cancelled: {file.Path}");
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                LogError($"Error downloading file {file.Path}: {ex.Message}");
                return false;
            }
        }

        private void LogDebug(string message)
        {
            Debug.WriteLine($"[UPDATE-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private void LogError(string message)
        {
            Debug.WriteLine($"[UPDATE-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }

    // Data Models
    public class GameManifest
    {
        public string Version { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public List<GameFile> Files { get; set; } = new();
        public string CheckedAt { get; set; } = string.Empty;
    }

    public class GameFile
    {
        public string Path { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool Custom { get; set; }
        public Dictionary<string, string> Urls { get; set; } = new();
    }

    public class UpdateCheckResult
    {
        public bool Success { get; set; }
        public bool UpdatesAvailable { get; set; }
        public List<GameFile> FilesToUpdate { get; set; } = new();
        public long TotalSize { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class DownloadProgress
    {
        public string FileName { get; set; } = string.Empty;
        public int FileIndex { get; set; }
        public int TotalFiles { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double OverallProgress { get; set; }
        public double FileProgress { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DownloadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int FilesDownloaded { get; set; }
        public long TotalBytesDownloaded { get; set; }
    }
}