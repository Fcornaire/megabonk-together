using BepInEx.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MegabonkTogether.Common
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public interface IAutoUpdaterService
    {
        public void Initialize();
        Task<bool> CheckAndUpdate();
        bool IsAnUpdateAvailable();
        string GetDownloadedVersion();
        public void LaunchUpdaterOnExit(string pluginDirectory);
        public string GetLatestVersion();

    }

    public class AutoUpdaterService : IAutoUpdaterService
    {
        private const string UPDATE_FILE_PREFIX = ".update_download_";

        private const string GITHUB_OWNER = "Fcornaire";
        private const string GITHUB_REPO = "megabonk-together";
        private string currentVersion;
        private string pluginPath;
        private ManualLogSource logger;

        private bool isUpdateAvailable = false;
        private string downloadedVersion = "";
        private string latestVersion = "";

        public AutoUpdaterService(ManualLogSource logger)
        {
            this.logger = logger;
        }

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        static AutoUpdaterService()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MegabonkTogether-AutoUpdater");
        }


        public void Initialize()
        {
            this.currentVersion = MyPluginInfo.PLUGIN_VERSION;
            this.pluginPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        }

        public bool IsAnUpdateAvailable()
        {
            return isUpdateAvailable;
        }

        public string GetDownloadedVersion()
        {
            return downloadedVersion;
        }

        public string GetLatestVersion()
        {
            if (string.IsNullOrEmpty(latestVersion))
            {
                var task = GetLatestRelease();
                task.Wait();
                var latestRelease = task.Result;
                if (latestRelease != null)
                {
                    latestVersion = latestRelease.TagName;
                }
            }

            return latestVersion;
        }

        public async Task<bool> CheckAndUpdate()
        {
            if (isUpdateAvailable)
            {
                return true;
            }

            latestVersion = "";

            try
            {
                logger.LogInfo("Checking for updates...");

                var latestRelease = await GetLatestRelease();
                if (latestRelease == null)
                {
                    logger.LogInfo("No releases found");
                    return false;
                }

                latestVersion = latestRelease.TagName;

                if (IsNewerVersion(latestRelease.TagName, currentVersion))
                {
                    logger.LogInfo($"New version available: {latestRelease.TagName} (current: {currentVersion})");
                    isUpdateAvailable = await PrepareUpdate(latestRelease);
                    if (isUpdateAvailable)
                    {
                        downloadedVersion = latestRelease.TagName;
                    }
                    return isUpdateAvailable;
                }
                else
                {
                    logger.LogInfo($"Already on latest version: {currentVersion}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for updates: {ex.Message}");
                return false;
            }
        }

        private async Task<GitHubRelease> GetLatestRelease()
        {
            var url = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Failed to fetch releases: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubRelease>(json);
        }

        private bool IsNewerVersion(string remoteVersion, string localVersion)
        {
            remoteVersion = remoteVersion.TrimStart('v');
            localVersion = localVersion.TrimStart('v');

            try
            {
                var remote = new Version(remoteVersion);
                var local = new Version(localVersion);
                return remote > local;
            }
            catch
            {
                return string.Compare(remoteVersion, localVersion, StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        private async Task<bool> PrepareUpdate(GitHubRelease release)
        {
            try
            {
                var asset = release.Assets.FirstOrDefault(a =>
                    Regex.IsMatch(a.Name ?? string.Empty, @"^Megabonk\-Together\-\d+\.\d+\.\d+\.zip$", RegexOptions.IgnoreCase)); //this should pick Megabonk-Together-X.Y.Z.zip

                if (asset == null)
                {
                    logger.LogError("No suitable asset found in release");
                    return false;
                }

                logger.LogInfo($"Downloading update: {asset.Name}");

                var downloadUrl = !string.IsNullOrEmpty(asset.BrowserDownloadUrl) ? asset.BrowserDownloadUrl : asset.Url;

                var pluginDirectory = Path.GetDirectoryName(pluginPath);
                var downloadPath = Path.Combine(pluginDirectory, UPDATE_FILE_PREFIX + asset.Name);

                CleanupOldUpdateFiles(pluginDirectory);

                await DownloadFile(downloadUrl, downloadPath);

                logger.LogInfo($"Update {release.TagName} downloaded. Quit the game to apply.");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error preparing update: {ex.Message}");
                return false;
            }
        }

        private async Task DownloadFile(string url, string destinationPath)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/octet-stream");
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Download failed: {response.StatusCode} - {response.ReasonPhrase}");
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError($"Error details: {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);
        }

        private static void CleanupOldUpdateFiles(string pluginDirectory)
        {
            try
            {
                var updateFiles = Directory.GetFiles(pluginDirectory, UPDATE_FILE_PREFIX + "*");
                foreach (var file in updateFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }
        }

        public void LaunchUpdaterOnExit(string pluginDirectory)
        {
            try
            {
                logger.LogInfo("=== Updater ===");

                var updateFiles = Directory.GetFiles(pluginDirectory, UPDATE_FILE_PREFIX + "*");
                if (updateFiles.Length == 0)
                {
                    logger.LogInfo("No update files found with prefix: " + UPDATE_FILE_PREFIX);
                    return;
                }

                logger.LogInfo($"Found {updateFiles.Length} update file(s): {string.Join(", ", updateFiles.Select(Path.GetFileName))}");

                var updaterPath = Path.Combine(pluginDirectory, "ApplyUpdate.bat");
                logger.LogInfo($"Looking for updater at: {updaterPath}");

                if (!File.Exists(updaterPath))
                {
                    logger.LogError($"Updater script not found at: {updaterPath}");
                    return;
                }

                var currentProcess = Process.GetCurrentProcess();
                var gameProcessId = currentProcess.Id;

                logger.LogInfo($"Current game process ID: {gameProcessId}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"{gameProcessId} \"{pluginDirectory}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = pluginDirectory
                };

                logger.LogInfo($"Starting updater: {updaterPath}");
                var updaterProcess = Process.Start(startInfo);

                if (updaterProcess != null)
                {
                    logger.LogInfo($"Updater process STARTED with PID: {updaterProcess.Id}");
                }
                else
                {
                    logger.LogError("Failed to start updater process");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in LaunchUpdaterOnExit: {ex}");
            }
        }
    }
}
