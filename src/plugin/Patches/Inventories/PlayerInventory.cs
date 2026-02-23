using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Inventories
{
    [HarmonyPatch(typeof(PlayerInventory))]
    internal static class PlayerInventoryPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevent inventory tick (spawning projectiles) when dead. Only tick status effects.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerInventory.PhysicsTick))]
        public static bool PhysicsTick_Prefix(PlayerInventory __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (GameManager.Instance.player.inventory != null && __instance == GameManager.Instance.player.inventory && GameManager.Instance.player.IsDead())
            {
                __instance.statusEffects.Tick();
                return false;
            }

            return true;
        }


        /// <summary>
        /// Capture gold before change to compute actual delta post-multiplier.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerInventory.ChangeGold))]
        public static void ChangeGold_Prefix(PlayerInventory __instance, ref float __state)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            __state = __instance.gold;
        }

        /// <summary>
        /// Synchronize gold changes in shared experience mode.
        /// Because multiple deductions can happen, we make sure to not pass under zero
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerInventory.ChangeGold))]
        public static void ChangeGold_Postfix(PlayerInventory __instance, int amount, float __state)
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

            float actualDelta = __instance.gold - __state;
            synchronizationService.OnChangeGold(actualDelta);

            if (__instance.gold < 0)
            {
                __instance.gold = 0;
            }
        }
    }
}
