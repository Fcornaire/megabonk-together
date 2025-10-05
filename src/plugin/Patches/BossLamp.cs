using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(BossLamp))]
    internal static class BossLampPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IGameBalanceService gameBalanceService = Plugin.Services.GetService<IGameBalanceService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Update charge time based on game balance settings (number of players)
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BossLamp.Awake))]
        public static void Awake_Postfix(BossLamp __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            __instance.chargeTime = gameBalanceService.GetBossLampRequiredCharge();
        }

        /// <summary>
        /// Synchronize starting to charge lamp
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.OnTriggerEnter))]
        public static bool OnTriggerEnter_Prefix(BossLamp __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }

            var lampNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");

            if (lampNetplayId.HasValue)
            {
                return synchronizationService.OnStartingToChargingLamp(lampNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Lamp has no netplay id set!");
            }

            return true;
        }

        /// <summary>
        /// Synchronize stopping charging lamp
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.OnTriggerExit))]
        public static bool OnTriggerExit_Prefix(BossLamp __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }
            var lampNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            if (lampNetplayId.HasValue)
            {
                return synchronizationService.OnStoppingChargingLamp(lampNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Lamp has no netplay id set!");
            }
            return true;
        }
    }
}
