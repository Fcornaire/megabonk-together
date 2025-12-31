using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(BossPylon))]
    internal static class BossPylonPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Synchronize starting to charge pylon.
        /// The server check and notify other clients if they need to start (Nothing happens if already charging)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossPylon.OnTriggerEnter))]
        public static bool OnTriggerEnter_Prefix(BossPylon __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }

            var pylonNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");

            if (pylonNetplayId.HasValue)
            {
                return synchronizationService.OnStartingToChargingPylon(pylonNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Pylon has no netplay id set!");
            }

            return true;
        }

        /// <summary>
        /// Synchronize stopping to pylon.
        /// The server check and notify other clients if they need to stop (Nothing happens if a player is still charging)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossPylon.OnTriggerExit))]
        public static bool OnTriggerExit_Prefix(BossPylon __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }
            var pylonNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            if (pylonNetplayId.HasValue)
            {
                return synchronizationService.OnStoppingChargingPylon(pylonNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("BossPylon has no netplay id set!");
            }
            return true;
        }
    }
}
