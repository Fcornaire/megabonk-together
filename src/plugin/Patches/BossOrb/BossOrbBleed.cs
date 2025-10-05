using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.BossOrbPatch
{
    [HarmonyPatch(typeof(BossOrbBleed))]
    internal static class BossOrbBleedPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Skip on client to prevent trail artifacts.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossOrbBleed.FloatMovement))]
        public static bool FloatMovement_Prefix(BossOrbBleed __instance)
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
