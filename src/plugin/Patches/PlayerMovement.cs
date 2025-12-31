using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PlayerMovement))]
    internal static class PlayerMovementPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Reset world size when teleporting player back to bounds (used when leaving graveyard boss room)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerMovement.TeleportPlayerBackToBounds))]
        public static void TeleportPlayerBackToBounds_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            Plugin.Instance.SetWorldSize(Plugin.Instance.OriginalWorldSize);
        }
    }
}
