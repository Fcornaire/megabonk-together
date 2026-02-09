using Assets.Scripts.Steam;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(SteamAchievementsManager))]
    internal static class SteamAchievementsManagerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevents the original Steam achievement upload when running netplay ssession
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SteamAchievementsManager.QueueUpload))]
        public static bool QueueUpload_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            Plugin.Log.LogInfo("Denied achievement upload attempt.");
            return false;
        }

        /// <summary>
        /// Prevents the original Steam achievement upload when running netplay ssession
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SteamAchievementsManager.TryUploadAchievements))]
        public static bool TryUploadAchievements_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }
            Plugin.Log.LogInfo("Denied achievement upload attempt.");
            return false;
        }
    }
}
