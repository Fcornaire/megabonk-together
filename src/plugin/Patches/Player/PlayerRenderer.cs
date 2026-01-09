using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Player
{
    [HarmonyPatch(typeof(PlayerRenderer))]
    internal static class PlayerRendererPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Synchronize hat changes
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerRenderer.SetHat))]
        public static void SetHat_Postfix(PlayerRenderer __instance, HatData hatData)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            if (hatData == null)
            {
                return;
            }

            synchronizationService.OnHatChanged(hatData.eHat);
        }
    }
}
