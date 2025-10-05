using Assets.Scripts.Steam;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(SteamStatsManager))]
    internal static class SteamStatsManagerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevents the original Steam stats upload when running netplay ssession
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SteamStatsManager.QueueUpload))]
        public static bool QueueUpload_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            Plugin.Log.LogInfo("Blocked attempt to upload stats to Steam.");
            return false;
        }

        /// <summary>
        /// Prevents the original Steam stats upload when running netplay ssession
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SteamStatsManager.TryUploadStats))]
        public static bool TryUploadStats_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            Plugin.Log.LogInfo("Blocked attempt to upload stats to Steam.");
            return false;
        }
    }
}
