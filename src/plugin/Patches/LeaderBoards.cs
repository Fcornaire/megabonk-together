using Assets.Scripts.Steam;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(Leaderboards))]
    internal static class LeaderBoardsPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevents the original leaderboard score upload when running netplay ssession
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Leaderboards.UploadScore))]
        public static bool UploadScore_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            Plugin.Log.LogInfo("Blocking leaderboard upload");
            return false;
        }
    }
}
