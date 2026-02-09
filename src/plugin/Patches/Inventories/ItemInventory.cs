using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Inventories
{
    [HarmonyPatch(typeof(ItemInventory))]
    internal static class ItemInventoryPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();


        /// <summary>
        /// Synchronize item addition
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemInventory.AddItem), [typeof(EItem)])]
        public static void AddItem_Postfix(EItem eItem)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnItemAdded(eItem);
        }

        /// <summary>
        /// Synchronize item removal
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemInventory.RemoveItem))]
        public static void RemoveItem_Postfix(EItem eItem)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnItemRemoved(eItem);
        }
    }
}
