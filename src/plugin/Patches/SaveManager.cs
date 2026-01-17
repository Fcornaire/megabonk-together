using HarmonyLib;
using MegabonkTogether.Common;
using MegabonkTogether.Configuration;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace MegabonkTogether.Patches
{
    /// <summary>
    /// Prevent saving during netplay sessions
    /// </summary>
    [HarmonyPatch(typeof(SaveManager))]
    internal static class SaveManagerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetRequiredService<ISynchronizationService>();
        private static readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();

        /// <summary>
        /// Prevent saving on netplay sessions unless allowed in config.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveManager.SaveStats))]
        public static bool SaveGame_Prefix()
        {
            if (synchronizationService.HasNetplaySessionInitialized() || synchronizationService.IsLoadingNextLevel())
            {
                if (!ModConfig.AllowSavesDuringNetplay.Value)
                {
                    Plugin.Log.LogInfo("Skipping SaveStats during netplay session");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Prevent saving on netplay sessions unless allowed in config.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveManager.SaveProgression))]
        public static bool SaveProgression_Prefix()
        {
            if (synchronizationService.HasNetplaySessionInitialized() || synchronizationService.IsLoadingNextLevel())
            {
                if (!ModConfig.AllowSavesDuringNetplay.Value)
                {
                    Plugin.Log.LogInfo("Skipping SaveProgression during netplay session");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Prevent saving on netplay sessions unless allowed in config.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveManager.SaveConfig))]
        public static bool SaveConfig_Prefix()
        {
            if (synchronizationService.HasNetplaySessionInitialized() || synchronizationService.IsLoadingNextLevel())
            {
                if (!ModConfig.AllowSavesDuringNetplay.Value)
                {
                    Plugin.Log.LogInfo("Skipping SaveConfig during netplay session");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Prevent saving on netplay sessions unless allowed in config.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveManager.SaveTemp))]
        public static bool SaveTemp_Prefix()
        {
            if (synchronizationService.HasNetplaySessionInitialized() || synchronizationService.IsLoadingNextLevel())
            {
                if (!ModConfig.AllowSavesDuringNetplay.Value)
                {
                    Plugin.Log.LogInfo("Skipping SaveTemp during netplay session");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Trigger update when quitting if an update is available
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveManager.OnApplicationQuit))]
        public static void OnApplicationQuit_Prefix()
        {
            try
            {
                if (!autoUpdaterService.IsAnUpdateAvailable())
                {
                    Plugin.Log.LogInfo("No updates available to apply.");
                    return;
                }

                Plugin.Log.LogInfo("Update available - launching updater NOW...");
                var pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                autoUpdaterService.LaunchUpdaterOnExit(pluginDirectory);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error when quitting: {ex}");
            }
        }
    }
}
