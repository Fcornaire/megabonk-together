using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(WeaponInventory))]
    public static class WeaponInventoryPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Synchronizes weapon additions in netplay
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WeaponInventory.AddWeapon))]
        public static void AddWeapon_Post(WeaponInventory __instance, WeaponData weaponData, List<StatModifier> upgradeOffer)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnWeaponAdded(__instance, weaponData, upgradeOffer);

        }

        /// <summary>
        /// Prevent weapons tick on client (Server side only)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(WeaponInventory.Tick))]
        public static bool Tick_Prefix(WeaponInventory __instance)
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

        /// <summary>
        /// Synchronizes weapon toggles
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WeaponInventory.ToggleWeapon))]
        public static void ToggleWeapon_Postfix(WeaponInventory __instance, EWeapon eWeapon, bool enable)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnWeaponToggled(__instance, eWeapon, enable);
        }
    }
}
