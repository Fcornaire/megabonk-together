using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches.Interactables
{
    [HarmonyPatch(typeof(InteractableMicrowave))]
    internal static class InteractableMicroWavePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly ISpawnedObjectManagerService spawnedObjectManagerService = Plugin.Services.GetService<ISpawnedObjectManagerService>();

        /// <summary>
        /// On client, use the rarity sent by server
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InteractableMicrowave.Start))]
        public static void Start_Prefix(InteractableMicrowave __instance)
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
        /// On server, send the microwave to clients (we wait for start to have the correct rarity)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InteractableMicrowave.Start))]
        public static void Start_Postfix(InteractableMicrowave __instance)
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
