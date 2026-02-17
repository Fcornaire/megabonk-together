using HarmonyLib;
using Inventory__Items__Pickups.Xp_and_Levels;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PlayerXp))]
    internal static class PlayerXpPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Synchronize XP changes in shared experience mode.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerXp.AddXp))]
        public static void AddXp_Postfix(PlayerXp __instance, int amount)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!synchronizationService.IsSharedExperienceEnabled())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.PlayerXpAddXp(__instance.xp, amount, __instance.leftOverXp);
        }

    }
}
