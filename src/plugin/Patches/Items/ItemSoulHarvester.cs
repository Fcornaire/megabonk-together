using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Items
{
    [HarmonyPatch(typeof(ItemSoulHarvester))]
    internal static class ItemSoulHarvesterPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Prevent triggering SoulHarvester effect if not owned by the local player.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemSoulHarvester.OnEnemyDied))]
        public static bool OnEnemyDied_Prefix(ItemSoulHarvester __instance, Enemy enemy)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (GameManager.Instance.player.inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SoulHarvester) != __instance)
            {
                return false;
            }
            
            return true;
        }
    }
}
