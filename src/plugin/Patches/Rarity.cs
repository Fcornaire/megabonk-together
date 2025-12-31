using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(Rarity))]
    internal static class RarityPatches
    {
        private static readonly ISpawnedObjectManagerService spawnedObjectManagerService = Plugin.Services.GetService<ISpawnedObjectManagerService>();

        /// <summary>
        /// Use the requested shady guy rarity on client (set in InteractableShadyGuy.Start_Prefix)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Rarity.GetShadyGuyRarity))]
        public static void GetShadyGuyRarity_Postfix(ref EItemRarity __result)
        {
            var rarity = spawnedObjectManagerService.UnqueueShadyGuyRarityRequest();

            if (rarity.HasValue)
            {
                __result = rarity.Value;
            }
        }
    }
}
