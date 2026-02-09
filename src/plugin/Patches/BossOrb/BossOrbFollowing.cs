using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.BossOrbPatch
{
    [HarmonyPatch(typeof(BossOrb))]
    internal static class BossOrbFollowingPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Skip on client to prevent trail artifacts.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossOrb.FloatMovement))]
        public static bool FloatMovement_Prefix(BossOrb __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                return false;
            }
            return true;
        }
    }
}
