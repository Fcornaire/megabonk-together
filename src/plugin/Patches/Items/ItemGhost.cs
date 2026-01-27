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

        /// <summary>
        /// Skip original interaction if not successful. This prevent a memory access violation somehow when the original code run 
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemGhost.OnInteracted))]
        public static bool OnInteracted_Prefix(bool success)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!success)
            {
                return false;
            }

            return true;
        }
    }
}
