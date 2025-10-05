using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Interactables
{
    [HarmonyPatch(typeof(InteractableCharacterFight))]
    internal static class InteractableCharacterFightPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Host.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Synchronize to interactable fight enemy spawn to server
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InteractableCharacterFight.SpawnEnemy))]
        public static bool SpawnEnemy_Postfix(InteractableCharacterFight __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                synchronizationService.OnInteractableFightEnemySpawned(__instance);
                __instance.gameObject.SetActive(false);
                return false;
            }

            return true;
        }
    }
}
