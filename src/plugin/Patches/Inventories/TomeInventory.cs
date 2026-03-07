using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Inventory__Items__Pickups;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Inventories
{
    [HarmonyPatch(typeof(TomeInventory))]
    internal static class TomeInventoryPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Track netplayer adding a chaos tome so we can prevent triggering chaos tome effect for everyone
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TomeInventory.AddTome))]
        public static void AddTome_Prefix(TomeInventory __instance, TomeData tomeData)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            ETome tome = tomeData.eTome;
            if (tome == ETome.Chaos && playerManagerService.IsRemoteTomeInventory(__instance))
            {
                Plugin.Instance.IS_NETPLAYER_ADDING_TOME = true;
            }
        }
    }
}
