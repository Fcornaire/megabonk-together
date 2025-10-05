using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches.Interactables
{
    [HarmonyPatch(typeof(InteractableShadyGuy))]
    internal static class InteractableShadyGuyPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly ISpawnedObjectManagerService spawnedObjectManagerService = Plugin.Services.GetService<ISpawnedObjectManagerService>();

        /// <summary>
        /// On client, use the rarirty sent by server
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InteractableShadyGuy.Start))]
        public static void Start_Prefix(InteractableShadyGuy __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;

            if (isServer) return;

            var rarity = DynamicData.For(__instance).Get<EItemRarity?>("rarity");
            if (rarity.HasValue)
            {
                spawnedObjectManagerService.AddShadyGuyRarityRequest(rarity.Value);
            }

            return;
        }


        /// <summary>
        /// On server, send the shady guy to clients (we wait for start to have the correct rarity)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InteractableShadyGuy.Start))]
        public static void Start_Postfix(InteractableShadyGuy __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (isServer)
            {
                synchronizationService.OnSpawnedObject(__instance.gameObject);
            }
        }
    }
}
