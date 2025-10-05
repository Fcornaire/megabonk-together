using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Utility;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(OpenChest))]
    internal static class OpenChestPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();

        /// <summary>
        /// Send chest opened info to other players only when local player opens a chest.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OpenChest.OnTriggerStay))]
        public static void OnTriggerStay_Postfix(Collider other, OpenChest __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (__instance.readyForPickupTime <= MyTime.time && !__instance.pickedup && other == GameManager.Instance.player.GetComponent<Collider>()) //Only send if ready and local player
            {
                synchronizationService.OnChestOpened(__instance);
            }
        }
    }
}
