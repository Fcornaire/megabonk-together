using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Interactables
{
    [HarmonyPatch(typeof(InteractableSkeletonKingStatue))]
    internal static class InteractableSkeletonKingStatuePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetRequiredService<ISynchronizationService>();

        /// <summary>
        /// Manually deactivate the statue on client
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InteractableSkeletonKingStatue.Interact))]
        public static void Interact_Postfix(InteractableSkeletonKingStatue __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                __instance.gameObject.SetActive(false);
            }
        }
    }
}
