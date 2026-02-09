using BepInEx.Logging;
using MegabonkTogether.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

namespace MegabonkTogether.Services
{
    public class VersionChanges
    {
        public string Version { get; set; }
        public ICollection<string> Changes { get; set; } = [];
    }

    public interface IChangelogService
    {
        Task<ICollection<VersionChanges>> LoadChangelogAsync();
        ICollection<VersionChanges> GetChangesBetweenVersions(ICollection<VersionChanges> changelog, string fromVersion, string toVersion);
        bool ShouldShowChangelog();
        void MarkChangelogAsShown();
        void SetShowChangelogOnNextLaunch(string currentVersion);
    }

    public class ChangelogService(IAutoUpdaterService autoUpdaterService, ManualLogSource logger) : IChangelogService
    {
        public const string CHANGELOG_FILENAME = "CHANGELOG.toml";

        public async Task<ICollection<VersionChanges>> LoadChangelogAsync()
        {
            try
            {
                var pluginPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var pluginDirectory = Path.GetDirectoryName(pluginPath);

                await autoUpdaterService.EnsureChangelogExists(MyPluginInfo.PLUGIN_VERSION, pluginDirectory);

                var changelogPath = Path.Combine(pluginDirectory, CHANGELOG_FILENAME);

                if (!File.Exists(changelogPath))
                {
                    logger.LogError($"Changelog file not found");
                    return [];
                }

                logger.LogInfo($"Loading changelog...");

                var tomlContent = await File.ReadAllTextAsync(changelogPath);
                return ParseChangelog(tomlContent);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading changelog: {ex.Message}");
                return [];
            }
        }

        private ICollection<VersionChanges> ParseChangelog(string content)
        {
            var changelog = new List<VersionChanges>();

            try
            {
                var model = Toml.ToModel(content);

                if (!model.ContainsKey("version"))
                {
                    logger.LogWarning("No 'version' table found in changelog");
                    return changelog;
                }

                if (model["version"] is not TomlTable versionTable)
                {
                    logger.LogWarning("'version' is not a table");
                    return changelog;
                }

                foreach (var kvp in versionTable)
                {
                    var version = kvp.Key;

                    if (kvp.Value is not TomlTable versionData)
                    {
                        logger.LogWarning($"Version {version} data is not a table");
                        continue;
                    }

                    if (!versionData.ContainsKey("changes"))
                    {
                        logger.LogWarning($"Version {version} has no 'changes' array");
                        continue;
                    }

                    if (versionData["changes"] is not TomlArray changesArray)
                    {
                        logger.LogWarning($"Version {version} 'changes' is not an array");
                        continue;
                    }

                    var changes = new List<string>();
                    foreach (var item in changesArray)
                    {
                        if (item is string changeText && !string.IsNullOrWhiteSpace(changeText))
                        {
                            changes.Add(changeText.Trim());
                        }
                    }

                    if (changes.Count > 0)
                    {
                        changelog.Add(new VersionChanges
                        {
                            Version = version,
                            Changes = changes
                        });
                    }
                }

                changelog = [.. changelog.OrderByDescending(v =>
                {
                    try
                    {
                        return new Version(v.Version);
                    }
                    catch
                    {
                        return new Version(0, 0, 0);
                    }
                })];
            }
            catch (Exception ex)
            {
                logger.LogError($"Error parsing changelog TOML: {ex.Message}");
            }

            return changelog;
        }

        public ICollection<VersionChanges> GetChangesBetweenVersions(ICollection<VersionChanges> changelog, string fromVersion, string toVersion)
        {
            if (changelog == null || changelog.Count == 0)
                return [];

            fromVersion = fromVersion?.TrimStart('v') ?? "";
            toVersion = toVersion?.TrimStart('v') ?? "";

            Version from = null;
            Version to = null;

            try
            {
                if (!string.IsNullOrEmpty(fromVersion)) from = new Version(fromVersion);
                if (!string.IsNullOrEmpty(toVersion)) to = new Version(toVersion);
            }
            catch
            {
                logger.LogWarning($"Could not parse versions: from={fromVersion}, to={toVersion}");
                return [.. changelog.Take(1)];
            }

            if (from == null || to == null) return [.. changelog.Take(1)];

            return [.. changelog.Where(v =>
            {
                try
                {
                    var ver = new Version(v.Version);
                    return ver > from && ver <= to;
                }
                catch
                {
                    return false;
                }
            })];
        }

        public bool ShouldShowChangelog()
        {
            return Configuration.ModConfig.ShowChangelog.Value;
        }

        public void MarkChangelogAsShown()
        {
            Configuration.ModConfig.ShowChangelog.Value = false;
            Configuration.ModConfig.Save();
            logger.LogInfo("Changelog marked as shown");
        }

        public void SetShowChangelogOnNextLaunch(string currentVersion)
        {
            Configuration.ModConfig.PreviousVersion.Value = currentVersion;
            Configuration.ModConfig.ShowChangelog.Value = true;
            Configuration.ModConfig.Save();
            logger.LogInfo($"Changelog will be shown on next launch. Previous version set to: {currentVersion}");
        }
    }
}
