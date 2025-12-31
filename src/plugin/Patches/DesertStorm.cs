using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(DesertStorm))]
    internal static class DesertStormPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevent non-hosts from starting desert storms
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DesertStorm.FadeIn))]
        public static bool FadeIn_Postfix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;

            if (!isHost)
            {
                return Plugin.Instance.CAN_START_STOP_STORMS;
            }

            return true;
        }

        /// <summary>
        /// Synchronize storm start event
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesertStorm.FadeIn))]
        public static void FadeIn_Postfix(DesertStorm __instance)
        {
            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return;
            }
            synchronizationService.OnStormStarted(__instance);

        }

        /// <summary>
        /// Prevent non-hosts from stopping desert storms
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DesertStorm.FadeOut))]
        public static bool FadeOut_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return Plugin.Instance.CAN_START_STOP_STORMS;
            }
            return true;
        }

        /// <summary>
        /// Synchronize storm stop event
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DesertStorm.FadeOut))]
        public static void FadeOut_Postfix()
        {
            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return;
            }
            synchronizationService.OnStormStopped();
        }
    }
}
