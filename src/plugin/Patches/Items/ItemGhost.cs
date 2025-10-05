using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Items
{
    [HarmonyPatch(typeof(ItemGhost))]
    internal static class ItemGhostPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemGhost.OnInteracted))]
        public static void OnInteracted_Prefix(ItemGhost __instance) //TODO: handle ghost item interaction  
        {
            //var isRemoteItem = playerManagerService.IsRemoteItem(__instance);

        }
    }
}
