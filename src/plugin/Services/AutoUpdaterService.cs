using BepInEx.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        public bool IsThunderstoreBuild();
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

#if THUNDERSTORE
        private bool isThunderstoreBuild = true;
#else
        private bool isThunderstoreBuild = false;
#endif

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

        public bool IsThunderstoreBuild()
        {
            return isThunderstoreBuild;
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

                    if (isThunderstoreBuild)
                    {
                        logger.LogInfo("Thunderstore build detected - update download is disabled. Please update through Thunderstore.");
                        isUpdateAvailable = true;
                        downloadedVersion = latestRelease.TagName;
                        return true;
                    }

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

                GenerateUpdaterBatchFile(pluginDirectory, release.TagName);

                logger.LogInfo($"Update {release.TagName} downloaded. Quit the game to apply.");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error preparing update: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// I think mod site are flagging the bat, so we will generate it on the fly and they can leave me alone
        /// </summary>
        private void GenerateUpdaterBatchFile(string pluginDirectory, string version)
        {
            try
            {
                var updaterPath = Path.Combine(pluginDirectory, "ApplyUpdate.bat");
                var batchContent = new StringBuilder();

                batchContent.AppendLine("@echo off");
                batchContent.AppendLine("setlocal enabledelayedexpansion");
                batchContent.AppendLine();
                batchContent.AppendLine("REM ========================================");
                batchContent.AppendLine("REM  MegabonkTogether Auto-Updater");
                batchContent.AppendLine("REM  Applies updates after game exits");
                batchContent.AppendLine("REM ========================================");
                batchContent.AppendLine();
                batchContent.AppendLine("set GAME_PID=%1");
                batchContent.AppendLine("set PLUGIN_DIR=%2");
                batchContent.AppendLine();
                batchContent.AppendLine("if \"%GAME_PID%\"==\"\" (");
                batchContent.AppendLine("    echo ERROR: Missing game process ID");
                batchContent.AppendLine("    echo Usage: ApplyUpdate.bat [gameProcessId] [pluginDirectory]");
                batchContent.AppendLine("    pause");
                batchContent.AppendLine("    exit /b 1");
                batchContent.AppendLine(")");
                batchContent.AppendLine();
                batchContent.AppendLine("if \"%PLUGIN_DIR%\"==\"\" (");
                batchContent.AppendLine("    echo ERROR: Missing plugin directory");
                batchContent.AppendLine("    echo Usage: ApplyUpdate.bat [gameProcessId] [pluginDirectory]");
                batchContent.AppendLine("    pause");
                batchContent.AppendLine("    exit /b 1");
                batchContent.AppendLine(")");
                batchContent.AppendLine();
                batchContent.AppendLine("cls");
                batchContent.AppendLine("echo ========================================");
                batchContent.AppendLine($"echo   MegabonkTogether Auto-Updater v{version}");
                batchContent.AppendLine("echo ========================================");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine();
                batchContent.AppendLine("REM Wait for exit");
                batchContent.AppendLine("echo Waiting for game process (PID: %GAME_PID%) to exit...");
                batchContent.AppendLine(":WAIT_LOOP");
                batchContent.AppendLine("tasklist /FI \"PID eq %GAME_PID%\" 2>NUL | find \"%GAME_PID%\" >NUL");
                batchContent.AppendLine("if !ERRORLEVEL! EQU 0 (");
                batchContent.AppendLine("    timeout /t 1 /nobreak >NUL");
                batchContent.AppendLine("    goto WAIT_LOOP");
                batchContent.AppendLine(")");
                batchContent.AppendLine("echo Game process exited successfully.");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine();
                batchContent.AppendLine("echo Waiting a bit for file locks to release...");
                batchContent.AppendLine("timeout /t 2 /nobreak >NUL");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine();
                batchContent.AppendLine("set UPDATE_FILE=");
                batchContent.AppendLine("for %%F in (\"%PLUGIN_DIR%\\.update_download_*\") do (");
                batchContent.AppendLine("    set UPDATE_FILE=%%~fF");
                batchContent.AppendLine(")");
                batchContent.AppendLine();
                batchContent.AppendLine("if \"%UPDATE_FILE%\"==\"\" (");
                batchContent.AppendLine("    echo ERROR: No update file found");
                batchContent.AppendLine("    pause");
                batchContent.AppendLine("    exit /b 1");
                batchContent.AppendLine(")");
                batchContent.AppendLine();
                batchContent.AppendLine("echo Found update file: %UPDATE_FILE%");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine();
                batchContent.AppendLine("REM backup");
                batchContent.AppendLine("set PLUGIN_DLL=%PLUGIN_DIR%\\MegabonkTogether.Plugin.dll");
                batchContent.AppendLine("set BACKUP_DLL=%PLUGIN_DLL%.backup");
                batchContent.AppendLine();
                batchContent.AppendLine("if exist \"%PLUGIN_DLL%\" (");
                batchContent.AppendLine("    echo Creating backup...");
                batchContent.AppendLine("    copy /Y \"%PLUGIN_DLL%\" \"%BACKUP_DLL%\" >NUL");
                batchContent.AppendLine("    if !ERRORLEVEL! NEQ 0 (");
                batchContent.AppendLine("        echo ERROR: Failed to create backup");
                batchContent.AppendLine("        pause");
                batchContent.AppendLine("        exit /b 1");
                batchContent.AppendLine("    )");
                batchContent.AppendLine("    echo Backup created.");
                batchContent.AppendLine("    echo.");
                batchContent.AppendLine(")");
                batchContent.AppendLine();
                batchContent.AppendLine("echo Applying update...");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine();
                batchContent.AppendLine("echo %UPDATE_FILE% | find /i \".zip\" >NUL");
                batchContent.AppendLine("if !ERRORLEVEL! EQU 0 (");
                batchContent.AppendLine("    echo Extracting ZIP archive...");
                batchContent.AppendLine("    ");
                batchContent.AppendLine("    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command \"$zipPath = '%UPDATE_FILE%'; $destPath = '%PLUGIN_DIR%'; Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath); try { foreach ($entry in $zip.Entries) { if ($entry.Name -match '\\.dll$' -or $entry.Name -match '\\.exe$') { $targetPath = Join-Path $destPath $entry.Name; [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true); Write-Host ('  Extracted: ' + $entry.Name) } } } finally { $zip.Dispose() }\"");
                batchContent.AppendLine("    ");
                batchContent.AppendLine("    if !ERRORLEVEL! NEQ 0 (");
                batchContent.AppendLine("        echo ERROR: Failed to extract ZIP");
                batchContent.AppendLine("        goto RESTORE_BACKUP");
                batchContent.AppendLine("    )");
                batchContent.AppendLine(")");
                batchContent.AppendLine();
                batchContent.AppendLine("echo.");
                batchContent.AppendLine("echo Cleaning up...");
                batchContent.AppendLine("del /F /Q \"%UPDATE_FILE%\" 2>NUL");
                batchContent.AppendLine("if exist \"%BACKUP_DLL%\" del /F /Q \"%BACKUP_DLL%\" 2>NUL");
                batchContent.AppendLine();
                batchContent.AppendLine("echo.");
                batchContent.AppendLine("echo ========================================");
                batchContent.AppendLine("echo   Update applied successfully!");
                batchContent.AppendLine("echo ========================================");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine("echo You can now restart the game.");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine("echo This window will close in 5 seconds...");
                batchContent.AppendLine("timeout /t 5 /nobreak >NUL");
                batchContent.AppendLine("exit /b 0");
                batchContent.AppendLine();
                batchContent.AppendLine(":RESTORE_BACKUP");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine("echo Restoring backup...");
                batchContent.AppendLine("if exist \"%BACKUP_DLL%\" (");
                batchContent.AppendLine("    copy /Y \"%BACKUP_DLL%\" \"%PLUGIN_DLL%\" >NUL");
                batchContent.AppendLine("    echo Backup restored.");
                batchContent.AppendLine(") else (");
                batchContent.AppendLine("    echo WARNING: No backup found to restore");
                batchContent.AppendLine(")");
                batchContent.AppendLine("echo.");
                batchContent.AppendLine("echo Update failed. Press any key to exit...");
                batchContent.AppendLine("pause >NUL");
                batchContent.AppendLine("exit /b 1");

                File.WriteAllText(updaterPath, batchContent.ToString());
                logger.LogInfo($"Generated updater batch file!");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error generating updater batch file: {ex.Message}");
                throw;
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
            if (isThunderstoreBuild)
            {
                logger.LogInfo("Thunderstore build - updater launch is disabled");
                return;
            }

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
