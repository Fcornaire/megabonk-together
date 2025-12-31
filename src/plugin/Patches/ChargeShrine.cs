using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(ChargeShrine))]
    internal static class ChargeShrinePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Synchronize starting to charge shrine.
        /// The server check and notify other clients if they need to start (Nothing happens if already charging)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChargeShrine.OnTriggerEnter))]
        public static bool OnTriggerEnter_Prefix(ChargeShrine __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }

            var shrineNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");

            if (shrineNetplayId.HasValue)
            {
                return synchronizationService.OnStartingToChargingShrine(shrineNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Charge shrine has no netplay id set!");
            }

            return true;
        }

        /// <summary>
        /// Synchronize stopping to charge shrine.
        /// The server check and notify other clients if they need to stop (Nothing happens if a player is still charging)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChargeShrine.OnTriggerExit))]
        public static bool OnTriggerExit_Prefix(ChargeShrine __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }
            var shrineNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            if (shrineNetplayId.HasValue)
            {
                return synchronizationService.OnStoppingChargingShrine(shrineNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Charge shrine has no netplay id set!");
            }
            return true;
        }
    }
}
