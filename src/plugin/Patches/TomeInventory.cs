using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;


namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(TomeInventory))]
    internal class TomeInventoryPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Synchronize tome additions on server
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(TomeInventory.AddTome))]
        public static void AddTome_Postfix(TomeInventory __instance, TomeData tomeData, Il2CppSystem.Collections.Generic.List<StatModifier> upgradeOffer, ERarity rarity)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnTomeAdded(__instance, tomeData, upgradeOffer, rarity);
        }
    }
}
